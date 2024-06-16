using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.ControlPanel {
	internal class ControlPanelCommands {

		public static void Register() {
			ControlPanelCore.RegisterCommand("test_incoming", TestIncoming);
		}

		public static void Unregister() {
			ControlPanelCore.UnregisterCommand("test_incoming");
		}

		private static void TestIncoming(string[] obj) {
			Engine.Commands.Log($"Incoming test message: {string.Join('\n', obj)}");
		}

	}
}
