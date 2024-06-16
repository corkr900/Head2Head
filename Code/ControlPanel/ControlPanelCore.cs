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
			public CommandData(string cmd, Type dataType, Action<object> handler) {
				Command = cmd;
				Handler = handler;
				DataType = dataType;
			}
			public readonly string Command;
			public readonly Action<object> Handler;
			public readonly Type DataType;
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

		public static void RegisterCommand<TIncoming>(string command, Action<TIncoming> handler) where TIncoming : class {
			commands[command.ToLower()] = new CommandData(command, typeof(TIncoming),
				(object o) => {
					if (o is not null or TIncoming) {
						Engine.Commands.Log($"Received unexpected data: {o?.GetType()?.FullName}\nExpected {typeof(TIncoming).FullName}");
						Logger.Log(LogLevel.Warn, "Head2Head", $"Received unexpected data: {o?.GetType()?.FullName}\nExpected {typeof(TIncoming).FullName}");
					}
					else handler(o as TIncoming);
				});
		}

		public static void UnregisterCommand(string command) {
			string cmd = command.ToLower();
			if (commands.ContainsKey(cmd)) commands.Remove(cmd);
		}

		internal static void SendImmediate(ControlPanelPacket packet) {
			SocketHandler.Send(packet.Payload, packet.ClientToken);
		}

		internal static void FlushIncoming() {
			while (SocketHandler.IncomingCommands.TryDequeue(out ControlPanelPacket command)) {
				string cmd = command?.Command?.ToLower();
				if (!commands.ContainsKey(cmd)) {
					// TODO error message
					continue;
				}
				CommandData cmdData = commands[cmd];
				cmdData.Handler(JsonSerializer.Deserialize(command.Payload, cmdData.DataType));
			}
		}

		internal static void LogError(string message) {
			// TODO (!!!)
		}

	}
}
