using Monocle;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static Celeste.Mod.CelesteNet.DataTypes.DataInternalBlob;

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
		/// The full set of arguments. Args[0] will be the client's token and Args[1] will be the command
		/// </summary>
		//public string[] Args { get; private set; }

		/// <summary>
		/// The raw data payload
		/// </summary>
		public string Payload { get; set; }

		private ControlPanelPacket() { }

		internal static ControlPanelPacket CreateIncoming(string text) {
			string[] parts = text.Split('|', StringSplitOptions.TrimEntries);
			if (parts.Length < 2) {
				Logger.Log(LogLevel.Error, "Head2Head", $"Received invalid incoming message: {text}");
				Engine.Commands.Log($"Received invalid incoming message: {text}");
				return null;
			}
			ControlPanelPacket packet = new();
			packet.Incoming = true;
			packet.ClientToken = parts[0];
			packet.Command = parts[1];
			packet.Payload = text;
			return packet;
		}

		internal static ControlPanelPacket CreateOutgoing(string command, object data, string targetToken = "") {
			try {
				string doc = JsonSerializer.Serialize(new SerializableCommand(command, data));
				ControlPanelPacket packet = new();
				packet.Outgoing = true;
				packet.ClientToken = targetToken;
				packet.Command = command;
				packet.Payload = doc;
				return packet;
			}
			catch (Exception e) {
				Engine.Commands.Log(e);
				return null;
			}
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
