using Monocle;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
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
		public string[] Args { get; private set; }

		/// <summary>
		/// The raw payload as received from the stream
		/// </summary>
		public string Payload {
			get => Outgoing ? $"{Command}|{string.Join('|', Args)}" : _payload;
			set => _payload = value;
		}
		private string _payload;  // Only used for incoming

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
			packet.Args = parts;
			return packet;
		}

		internal static ControlPanelPacket CreateOutgoing(string command, params string[] args) {
			ControlPanelPacket packet = new();
			packet.Outgoing = true;
			packet.ClientToken = "";
			packet.Command = command;
			packet.Args = args;
			return packet;
		}
	}
}
