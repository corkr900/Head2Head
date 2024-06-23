using Monocle;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.ControlPanel {
	internal class ControlPanelCore {

		private class CommandData {
			public CommandData(string cmd, Action<ControlPanelPacket> handler) {
				Command = cmd;
				Handler = handler;
			}
			public readonly string Command;
			public readonly Action<ControlPanelPacket> Handler;
		}

		private static Dictionary<string, CommandData> commands = new();

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

		public static void RegisterCommand(string command, Action<ControlPanelPacket> handler) {
			commands[command.ToLower()] = new CommandData(command, handler);
		}

		public static void UnregisterCommand(string command) {
			string cmd = command.ToLower();
			if (commands.ContainsKey(cmd)) commands.Remove(cmd);
		}

		public static void UnregisterAllCommands() {
			commands.Clear();
		}

		internal static void SendImmediate(ControlPanelPacket packet) {
			SocketHandler.Send(packet.Payload, packet.ClientToken);
		}

		internal static void FlushIncoming() {
			while (SocketHandler.IncomingCommands.TryDequeue(out ControlPanelPacket command)) {
				string cmd = command?.Command?.ToLower();
				if (string.IsNullOrEmpty(cmd)) continue;
				if (!commands.TryGetValue(cmd, out CommandData cmdData)) {
					Logger.Log(LogLevel.Warn, "Head2Head", $"Received unknown incoming Control Panel command '{cmd}'");
					continue;
				}
				try {
					cmdData.Handler(command);
				}
				catch (Exception e) {
					Logger.Log(LogLevel.Warn, "Head2Head", $"Error occurred deserializing incoming command '{cmd}'\n{e}");
				}
			}
		}

		internal static void LogError(string message) {
			// TODO (!!!)
		}

	}
}
