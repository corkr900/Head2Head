﻿using Monocle;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static Celeste.Mod.CelesteNet.DataTypes.DataInternalBlob;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Celeste.Mod.Head2Head.ControlPanel {
	internal class ControlPanelPacket {

		/// <summary>
		/// Indicates whether thje packet is outgoing
		/// </summary>
		public bool Outgoing { get; private set; } = true;

		/// <summary>
		/// Indicates whether this packet is incoming
		/// </summary>
		public bool Incoming {
			get => !Outgoing;
			private set => Outgoing = !value;
		}

		/// <summary>
		/// The client's unique identifier
		/// </summary>
		public string ClientToken { get; private set; } = ControlPanel.ClientToken.Server;

		/// <summary>
		/// The command to run on the receiver's end
		/// </summary>
		public string Command { get; private set; }
		/// <summary>
		/// The JSON Data element sent from the client on incoming packets
		/// </summary>
		public JsonElement Json { get; private set; }
		/// <summary>
		/// The raw data payload for outgoing packets
		/// </summary>
		public string Payload {
			get {
				if (_deferredSerializationData != null) {
					_serailizedData = JsonSerializer.Serialize(new SerializableCommand(Command, _deferredSerializationData));
					_deferredSerializationData = null;
				}
				return _serailizedData;
			}
		}
		private string _serailizedData = null;
		public string RequestID { get; private set; }

		private object _deferredSerializationData = null;

		private ControlPanelPacket() { }

		internal static ControlPanelPacket CreateIncoming(byte[] data) {
			JsonElement cmdElem;
			JsonElement tokenElem;
			JsonElement dataElem;
			JsonElement reqElem;
			bool hasReqElem;
			try {
				JsonDocument doc = JsonDocument.Parse(data);
				if (doc.RootElement.ValueKind != JsonValueKind.Object
					|| !doc.RootElement.TryGetProperty("Command", out cmdElem)
					|| cmdElem.ValueKind != JsonValueKind.String
					|| !doc.RootElement.TryGetProperty("Token", out tokenElem)
					|| tokenElem.ValueKind != JsonValueKind.String
					|| !doc.RootElement.TryGetProperty("Data", out dataElem))
				{
					Logger.Log(LogLevel.Error, "Head2Head", $"An error occurred deserializing an incoming Control Panel message:" +
						$"The message is not a JSON object containing the properties 'Command' (string), 'Token' (string), and 'Data' (any).");
					return null;
				}
				hasReqElem = doc.RootElement.TryGetProperty("RequestID", out reqElem);
			}
			catch (Exception e) {
				Logger.Log(LogLevel.Error, "Head2Head", $"An error occurred deserializing an incoming Control Panel message:\n{e}");
				return null;
			}
			ControlPanelPacket packet = new();
			packet.Incoming = true;
			packet.ClientToken = tokenElem.ToString();
			packet.Command = cmdElem.ToString();
			packet.Json = dataElem;
			packet.RequestID = hasReqElem ? reqElem.ToString() : "";
			return packet;
		}

		internal static ControlPanelPacket CreateOutgoing(string command, object data, string targetToken = "", string requestID = "") {
			try {
				ControlPanelPacket packet = new();
				packet.Outgoing = true;
				packet.ClientToken = targetToken;
				packet.Command = command;
				packet.RequestID = requestID;
				packet._deferredSerializationData = data;
				return packet;
			}
			catch (Exception e) {
				Logger.Log(LogLevel.Warn, "Head2Head", $"Error occurred in outgoing packet serialization:\n{e}\n----- WITH PAYLOAD OBJECT -----\n{data}");
				return null;
			}
		}

		internal string GetString(string propertyName, string dflt = "") {
			return Json.TryGetProperty(propertyName, out JsonElement elem) ? elem.GetString() ?? dflt : dflt;
		}
	}

	public struct SerializableCommand {
		public SerializableCommand(string cmd, object data) {
			Command = cmd;
			Data = data;
		}
		public string Command { get; private set; }
		public object Data { get; private set; }
	}

}
