using Celeste.Mod.Head2Head.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.ControlPanel {


	public struct MatchSerializableInfo {
		public MatchSerializableInfo(MatchDefinition definition) {
			def = definition;
		}
		private readonly MatchDefinition def;

		public string InternalID => def?.MatchID;
		public MatchState State => def?.State ?? MatchState.None;
		public string StateTitle => Util.TranslatedMatchState(State);
		public string DisplayName => def?.MatchDisplayName;
		public string CategoryName => def?.CategoryDisplayName;
		public bool IsRandomizer => def?.HasRandomizerObjective ?? false;
		public List<PlayerSerializableInfo> Players => GetPlayerInfo();
		public List<string> AvailableActions => GetActions();
		public string PrimaryMap => def.Phases[0]?.Area.SID;
		public string PrimaryMapName => def.Phases[0]?.Area.DisplayName;
		public string PrimaryMapIconId => SerializeImage.FromGui(def.CategoryIcon).Id;

		private List<PlayerSerializableInfo> GetPlayerInfo() {
			if (def == null) return new();
			List<PlayerSerializableInfo> ret = new(def.Players.Count);
			foreach (PlayerID pid in def.Players) {
				ret.Add(new PlayerSerializableInfo(def, pid));
			}
			return ret;
		}

		private List<string> GetActions() {
			List<string> ret = new();
			if (Head2HeadModule.Instance.CanStageMatch() && PlayerStatus.Current.CurrentMatchID != def.MatchID) {
				ret.Add("STAGE_MATCH");
			}
			if (Head2HeadModule.Instance.CanJoinMatch()) {
				ret.Add("JOIN_MATCH");
			}
			return ret;
		}

	}

	public struct PlayerSerializableInfo {
		public PlayerSerializableInfo(MatchDefinition definition, PlayerID player) {
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
		public List<PlayerPhaseSerializableInfo> Phases => GetPhases();

		private List<PlayerPhaseSerializableInfo> GetPhases() {
			List<PlayerPhaseSerializableInfo> ret = new(def.Phases.Count);
			foreach (MatchPhase ph in def.Phases) {
				ret.Add(new PlayerPhaseSerializableInfo(def, pla, ph));
			}
			return ret;
		}
	}

	public struct PlayerPhaseSerializableInfo {
		public PlayerPhaseSerializableInfo(MatchDefinition definition, PlayerID player, MatchPhase phase) {
			def = definition;
			pla = player;
			ph = phase;
		}
		private readonly MatchDefinition def;
		private readonly PlayerID pla;
		private readonly MatchPhase ph;

		public uint InternalId => ph.ID;
		public bool Completed => def.Result?[pla].Result == ResultCategory.Completed;
		public List<PlayerObjectiveSerializableInfo> Objectives => GetObjectives();

		private List<PlayerObjectiveSerializableInfo> GetObjectives() {
			List<PlayerObjectiveSerializableInfo> ret = new(def.Phases.Count);
			foreach (MatchObjective obj in ph.Objectives) {
				ret.Add(new PlayerObjectiveSerializableInfo(pla, obj));
			}
			return ret;
		}
	}

	public struct PlayerObjectiveSerializableInfo {
		public PlayerObjectiveSerializableInfo(PlayerID player, MatchObjective obj) {
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
		public double TimeLimit => new TimeSpan(ob.AdjustedTimeLimit(pla)).TotalMilliseconds;
		public string Icon => ob.GetIconURI();
	}

}
