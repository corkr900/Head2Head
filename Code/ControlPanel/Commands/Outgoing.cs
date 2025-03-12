using Celeste.Mod.Head2Head.IO;
using Celeste.Mod.Head2Head.Shared;
using Monocle;
using System;
using System.Collections.Generic;
using System.IO;
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

		public static void ControlPanelActionsUpdate(string targetClientToken = "") {
			ControlPanelCore.SendImmediate(ControlPanelPacket.CreateOutgoing(
				"UPDATE_ACTIONS",
				new SerializeControlPanelActions(),
				targetClientToken
			));
		}

		public static void CommandResult(bool result, string targetClientToken, string requestID, string info = "") {
			if (string.IsNullOrEmpty(requestID) || string.IsNullOrEmpty(targetClientToken)) return;
			ControlPanelCore.SendImmediate(ControlPanelPacket.CreateOutgoing(
				"RESULT",
				new SerializeCommandResult() {
					Result = result,
					Info = info
				},
				targetClientToken,
				requestID
			));
		}

		public static void ImageData(string atlas, string path, string targetClientToken) {
			ControlPanelPacket outgoing = atlas switch {
				"gui" => ControlPanelPacket.CreateOutgoing(
					"IMAGE",
					SerializeImage.FromGui(path, true),
					targetClientToken),
				_ => null
			};
			if (outgoing != null) ControlPanelCore.SendImmediate(outgoing);
		}

		public static void PlayerListUpdate(string targetClientToken) {
			var packet = ControlPanelPacket.CreateOutgoing("PLAYER_LIST", new SerializePlayerList(), targetClientToken);
			ControlPanelCore.SendImmediate(packet);
		}

		internal static void PlayerEnabledMods(string targetClientToken, string displayText) {
			ControlPanelCore.SendImmediate(ControlPanelPacket.CreateOutgoing("PLAYER_ENABLED_MODS", displayText, targetClientToken));
		}
	}
}
