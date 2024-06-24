using Celeste.Mod.Head2Head.IO;
using Celeste.Mod.Head2Head.Shared;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.ControlPanel.Commands {
	internal class Outgoing {

		public static void CurrentMatchStatus(MatchDefinition def, string targetClientToken = "") {
			ControlPanelCore.SendImmediate(ControlPanelPacket.CreateOutgoing(
				"CURRENT_MATCH",
				new SerializeMatch(def),
				targetClientToken
			));
		}

		public static void OtherMatchStatus(MatchDefinition def, string targetClientToken = "") {
			ControlPanelCore.SendImmediate(ControlPanelPacket.CreateOutgoing(
				"OTHER_MATCH",
				new SerializeMatch(def),
				targetClientToken
			));
		}

		public static void MatchNoLongerCurrent(string id, string targetClientToken = "") {
			ControlPanelCore.SendImmediate(ControlPanelPacket.CreateOutgoing(
				"MATCH_NOT_CURRENT",
				id,
				targetClientToken
			));
		}

		public static void MatchForgotten(string id, string targetClientToken = "") {
			ControlPanelCore.SendImmediate(ControlPanelPacket.CreateOutgoing(
				"MATCH_FORGOTTEN",
				id,
				targetClientToken
			));
		}

		public static void MatchLog(MatchLog log, string targetClientToken = "") {
			ControlPanelCore.SendImmediate(ControlPanelPacket.CreateOutgoing(
				"MATCH_LOG",
				log,
				targetClientToken
			));
		}

	}
}
