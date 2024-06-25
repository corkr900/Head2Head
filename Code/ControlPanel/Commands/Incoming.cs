using Celeste.Mod.Head2Head.IO;
using Celeste.Mod.Head2Head.Shared;
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
			ControlPanelCore.RegisterCommand("stage_match", StageMatch);
			ControlPanelCore.RegisterCommand("join_match", JoinMatch);
			ControlPanelCore.RegisterCommand("start_match", StartMatch);
			ControlPanelCore.RegisterCommand("get_my_match_log", GetMyMatchLog);
			ControlPanelCore.RegisterCommand("get_other_match_log", RequestMatchLog);
			ControlPanelCore.RegisterCommand("forget_match", ForgetMatch);
			ControlPanelCore.RegisterCommand("match_drop_out", DropOutOfMatch);
			ControlPanelCore.RegisterCommand("kill_match", KillMatch);
			SocketHandler.OnClientConnected += OnClientConnected;
        }

		public static void Unregister() {
			SocketHandler.OnClientConnected -= OnClientConnected;
			ControlPanelCore.UnregisterAllCommands();
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
			string request = pack.Json.ToString();
			int pos = request.IndexOf(':');
			string atlas = request[..pos];
			string path = request[(pos + 1)..];
			ControlPanelPacket outgoing = atlas switch {
				"gui" => ControlPanelPacket.CreateOutgoing(
					"IMAGE",
					SerializeImage.FromGui(path, true),
					pack.ClientToken
				),
				_ => null
			};
			if (outgoing != null) ControlPanelCore.SendImmediate(outgoing);
		}

		private static void StageMatch(ControlPanelPacket packet) {
			string id = packet.Json.GetString();
			if (Head2HeadModule.knownMatches.TryGetValue(id, out var match)) {
				if (id != PlayerStatus.Current.CurrentMatchID) {
					Head2HeadModule.Instance.StageMatch(match);
				}
			}
			else {
				// No longer know the match
				Outgoing.MatchForgotten(id);
			}
		}

		private static void JoinMatch(ControlPanelPacket packet) {
			string id = packet.Json.GetString();
			if (Head2HeadModule.knownMatches.TryGetValue(id, out var match)) {
				if (id != PlayerStatus.Current.CurrentMatchID) {
					Head2HeadModule.Instance.StageMatch(match);
				}
				Head2HeadModule.Instance.JoinStagedMatch();
			}
			else {
				// No longer know the match
				Outgoing.MatchForgotten(id);
			}
		}

		private static void StartMatch(ControlPanelPacket packet) {
			string id = packet.Json.GetString();
			if (!Head2HeadModule.knownMatches.TryGetValue(id, out var match)) {
				// No longer know the match
				Outgoing.MatchForgotten(id);
				return;
			}
			else if (id != PlayerStatus.Current.CurrentMatchID
				&& !RoleLogic.AllowStartingUnstagedMatches()
				&& !Head2HeadModule.Instance.StageMatch(match))
			{
				// Cannot stage the match and cannot start without staging
				return;
			}
			else if (!Head2HeadModule.Instance.CanStartMatch()) {
				// Staged but cannot start... probably because I have to join or my role prevents it
				return;
			}
			else if (id == PlayerStatus.Current.CurrentMatchID) {
				// is staged, start
				Head2HeadModule.Instance.BeginStagedMatch();
			}
			else if (match.State < MatchState.InProgress && RoleLogic.AllowStartingUnstagedMatches()) {
				// is not staged, start
				match.State = MatchState.InProgress;  // sends update
			}
		}

		private static void ForgetMatch(ControlPanelPacket packet) {
			string id = packet.Json.GetString();
			Head2HeadModule.Instance.TryForgetMatch(id);
		}

		private static void DropOutOfMatch(ControlPanelPacket packet) {
			string id = packet.Json.GetString();
			if (PlayerStatus.Current.CurrentMatchID != id) {
				// Not in the match
				return;
			}
			else {
				Head2HeadModule.Instance.DropOutOfCurrentMatch();
			}
		}

		private static void KillMatch(ControlPanelPacket packet) {
			string id = packet.Json.GetString();
			if (!Head2HeadModule.knownMatches.TryGetValue(id, out var match)) {
				// Don't know the match
				return;
			}
			else {
				Head2HeadModule.Instance.KillMatch(match);
			}
		}

		private static void GetMyMatchLog(ControlPanelPacket packet) {
			string id = packet.Json.GetString();
			if (!ActionLogger.LogFileExists(id)) {
				// TODO no log file
			}
			else {
				Outgoing.MatchLog(ActionLogger.LoadLog(id), packet.ClientToken);
			}
		}

		private static void RequestMatchLog(ControlPanelPacket packet) {
			string matchID = packet.Json.GetProperty("matchID").GetString() ?? "";
			string serialPlayerID = packet.Json.GetProperty("playerID").GetString() ?? "";
			PlayerID playerID = PlayerID.FromSerialized(serialPlayerID);
			if (!playerID.IsDefault) {
				CNetComm.Instance.SendMatchLogRequest(playerID, matchID, true, packet.ClientToken);
			}
		}

	}
}
