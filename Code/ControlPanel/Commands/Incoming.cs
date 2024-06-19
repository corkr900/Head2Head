using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.ControlPanel.Commands
{
    internal class Incoming
    {

        public static void Register()
        {
            ControlPanelCore.RegisterCommand("test_incoming", TestIncoming);
			ControlPanelCore.RegisterCommand("request_image", RequestImage);
			SocketHandler.OnClientConnected += OnClientConnected;
        }

        public static void Unregister() {
			SocketHandler.OnClientConnected -= OnClientConnected;
			ControlPanelCore.UnregisterCommand("test_incoming");
			ControlPanelCore.UnregisterCommand("request_image");
		}

		private static void OnClientConnected(string token) {
			foreach (var match in Head2HeadModule.knownMatches.Values) {
                match.SendControlPanelUpdate(token);
            }
		}

		private static void TestIncoming(ControlPanelPacket pack)
        {
            Engine.Commands.Log($"Incoming test message: {pack.Json}");
        }

		private static void RequestImage(ControlPanelPacket pack) {
			Engine.Commands.Log($"Incoming image request: {pack.Json}");
			string request = pack.Json.ToString();
			int pos = request.IndexOf(':');
			string atlas = request[..pos];
			string path = request[(pos + 1)..];
			ControlPanelPacket outgoing = atlas switch {
				"gui" => ControlPanelPacket.CreateOutgoing(
					"IMAGE",
					SerializeImage.FromGui(path),
					pack.ClientToken
				),
				_ => null
			};
			if (outgoing != null) ControlPanelCore.SendImmediate(outgoing);
		}

	}
}
