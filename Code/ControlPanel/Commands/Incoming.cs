using Celeste.Mod.Head2Head.Integration;
using Celeste.Mod.Head2Head.IO;
using Celeste.Mod.Head2Head.Shared;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.ControlPanel.Commands
{
    internal class Incoming
    {

        public static void Register() {
            ControlPanelCore.RegisterCommand("subscriptions", Subscriptions);
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
			ControlPanelCore.RegisterCommand("unstage_match", UnstageMatch);
			ControlPanelCore.RegisterCommand("dbg_purge_data", PurgeData);
			ControlPanelCore.RegisterCommand("dbg_pull_data", PullData) ;
			ControlPanelCore.RegisterCommand("give_match_pass", GiveMatchPass);
			ControlPanelCore.RegisterCommand("go_to_Lobby", GoToLobby);
			ControlPanelCore.RegisterCommand("stage_custom_rando", StageCustomRandomizerMatch);
			ControlPanelCore.RegisterCommand("save_custom_rando", SaveCustomRandomizerCategory);
			ClientSocket.OnClientConnected += OnClientConnected;
        }

		public static void Unregister() {
			ClientSocket.OnClientConnected -= OnClientConnected;
			ControlPanelCore.UnregisterAllCommands();
		}

		private static void OnClientConnected(string token) {
			foreach (var match in Head2HeadModule.knownMatches.Values) {
                match?.SendControlPanelUpdate(token);
            }
			Outgoing.ControlPanelActionsUpdate(token);
		}

		private static void Subscriptions(ControlPanelPacket packet) {
			string clientToken = packet.ClientToken;
			ClientSocket cli = ControlPanelCore.GetClient(clientToken);
			if (cli == null) {
				Logger.Log(LogLevel.Warn, "Head2Head", $"Tried to set subscriptions for nonexistant client with token '{clientToken}'.");
				return;
			}
			if (packet.Json.TryGetProperty("remove", out JsonElement removals)) {
				if (removals.ValueKind != JsonValueKind.Array) {
					Logger.Log(LogLevel.Error, "Head2Head", $"Subscriptions command: unexpected value kind for property 'remove'. Epected Array, got '{removals.ValueKind}'.");
					return;
				}
				foreach (JsonElement elem in removals.EnumerateArray()) {
					string command = elem.GetString();
					cli.SetSubscription(command, false);
				}
			}
			if (packet.Json.TryGetProperty("add", out JsonElement additions)) {
				if (additions.ValueKind != JsonValueKind.Array) {
					Logger.Log(LogLevel.Error, "Head2Head", $"Subscriptions command: unexpected value kind for property 'add'. Epected Array, got '{additions.ValueKind}'.");
					return;
				}
				foreach (JsonElement elem in additions.EnumerateArray()) {
					string command = elem.GetString();
					cli.SetSubscription(command, true);
				}
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
			Outgoing.ImageData(atlas, path, pack.ClientToken);
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
			if (Head2HeadModule.Instance.TryForgetMatch(id, true)) {
				Outgoing.MatchForgotten(id);
			}
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
			if (ActionLogger.LogFileExists(id)) {
				Outgoing.MatchLog(ActionLogger.LoadLog(id), packet.ClientToken);
			}
		}

		private static void RequestMatchLog(ControlPanelPacket packet) {
			string matchID = packet.GetString("matchID");
			string serialPlayerID = packet.GetString("playerID");
			PlayerID playerID = PlayerID.FromSerialized(serialPlayerID);
			if (!playerID.IsDefault) {
				CNetComm.Instance.SendMatchLogRequest(playerID, matchID, true, packet.ClientToken);
			}
		}

		private static void PurgeData(ControlPanelPacket packet) {
			Head2HeadModule.Instance.PurgeAllData();
		}

		private static void PullData(ControlPanelPacket packet) {
			CNetComm.Instance.SendScanRequest();
		}

		private static void GiveMatchPass(ControlPanelPacket packet) {
			if (packet.Json.TryGetProperty("playerID", out JsonElement prop)) {
				PlayerID playerID = PlayerID.FromSerialized(prop.GetString() ?? "");
				CNetComm.Instance.SendMisc(Head2HeadModule.BTA_MATCH_PASS, playerID);
			}
		}

		private static void GoToLobby(ControlPanelPacket packet) {
			new FadeWipe(Engine.Scene, false, () => {
				LevelEnter.Go(new Session(GlobalAreaKey.Head2HeadLobby.Local.Value), false);
			});
			Head2HeadModule.Instance.ClearAutoLaunchInfo();
		}

		private static void UnstageMatch(ControlPanelPacket packet) {
			if (PlayerStatus.Current.CurrentMatch?.PlayerCanLeaveFreely(PlayerID.MyIDSafe) ?? false) {
				Outgoing.MatchNoLongerCurrent(PlayerStatus.Current.CurrentMatchID);
				PlayerStatus.Current.CurrentMatch = null;
				PlayerStatus.Current.Updated();
			}
		}

		private static RandomizerOptionsTemplate MakeRandoOptions(ControlPanelPacket packet) {
			string name = packet.GetString("name");
			string darkness = packet.GetString("darkness");
			string difficulty = packet.GetString("difficulty");
			string difficultyEagerness = packet.GetString("difficultyEagerness");
			string logicType = packet.GetString("logicType");
			string mapLength = packet.GetString("mapLength");
			string numDashes = packet.GetString("numDashes");
			string seedType = packet.GetString("seedType", "Random");
			string shineLights = packet.GetString("shineLights");
			string strawberryDensity = packet.GetString("strawberryDensity");
			string seed = packet.GetString("seed");
			return new RandomizerOptionsTemplate() {
				Darkness = darkness,
				Difficulty = difficulty,
				DifficultyEagerness = difficultyEagerness,
				LogicType = logicType,
				MapLength = mapLength,
				NumDashes = numDashes,
				SeedType = seedType,
				ShineLights = shineLights,
				StrawberryDensity = strawberryDensity,
				Seed = seed,
			};
		}

		private static void StageCustomRandomizerMatch(ControlPanelPacket packet) {
			string name = packet.GetString("name");
			MatchTemplate matchTemplate = RandomizerCategories.RandomizerTemplate(name, MakeRandoOptions(packet));
			if (Head2HeadModule.Instance.StageMatch(matchTemplate.BuildIL())) {
				Outgoing.CommandResult(true, packet.ClientToken, packet.RequestID, Dialog.Clean("Head2Head_ControlPanel_MatchWasStaged"));
			}
			else {
				Outgoing.CommandResult(false, packet.ClientToken, packet.RequestID, Dialog.Clean("Head2Head_ControlPanel_CouldntStageMatch"));
			}

		}

		private static void SaveCustomRandomizerCategory(ControlPanelPacket packet) {
			string name = packet.GetString("name");
			RandomizerOptionsTemplate options = MakeRandoOptions(packet);
			RandomizerCustomOptionsFile file = RandomizerCustomOptionsFile.Instance;
			if (string.IsNullOrEmpty(name)) {
				Outgoing.CommandResult(false, packet.ClientToken, packet.RequestID, Dialog.Clean("Head2Head_ControlPanel_NameNotProvided"));
			}
			else if (options == null) {
				Outgoing.CommandResult(false, packet.ClientToken, packet.RequestID, Dialog.Clean("Head2Head_ControlPanel_CouldntBuildOptions"));
			}
			else if (file.Categories.Any(cat => cat.Name.ToLower() == name.ToLower())) {
				Outgoing.CommandResult(false, packet.ClientToken, packet.RequestID, Dialog.Clean("Head2Head_ControlPanel_NameAlreadyExists"));
			}
			else {
				file.Categories.Add(new RandomizerCustomOptionsCategory() {
					Name = name,
					Options = options
				});
				RandomizerCustomOptionsFile.Save();
				Ruleset.DefaultRulesetStale = true;
				Outgoing.CommandResult(true, packet.ClientToken, packet.RequestID, Dialog.Clean("Head2Head_ControlPanel_CategorySaved"));
			}
		}

	}
}
