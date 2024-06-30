using Celeste.Mod.Head2Head.IO;
using Celeste.Mod.Head2Head.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.ControlPanel {


	public struct SerializeMatch {
		public SerializeMatch(MatchDefinition definition) {
			def = definition;
			categoryIcon = SerializeImage.FromGui(def.CategoryIcon);
		}
		private readonly MatchDefinition def;
		private readonly SerializeImage categoryIcon;
		//private readonly SerializeImage primaryMapIcon;

		public string InternalID => def?.MatchID;
		public MatchState State => def?.State ?? MatchState.None;
		public string StateTitle => Util.TranslatedMatchState(State);
		public string DisplayName => def?.MatchDisplayName;
		public string CategoryName => def?.CategoryDisplayName;
		public bool IsRandomizer => def?.HasRandomizerObjective ?? false;
		public List<SerializeMatchPlayer> Players => GetPlayerInfo();
		public List<string> AvailableActions => GetActions();
		public string PrimaryMap => def?.Phases[0]?.Area.SID;
		public string PrimaryMapName => def?.Phases[0]?.Area.DisplayName;
		public SerializeImage CategoryIcon => categoryIcon;

		private List<SerializeMatchPlayer> GetPlayerInfo() {
			if (def == null) return new();
			List<SerializeMatchPlayer> ret = new(def.Players.Count);
			foreach (PlayerID pid in def.Players) {
				ret.Add(new SerializeMatchPlayer(def, pid));
			}
			return ret;
		}

		private List<string> GetActions() {
			List<string> ret = new();
			if (def == null) return ret;

			ret.Add("STAGE_MATCH");
			if (!def.Players.Contains(PlayerID.MyIDSafe)) {
				ret.Add("JOIN_MATCH");
				if (RoleLogic.AllowKillingUnjoinedMatch()) {
					ret.Add("KILL_MATCH");
				}
			}
			else if (def.State <= MatchState.InProgress) {
				ret.Add("MATCH_DROP_OUT");
				if (RoleLogic.AllowKillingMatch()) {
					ret.Add("KILL_MATCH");
				}
			}
			if (def.State <= MatchState.Staged
				&& def.Players?.Count > 0
				&& (def.Players.Contains(PlayerID.MyIDSafe)
					|| RoleLogic.AllowMatchStart(false)))
			{
				ret.Add("START_MATCH");
			}
			if (def.State >= MatchState.InProgress) {
				ret.Add("GET_MY_MATCH_LOG");
			}
			ret.Add("FORGET_MATCH");
			return ret;
		}

	}

	public struct SerializeMatchPlayer {
		public SerializeMatchPlayer(MatchDefinition definition, PlayerID player) {
			def = definition;
			pla = player;
		}
		private readonly MatchDefinition def;
		private readonly PlayerID pla;

		public string DisplayName => pla.DisplayName;
		public string Id => pla.SerializedID;
		public ResultCategory Status => def?.GetPlayerResultCat(pla) ?? ResultCategory.NotJoined;
		public string StatusTitle => Util.TranslatedMatchResult(Status);
		public double Timer => new TimeSpan(def.GetPlayerTimer(pla)).TotalMicroseconds;
		public string FormattedTimer => Dialog.FileTime(def.GetPlayerTimer(pla));
		public List<SerializeMatchPlayerPhase> Phases => GetPhases();
		public List<string> Actions => GetActions();

		private List<SerializeMatchPlayerPhase> GetPhases() {
			List<SerializeMatchPlayerPhase> ret = new(def.Phases.Count);
			foreach (MatchPhase ph in def.Phases) {
				ret.Add(new SerializeMatchPlayerPhase(def, pla, ph));
			}
			return ret;
		}

		private List<string> GetActions() {
			List<string> ret = new();
			if (def.State >= MatchState.InProgress && !pla.Equals(PlayerID.MyID)) {
				ret.Add("GET_OTHER_MATCH_LOG");
			}
			// TODO "FORCE_DNF",
			return ret;
		}

	}

	public struct SerializeMatchPlayerPhase {
		public SerializeMatchPlayerPhase(MatchDefinition definition, PlayerID player, MatchPhase phase) {
			def = definition;
			pla = player;
			ph = phase;
		}
		private readonly MatchDefinition def;
		private readonly PlayerID pla;
		private readonly MatchPhase ph;

		public uint InternalId => ph.ID;
		public bool Completed => def.Result?[pla].Result == ResultCategory.Completed;
		public List<SerializeMatchPlayerObjective> Objectives => GetObjectives();

		private List<SerializeMatchPlayerObjective> GetObjectives() {
			List<SerializeMatchPlayerObjective> ret = new(def.Phases.Count);
			foreach (MatchObjective obj in ph.Objectives) {
				ret.Add(new SerializeMatchPlayerObjective(pla, obj));
			}
			return ret;
		}
	}

	public struct SerializeMatchPlayerObjective {
		public SerializeMatchPlayerObjective(PlayerID player, MatchObjective obj) {
			pla = player;
			ob = obj;
			PlayerStatus stat = pla.Equals(PlayerID.MyID) ? PlayerStatus.Current
				: Head2HeadModule.knownPlayers.ContainsKey(pla) ? Head2HeadModule.knownPlayers[pla]
				: null;
			state = stat.objectives.FirstOrDefault((H2HMatchObjectiveState _st) => _st.ObjectiveID == obj.ID);
		}
		private readonly PlayerID pla;
		private readonly MatchObjective ob;
		private readonly H2HMatchObjectiveState state;

		public uint InternalId => ob.ID;
		public string DisplayName => ob.Label;
		public MatchObjectiveType ObjectiveType => ob.ObjectiveType;
		public string ObjectiveTypeTitle => Util.TranslatedObjectiveLabel(ObjectiveType);
		public bool Completed => state.Completed;
		public int CollectablesGoal => ob.CollectableGoal;
		public int CollectablesObtained => state.CountCollectables();
		public string TimeLimit => Util.ReadableTimeSpanTitle(ob.AdjustedTimeLimit(pla));
		public string TimeRemaining => GetTimeRemaining();
		public SerializeImage Icon => SerializeImage.FromGui(ob.GetIconPath());

		private string GetTimeRemaining() {
			PlayerStatus stat = Head2HeadModule.GetPlayerStatus(pla);
            if (stat == null) {
				return "??";
            }
            long timeRemaining = Math.Max(stat.FileTimerAtMatchBegin + ob.AdjustedTimeLimit(pla) - stat.CurrentFileTimer, 0);
			return Util.ReadableTimeSpanTitle(timeRemaining);
		}

	}

}
