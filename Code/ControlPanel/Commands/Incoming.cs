using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.ControlPanel.Commands
{
    internal class Incoming
    {

        public static void Register()
        {
            ControlPanelCore.RegisterCommand<string>("test_incoming", TestIncoming);
            SocketHandler.OnClientConnected += OnClientConnected;
        }

        public static void Unregister()
        {
            ControlPanelCore.UnregisterCommand("test_incoming");
			SocketHandler.OnClientConnected -= OnClientConnected;
		}

		private static void OnClientConnected(string token) {
			foreach (var match in Head2HeadModule.knownMatches.Values) {
                match.SendControlPanelUpdate(token);
            }
		}

		private static void TestIncoming(string data)
        {
            Engine.Commands.Log($"Incoming test message: {data}");
        }

    }
}
