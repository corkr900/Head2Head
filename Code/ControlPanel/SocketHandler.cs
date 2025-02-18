using System;
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
using Celeste.Mod.Head2Head.ControlPanel.Commands;

namespace Celeste.Mod.Head2Head.ControlPanel {

	internal class SocketHandler {

		internal static Dictionary<string, ClientSocket> clients = new();
		internal static CancellationToken cancelToken;
		private static Thread newConnThread;
		/// <summary>
		/// An object to use as the lock target when modifying or iterating on the list of clients,
		/// or other operations that may affect server connectivity
		/// </summary>
		private static object ServerLock = new();

		internal static ConcurrentQueue<ControlPanelPacket> IncomingCommands = new();

		public static void Start() {
			lock (ServerLock) {
				if (newConnThread == null) {
					cancelToken = new();
					newConnThread = new(NewConnections);
					newConnThread.Start();
				}
			}
		}

		public static void Stop() {
			cancelToken = new(true);
			if (newConnThread?.Join(5000) == false) {
				Logger.Log(LogLevel.Warn, "Head2Head", $"Control Panel connection listener thread join timed out");
				lock (ServerLock) {
					foreach (ClientSocket client in clients.Values) {
						client.Join(100);
					}
					clients.Clear();
					newConnThread = null;
					IncomingCommands.Clear();
				}
			}
			else {
				lock (ServerLock) {
					clients.Clear();
					newConnThread = null;
					IncomingCommands.Clear();
				}
			}
		}

		private static void NewConnections() {
			TcpListener tcpListener = new TcpListener(IPAddress.Any, 8080);
			try {
				tcpListener.Start();
				while (!cancelToken.IsCancellationRequested) {
					// Start listening for connection requests
					Task<Socket> task = tcpListener.AcceptSocketAsync();
					while (!cancelToken.IsCancellationRequested) {
						// Every 50 ms, check if we have a new connection or a cancellation request
						if (task.IsCompletedSuccessfully) {
							Socket s = task.Result;
							ClientSocket cs = new(task.Result);
							lock (ServerLock) {
								if (cs.Connected) {
									clients.Add(cs.Token, cs);
								}
								else {
									Logger.Log(LogLevel.Warn, "Head2Head", $"New Control Panel connection request could not be completed.");
								}
							}
							break;
						}
						else if (task.IsCompleted) {
							Logger.Log(LogLevel.Warn, "Head2Head", $"New Control Panel connections task errored - {task.Status}");
							break;
						}
						// Clean up dead connections
						lock (ServerLock) {
							List<string> disconnectedClients = new();
							foreach (ClientSocket cs in clients.Values) {
								if (!cs.Connected) disconnectedClients.Add(cs.Token);
							}
							foreach (string token in disconnectedClients) {
								clients.Remove(token);
							}
						}
						task.Wait(50, cancelToken);
					}
				}
			}
			catch (Exception e) {
				Logger.Log(LogLevel.Error, "Head2Head", "An error occurred in websocket new connection listener:\n" + e.ToString());
			}
			lock (ServerLock) {
				foreach (ClientSocket sock in clients.Values) {
					sock.Join(100);
				};
			}
			tcpListener.Server?.Dispose();
			tcpListener.Stop();
			Logger.Log(LogLevel.Warn, "Head2Head", $"Stopped listening for new Control Panel connections.");
		}

		internal static void SafeSend(ControlPanelPacket packet) {
			byte[] payload = null;  // Defer any serialization until we know we're going to send it to a client
			bool allClients = string.IsNullOrEmpty(packet.ClientToken);
			lock (ServerLock) {
				if (allClients) {
					foreach (ClientSocket client in clients.Values) {
						if (client.CanSend(packet)) {
							payload ??= ClientSocket.EncodeMessage(packet.Payload);
							client.Send_Internal(payload);
						}
					}
				}
				else if (clients.TryGetValue(packet.ClientToken, out ClientSocket sock) && sock.Connected) {
					if (sock.CanSend(packet)) {
						payload ??= ClientSocket.EncodeMessage(packet.Payload);
						sock.Send_Internal(payload);
					}
				}
				else {
					Logger.Log(LogLevel.Warn, "Head2Head", $"Could not send control panel packet to client '{packet.ClientToken}': client disconnected or does not exist");
				}
			}
		}
	}

	internal class ClientSocket {

		public delegate void OnClientConnectedHandler(string token);
		public static event OnClientConnectedHandler OnClientConnected;

		public bool Connected => socket?.Connected ?? false;

		public string Token { get; private set; }
		public CancellationToken CancelToken { get; set; }
		private readonly Socket socket;
		private readonly Thread thread;
		private NetworkStream stream;
		private CommandSubscription[] subscriptions = CommandSubscription.AllAvailableSubscriptions().ToArray();

		internal ClientSocket(Socket socket) {
			Token = ClientToken.GetNew();
			this.socket = socket;
			CancelToken = new(false);
			if (socket.Connected) {
				thread = new Thread(Listener);
				thread.Start();
			}
		}

		public void Join(int timeout) {
			CancelToken = new(true);
			if (thread?.Join(timeout) == false) {
				Logger.Log(LogLevel.Warn, "Head2Head", $"Client thread join timed out for client '{Token}'");
			}
		}

		private void Listener() {
			try {
				stream = new NetworkStream(socket);
				while (!CancelToken.IsCancellationRequested) {
					if (!stream.DataAvailable || socket.Available < 3) {
						Thread.Sleep(10);
						continue;
					}

					// TODO (!!!) this could potentially split packets >:[
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
						byte[] payload = EncodeMessage(JsonSerializer.Serialize(new SerializableCommand(
							"WELCOME",
							new SerializeWelcomeArgs() {
								Token = Token,
								Version = ControlPanelCore.VERSION,
								RandomizerInstalled = Integration.RandomizerIntegration.RandomizerLoaded,
							}
						)));
						stream.Write(payload, 0, payload.Length);

						Logger.Log(LogLevel.Info, "Head2Head", $"New Control Panel client connected. Allocated token {Token}");
						OnClientConnected?.Invoke(Token);
					}
					else {
						byte[] message = DecodeMessage(bytes, out FrameOpCode opCode);
						if (opCode == FrameOpCode.ConnectionClose) {
							Logger.Log(LogLevel.Info, "Head2Head", $"Control Panel client with token '{Token}' disconnected.");
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
			Logger.Log(LogLevel.Info, "Head2Head", $"Closed connection with Control Panel client with token '{Token}'.");
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
					opCode = FrameOpCode.ConnectionClose;  // Act like a disconnection event
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
				if (!BitConverter.IsLittleEndian) Array.Reverse(lengthArr);
				dataLength = BitConverter.ToUInt16(lengthArr);
				indexFirstMask = 4;
			}
			else if (dataLength == 127) {  // Indicates length is stored in the following 64 bits
				byte[] lengthArr = new byte[] { bytes[5], bytes[4], bytes[3], bytes[2] };
				if (!BitConverter.IsLittleEndian) Array.Reverse(lengthArr);
				dataLength = BitConverter.ToUInt64(lengthArr);
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

		internal void Send_Internal(byte[] payload) {
			if (Connected) {
				try {
					stream?.Write(payload, 0, payload.Length);
				}
				catch(Exception e) {
					Logger.Log(LogLevel.Error, "Head2Head", $"Error occurred writing to Control Panel websocket: {e.Message}\n{e.StackTrace}");
				}
			}
		}

		internal bool CanSend(ControlPanelPacket packet) {
			foreach (CommandSubscription cs in subscriptions) {
				if (cs.Commands.Contains(packet.Command) && cs.Subscribed) return true;
			}
			return false;
		}

		public bool SetSubscription(string subscriptionName, bool newState) {
			subscriptionName = subscriptionName.ToUpper();
			foreach (CommandSubscription cs in subscriptions) {
				if (cs.SubscriptionName == subscriptionName) {
					cs.Subscribed = newState;
					return true;
				}
			}
			return false;
		}
	}
}
