using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.ControlPanel {
	internal class ControlPanelCore {

		private static uint MAX_PACKET_SIZE = 4092;

		private static Dictionary<string, Action<string[]>> commands = new();

		//private static TcpListener server;
		//private static TcpClient client;

		//private static IPAddress IPAddress => IPAddress.Parse("127.0.0.1");
		//private static int Port => 80;


		private static ConcurrentQueue<ControlPanelPacket> outgoing = new ConcurrentQueue<ControlPanelPacket>();

		public static void TryInitServer() {
			SocketHandler.Start();
		}

		public static void EndServer() {
			SocketHandler.Stop();
		}

		public static void RegisterCommand(string command, Action<string[]> handler) {
			commands[command.ToLower()] = handler;
		}

		public static void UnregisterCommand(string command) {
			string cmd = command.ToLower();
			if (commands.ContainsKey(cmd)) commands.Remove(cmd);
		}

		internal static void SendImmediate(ControlPanelPacket packet) {
			SocketHandler.Send(packet.Payload);
		}

		internal static void FlushIncoming() {
			while (SocketHandler.IncomingCommands.TryDequeue(out ControlPanelPacket command)) {
				string cmd = command?.Command?.ToLower();
				if (!commands.ContainsKey(cmd)) continue;
				commands[cmd](command.Args);
			}
		}

		internal static void LogError(string message) {
			// TODO (!!!)
		}

	}
}
