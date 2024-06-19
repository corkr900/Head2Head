﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Monocle;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Json.Nodes;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Text.Json;
using Celeste.Mod.CelesteNet.Client;

namespace Celeste.Mod.Head2Head.ControlPanel {

	internal class SocketHandler {

		public delegate void OnClientConnectedHandler(string token);
		public static event OnClientConnectedHandler OnClientConnected;

		internal static List<ClientSocket> clients = new();
		internal static DateTime LastStop = DateTime.MinValue;
		internal static DateTime StartTime = DateTime.Now;
		private static Thread newConnThread;
		private static object ServerLock = new();

		internal static ConcurrentQueue<ControlPanelPacket> IncomingCommands = new();

		public static void Start() {
			lock (ServerLock) {
				if (newConnThread == null) {
					StartTime = DateTime.Now;
					newConnThread = new(NewConnections);
					newConnThread.Start();
				}
			}
		}

		public static void Stop() {
			lock (ServerLock) {
				LastStop = DateTime.Now;
				newConnThread?.Join(5000);
				foreach (ClientSocket client in clients) {
					client.Close();
				}
				clients.Clear();
				newConnThread = null;
				IncomingCommands.Clear();
			}
		}

		private static void NewConnections() {
			TcpListener tcpListener = new TcpListener(IPAddress.Parse("127.0.0.1"), 8080);
			try {
				tcpListener.Start();
				while (StartTime > LastStop) {
					Task<Socket> task = tcpListener.AcceptSocketAsync();
					while (StartTime > LastStop) {
						task.Wait(50);
						lock (ServerLock) {
							if (task.IsCompletedSuccessfully) {
								Socket s = task.Result;
								ClientSocket cs = new(task.Result);
								if (cs.Connected) {
									clients.Add(cs);
									OnClientConnected?.Invoke(cs.Token);
								}
								break;
							}
							else if (task.IsCompleted) {
								Logger.Log(LogLevel.Warn, "Head2Head", $"New connections task errored - {task.Status}");
								break;
							}
							clients.RemoveAll(cs => !cs.Connected);
						}
					}
				}
			}
			catch (Exception e) {
				Logger.Log(LogLevel.Error, "Head2Head", "An error occurred in websocket new connection listener:\n" + e.ToString());
			}
			tcpListener.Server?.Dispose();
			tcpListener.Stop();
		}

		internal static void Send(string message, string clientToken) {
			byte[] payload = ClientSocket.EncodeMessage(message);
			bool allClients = string.IsNullOrEmpty(clientToken);
			lock (ServerLock) {
				foreach (var client in clients) {
					if (allClients || client.Token == clientToken) client.Send(payload);
				}
			}
		}
	}

	internal class ClientSocket {

		public bool Connected => socket?.Connected ?? false;

		public string Token { get; private set; }

		private readonly Socket socket;
		private readonly Thread thread;
		private DateTime threadStartTime;
		NetworkStream stream;

		internal ClientSocket(Socket socket) {
			this.socket = socket;
			if (socket.Connected) {
				thread = new Thread(Listener);
				thread.Start();
			}
		}

		private void Listener() {
			try {
				stream = new NetworkStream(socket);
				threadStartTime = DateTime.Now;
				while (threadStartTime > SocketHandler.LastStop) {
					if (!stream.DataAvailable || socket.Available < 3) {
						Thread.Sleep(10);
						continue;
					}

					// TODO (!!!) this could split packets >:[
					byte[] bytes = new byte[socket.Available];
					stream.Read(bytes, 0, bytes.Length);
					string s = Encoding.UTF8.GetString(bytes);

					if (Regex.IsMatch(s, "^GET", RegexOptions.IgnoreCase)) {
						// 1. Obtain the value of the "Sec-WebSocket-Key" request header without any leading or trailing whitespace
						// 2. Concatenate it with "258EAFA5-E914-47DA-95CA-C5AB0DC85B11" (a special GUID specified by RFC 6455)
						// 3. Compute SHA-1 and Base64 hash of the new value
						// 4. Write the hash back as the value of "Sec-WebSocket-Accept" response header in an HTTP response
						string swk = Regex.Match(s, "Sec-WebSocket-Key: (.*)").Groups[1].Value.Trim();
						string swka = swk + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
						byte[] swkaSha1 = System.Security.Cryptography.SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(swka));
						string swkaSha1Base64 = Convert.ToBase64String(swkaSha1);

						// HTTP/1.1 defines the sequence CR LF as the end-of-line marker
						byte[] response = Encoding.UTF8.GetBytes(
							"HTTP/1.1 101 Switching Protocols\r\n" +
							"Connection: Upgrade\r\n" +
							"Upgrade: websocket\r\n" +
							"Sec-WebSocket-Accept: " + swkaSha1Base64 + "\r\n\r\n");
						stream.Write(response, 0, response.Length);

						// Allocate a client token and send it up
						Token = ClientToken.GetNew();
						byte[] payload = EncodeMessage(JsonSerializer.Serialize(new SerializableCommand("ALLOCATE_TOKEN", Token)));
						stream.Write(payload, 0, payload.Length);

						Logger.Log(LogLevel.Info, "Head2Head", $"New Control Panel client connected. Allocated token {Token}");
					}
					else {
						byte[] message = DecodeMessage(bytes, out FrameOpCode opCode);
						if (opCode == FrameOpCode.ConnectionClose) {
							Logger.Log(LogLevel.Info, "Head2Head", $"Control Panel client disconnected.");
							break;
						}
						else if (opCode == FrameOpCode.Text) {
							SocketHandler.IncomingCommands.Enqueue(ControlPanelPacket.CreateIncoming(message));
						}
					}
				}
			}
			catch (Exception e) {
				Logger.Log(LogLevel.Error, "Head2Head", "An error occurred in websocket client listener:\n" + e.ToString());
			}
			stream?.Dispose();
			if (socket?.Connected == true) socket.Disconnect(false);
			socket?.Dispose();
		}

		internal enum FrameOpCode : byte {
			Continuation = 0x00,
			Text = 0x01,
			Binary = 0x02,
			ConnectionClose = 0x08,
			Ping = 0x09,
			Pong = 0x0a,
		}

		/// <summary>
		/// Decodes a websocket message
		/// </summary>
		/// <param name="bytes">The raw encoded message data</param>
		/// <param name="opCode">The OpCode of the message</param>
		/// <returns>The byte array of data, and the OpCode via output parameter</returns>
		internal static byte[] DecodeMessage(byte[] bytes, out FrameOpCode opCode) {
			// Ignore mask - framework will disconnect client if it sends an unmasked packet. (RFC6455 section 5.1)
			//bool mask = (bytes[1] & 0x80) != 0x00;

			//bool FIN = (bytes[0] & 0x80) != 0x00;
			opCode = (FrameOpCode)(bytes[0] & 0x7f);
			switch (opCode) {
				case FrameOpCode.Ping:
				case FrameOpCode.Pong:
				case FrameOpCode.ConnectionClose:
					return Array.Empty<byte>();
				case FrameOpCode.Text:
					break;
				case FrameOpCode.Binary:
				case FrameOpCode.Continuation:
					string errMsg = $"H2H Control Panel: received packet with opCode '{opCode}'. H2H websocket currently does not support this.";
					Logger.Log(LogLevel.Error, "Head2Head", errMsg);
					return null;
			}
			if ((bytes[0] & 0x80) != 0x80) {
				string errMsg = $"H2H Control Panel: received non-final packet with opCode '{opCode}'. H2H websocket currently does not support multi-packet messages.";
				Logger.Log(LogLevel.Error, "Head2Head", errMsg);
				return null;
			}

			// Use the second byte to determine message length
			ulong dataLength = bytes[1] & 0x7fU;
			uint indexFirstMask = 2;
			if (dataLength == 126) {  // Indicates length is stored in the following 16 bits
				byte[] lengthArr = new byte[] { bytes[3], bytes[2] };
				if (BitConverter.IsLittleEndian) Array.Reverse(lengthArr);
				dataLength = Convert.ToUInt16(lengthArr);
				indexFirstMask = 4;
			}
			else if (dataLength == 127) {  // Indicates length is stored in the following 64 bits
				byte[] lengthArr = new byte[] { bytes[5], bytes[4], bytes[3], bytes[2] };
				if (BitConverter.IsLittleEndian) Array.Reverse(lengthArr);
				dataLength = Convert.ToUInt64(lengthArr);
				indexFirstMask = 10;
			}
			// Get the mask value
			IEnumerable<byte> keys = bytes.Skip((int)indexFirstMask).Take(4);
			uint indexFirstDataByte = indexFirstMask + 4;
			// Decode the message
			byte[] decoded = new byte[dataLength];
			for (uint i = indexFirstDataByte, j = 0; i < bytes.Length && j < dataLength; i++, j++) {
				decoded[j] = (byte)(bytes[i] ^ keys.ElementAt((int)j & 0x3));
			}
			return decoded;
		}

		internal static byte[] EncodeMessage(string message) {
			byte[] response;
			byte[] bytesRaw = Encoding.UTF8.GetBytes(message);
			byte[] frame = new byte[10];

			var indexStartRawData = -1;
			var length = bytesRaw.Length;

			frame[0] = (byte)129;
			if (length <= 125) {
				frame[1] = (byte)length;
				indexStartRawData = 2;
			}
			else if (length >= 126 && length <= 65535) {
				frame[1] = (byte)126;
				frame[2] = (byte)((length >> 8) & 255);
				frame[3] = (byte)(length & 255);
				indexStartRawData = 4;
			}
			else {
				frame[1] = (byte)127;
				frame[2] = (byte)((length >> 56) & 255);
				frame[3] = (byte)((length >> 48) & 255);
				frame[4] = (byte)((length >> 40) & 255);
				frame[5] = (byte)((length >> 32) & 255);
				frame[6] = (byte)((length >> 24) & 255);
				frame[7] = (byte)((length >> 16) & 255);
				frame[8] = (byte)((length >> 8) & 255);
				frame[9] = (byte)(length & 255);

				indexStartRawData = 10;
			}

			response = new byte[indexStartRawData + length];

			int i, reponseIdx = 0;

			//Add the frame bytes to the reponse
			for (i = 0; i < indexStartRawData; i++) {
				response[reponseIdx] = frame[i];
				reponseIdx++;
			}

			//Add the data bytes to the response
			for (i = 0; i < length; i++) {
				response[reponseIdx] = bytesRaw[i];
				reponseIdx++;
			}

			return response;
		}

		internal void Close() {
			if (socket?.Connected == true) socket.Disconnect(false);
			socket?.Dispose();
		}

		internal void Send(byte[] payload) {
			if (Connected) {
				stream.Write(payload, 0, payload.Length);
			}
		}
	}
}
