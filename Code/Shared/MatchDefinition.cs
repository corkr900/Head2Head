using Celeste.Mod.Head2Head.IO;
using Celeste.Mod.CelesteNet;
using Celeste.Mod.CelesteNet.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Monocle;
using MonoMod.Utils;
using Celeste.Mod.Head2Head.Integration;
using System.Text.Json.Serialization;
using Celeste.Mod.Head2Head.ControlPanel.Commands;

namespace Celeste.Mod.Head2Head.Shared {

    public class MatchDefinition {
        private static uint localIDCounter = 1;

        #region Invariable Members / Properties

        public string MatchID = GenerateMatchID();
        public PlayerID Owner = PlayerID.MyIDSafe;

        public List<PlayerID> Players = new List<PlayerID>();
        public List<MatchPhase> Phases = new List<MatchPhase>();
        public List<MatchRule> Rules = new List<MatchRule>();

		public string CategoryDisplayNameOverride = "";
        public List<Role> AllowedRoles = null;
        public string RequiredRuleset;
		public DateTime CreationInstant;
        public string CategoryIcon;

        public RandomizerIntegration.SettingsBuilder RandoSettingsBuilder;

        #endregion

        #region Fullgame Category Flags

		public bool ChangeSavefile {
            get {
                return _changeSavefile || HasRandomizerObjective;
            }
            set {
                _changeSavefile = value;
            }
		}
		private bool _changeSavefile = false;
		public bool AllowCheatMode = false;

		#endregion

		#region Variable Members / Properties

		public MatchState State {
			get { return _state; }
			set {
                if (value == _state) return;
                _state = value;
				if (value == MatchState.Completed) CompletedInstant = SyncedClock.Now;
				if (value != MatchState.Staged && PlayerStatus.Current.CurrentMatch != this) return;
                if (value == MatchState.None || value == MatchState.Building) return;

                Outgoing.ControlPanelActionsUpdate();
                if (CNetComm.Instance.IsConnected) {
                    CNetComm.Instance.SendMatchUpdate(this);
                }
			}
        }
        private MatchState _state = MatchState.Building;
		public DateTime BeginInstant = DateTime.MinValue;

        public DateTime CompletedInstant = DateTime.MinValue;
		public MatchResult Result;

		#endregion

        public string MatchDisplayName {
			get {
                return Phases.Count == 0 ? CategoryDisplayName :
                    Phases[0].Fullgame ? FullGameDisplayName() :
					(!Phases[0].Area.ExistsLocal && !Phases[0].Area.IsRandomizer) ? string.Format(Dialog.Get("Head2Head_MapNotInstalled"), Phases[0].Area.DisplayName) :
                    string.Format(Dialog.Get("Head2Head_MatchTitle"), Phases[0].Area.DisplayName, CategoryDisplayName);

            }
		}

		private string FullGameDisplayName() {
            string levelSet = Phases[0].Area.IsVanilla ? "Celeste" : Dialog.Clean(Phases[0].Area.Data.LevelSet);
            return string.Format(Dialog.Get("Head2Head_MatchTitle"), levelSet, CategoryDisplayName);
        }

        public string CategoryDisplayName {
			get {
                if (!string.IsNullOrEmpty(CategoryDisplayNameOverride)) return CategoryDisplayNameOverride;
                if (Phases.Count > 0) return Phases[0].Title;
                return Dialog.Get("Head2Head_UntitledMatch");
			}
		}

        public bool HasRandomizerObjective {
            get {
                foreach (var ph in Phases) {
                    foreach (var obj in ph.Objectives) {
                        if (obj.ObjectiveType == MatchObjectiveType.RandomizerClear) return true;
                    }
                }
                return false;
            }
        }

		public GlobalAreaKey? VersionCheck() {
            foreach(MatchPhase ph in Phases) {
                if (!ph.Area.VersionMatchesLocal) {
                    return ph.Area;
                }
			}
            return null;
		}

        private static string GenerateMatchID() {
            return string.Format("h2hmid_{0}_{1}_{2}", PlayerID.MyIDSafe.GetHashCode(), ++localIDCounter, SyncedClock.Now.GetHashCode());
		}

        public void SetState_NoUpdate(MatchState newState) {
			_state = newState;
			if (newState == MatchState.Completed) CompletedInstant = SyncedClock.Now;
		}

        public void AssignIDs() {
            uint phaseID = 0;
            uint objectiveID = 10000;
            foreach (MatchPhase phase in Phases) {
                phase.ID = ++phaseID;
                foreach (MatchObjective ob in phase.Objectives) {
                    ob.ID = ++objectiveID;
				}
			}
		}

        public void RegisterSaveFile() {
            if (Result == null) Result = new MatchResult();
            MatchResultPlayer res = Result[PlayerID.MyIDSafe];
            if (res == null) {
                PlayerID? id = PlayerID.MyID;
                if (id == null) return;
                Result.players.Add(id.Value, new MatchResultPlayer() {
                    ID = id.Value,
                    Result = GetPlayerResultCat(id.Value),
                    SaveFile = SaveData.Instance.FileSlot,
                    FileTimeStart = PlayerStatus.Current.FileTimerAtMatchBegin,
					FileTimeEnd = PlayerStatus.Current.FileTimerAtMatchBegin,
				});
			}
			else {
                res.SaveFile = SaveData.Instance.FileSlot;
			}
            BroadcastUpdate();
		}

        public MatchPhase GetPhase(uint id) {
            foreach(MatchPhase phase in Phases) {
                if (phase.ID == id) return phase;
			}
            return null;
        }

        public MatchObjective GetObjective(uint id) {
            foreach (MatchPhase phase in Phases) {
                foreach (MatchObjective ob in phase.Objectives) {
                    if (ob.ID == id) return ob;
                }
            }
            return null;
        }

        public void BroadcastUpdate() {
            CNetComm.Instance.SendMatchUpdate(this);
            SendControlPanelUpdate();
		}

        public void SendControlPanelUpdate(string targetClientToken = "") {
			if (PlayerStatus.Current.CurrentMatch == this) {
				Outgoing.CurrentMatchStatus(this, targetClientToken);
			}
			else {
				Outgoing.OtherMatchStatus(this, targetClientToken);
			}
		}


		public ResultCategory GetPlayerResultCat(PlayerID id) {
            if (!Players.Contains(id)) return ResultCategory.NotJoined;
            if (Result != null && Result.players.ContainsKey(id)) {
                ResultCategory res = Result[id].Result;
                ResultCategory min = MinimumPlayerResult();
                return min > res ? min : res;
            }
            if (State == MatchState.InProgress) return ResultCategory.InMatch;
            if (State == MatchState.Completed) return ResultCategory.DNF;
            return ResultCategory.Joined;
		}

        private ResultCategory MinimumPlayerResult() {
            if (State == MatchState.InProgress) return ResultCategory.InMatch;
            if (State == MatchState.Completed) return ResultCategory.Completed;
            return ResultCategory.NotJoined;
        }

        public bool PlayerCanLeaveFreely(PlayerID id) {
            ResultCategory cat = GetPlayerResultCat(id);
            return cat == ResultCategory.NotJoined
                || cat == ResultCategory.Completed
                || cat == ResultCategory.DNF;
		}

        public bool CanApplyTimeAdjustments() {
            return (State == MatchState.Staged || State == MatchState.InProgress)
                && Phases.Count > 0
                && Phases[0].Objectives.Any((MatchObjective ob) => ob.ObjectiveType == MatchObjectiveType.TimeLimit);
        }

        public void PlayerDNF(DNFReason reason) {
            PlayerID id = PlayerID.MyIDSafe;
#if DEBUG
            // I need SOME way to test stuff...
            if (RoleLogic.IsDebug
                && id.Name == "corkr900"
                && reason != DNFReason.MatchCancel
                && reason != DNFReason.DropOut
                && reason != DNFReason.ChangeChannel) return;
#endif
            PlayerStatus stat = PlayerStatus.Current;
            ResultCategory cat = GetPlayerResultCat(id);
            if (cat == ResultCategory.Joined || cat == ResultCategory.InMatch) {
                if (Result == null) Result = new MatchResult();
                if (Result.players.ContainsKey(id)) {
                    Result[id].Result = ResultCategory.DNF;
                    Result[id].DNFreason = reason;
                    Result[id].FileTimeEnd = stat.FileTimerAtLastObjectiveComplete;
                    Result[id].FinalRoom = stat.CurrentRoom;
                }
                else {
                    Result[id] = new MatchResultPlayer() {
                        ID = id,
                        Result = ResultCategory.DNF,
                        DNFreason = reason,
                        FileTimeStart = stat.FileTimerAtMatchBegin,
                        FileTimeEnd = stat.FileTimerAtLastObjectiveComplete,
                        FinalRoom = stat.CurrentRoom,
                    };
                }
                CompleteIfNoRunners();
                BroadcastUpdate();
            }
        }

        public void PlayerFinished(PlayerID id, PlayerStatus stat) {
            ResultCategory cat = GetPlayerResultCat(id);
            if (cat == ResultCategory.InMatch) {
                if (Result == null) Result = new MatchResult();
                if (Result.players.ContainsKey(id)) {
                    Result[id].Result = ResultCategory.Completed;
                    Result[id].FileTimeEnd = stat.FileTimerAtLastObjectiveComplete;
                    Result[id].FinalRoom = stat.CurrentRoom;
                }
				else {
                    Result[id] = new MatchResultPlayer() {
                        ID = id,
                        Result = ResultCategory.Completed,
                        FileTimeStart = stat.FileTimerAtMatchBegin,
                        FileTimeEnd = stat.FileTimerAtLastObjectiveComplete,
                        FinalRoom = stat.CurrentRoom,
                    };
				}
                CompleteIfNoRunners();
                BroadcastUpdate();
                PlayerStatus.Current.Updated();
            }
        }

        public void CompleteIfNoRunners() {
            if (State != MatchState.InProgress) return;
            foreach (PlayerID player in Players) {
                if (GetPlayerResultCat(player) <= ResultCategory.InMatch) return;
            }
            SetState_NoUpdate(MatchState.Completed);
            PlayerStatus.Current.OnMatchEnded(this);
		}

        public void MergeDynamic(MatchDefinition newer) {
            if (newer == null) return;
            // Merge overall state
            bool isMatchCompletion = _state < MatchState.Completed && newer._state >= MatchState.Completed;
            SetState_NoUpdate((MatchState)Math.Max((int)_state, (int)newer._state));
            BeginInstant = newer.BeginInstant > BeginInstant ? newer.BeginInstant : BeginInstant;

            // Merge player list
            foreach (PlayerID id in newer.Players)
			{
                if (!Players.Contains(id)) {
                    Players.Add(id);
                }
			}

            foreach (MatchPhase ph in newer.Phases) {
                foreach (MatchObjective ob in ph.Objectives) {
                    if (ob.TimeLimitAdjustments == null) continue;
                    if (ob.ObjectiveType != MatchObjectiveType.TimeLimit) continue;
                    MatchObjective local = GetObjective(ob.ID);
                    if (local == null) {  // This shouldn't be possible but, just in case...
                        Logger.Log(LogLevel.Error, "Head2Head", "match definition does not match: " + CategoryDisplayName);
                        continue;
                    }
                    MergeObjective(local, ob);
                }
			}

            // Merge result object
            if (Result == null) Result = newer.Result;
			else {
                if (newer.Result != null) {
                    foreach (KeyValuePair<PlayerID, MatchResultPlayer> res in newer.Result.players) {
                        if (!Result.players.ContainsKey(res.Key)) {
                            Result.players.Add(res.Key, res.Value);
                            continue;
                        }
                        if (res.Value.Result > Result[res.Key].Result) {
                            Result[res.Key] = res.Value;
                        }
                        else if (res.Value.Result == Result[res.Key].Result && res.Value.Result == ResultCategory.DNF) {
                            if (res.Value.DNFreason > Result[res.Key].DNFreason) {
                                Result[res.Key].DNFreason = res.Value.DNFreason;
                            }
						}
                    }
                }
                // Sanity check - clean up illogical results
                List<PlayerID> playersToRemove = new List<PlayerID>();
                foreach (KeyValuePair<PlayerID, MatchResultPlayer> res in Result.players) {
                    if (_state >= MatchState.InProgress && res.Value.Result == ResultCategory.NotJoined) {
                        playersToRemove.Add(res.Key);
					}
					else if (_state == MatchState.Completed && res.Value.Result < ResultCategory.Completed) {
                        Result.players[res.Key].Result = ResultCategory.DNF;  // This could potentially be an issue if a desync occurs
                    }
                }
                foreach (PlayerID id in playersToRemove) {
                    Result.players.Remove(id);
				}
            }
            if (isMatchCompletion) {
                PlayerStatus.Current.OnMatchEnded(this);
			}
			SendControlPanelUpdate();
		}

        private void MergeObjective(MatchObjective local, MatchObjective update) {
            if (local.TimeLimitAdjustments == null) {
                local.TimeLimitAdjustments = update.TimeLimitAdjustments;
                return;
            }
            foreach (Tuple<PlayerID, long> t in update.TimeLimitAdjustments) {
                Tuple<PlayerID, long> localtup = local.TimeLimitAdjustments.FirstOrDefault(
                    (Tuple<PlayerID, long> tup) => tup.Item1.Equals(t.Item1));
                if (localtup == null) {
                    local.TimeLimitAdjustments.Add(t);
                }
                else if (localtup.Item2 != t.Item2) {
                    local.TimeLimitAdjustments.Remove(localtup);
                    local.TimeLimitAdjustments.Add(t);
                }
            }
        }

        public bool AllPhasesExistLocal() {
            foreach(MatchPhase p in Phases) {
                if (!p.Area.IsValidInstalledMap) return false;
			}
            return true;
        }

		internal long GetPlayerTimer(PlayerID pla) {
            return GetPlayerResultCat(pla) switch {
                ResultCategory.Completed => Result?[pla]?.FileTimeTotal ?? 0,
				ResultCategory.DNF => 999999999999999,
                ResultCategory.InMatch => Head2HeadModule.knownPlayers.ContainsKey(pla)
                    ? Head2HeadModule.knownPlayers[pla].CurrentMatchTimer()
                    : 0,
				_ => 0
            };
		}
	}

    public class MatchPhase {
        public StandardCategory category;
        public uint ID;
        public bool Fullgame = false;
        public string LevelSet = "";
        public int Order = 0;
        public GlobalAreaKey Area;
        public List<MatchObjective> Objectives = new List<MatchObjective>();

        public string Title {
            get {
                return Util.TranslatedCategoryName(category);
            }
        }

        public bool HasTimeLimit {
            get {
                if (Objectives == null) return false;
                foreach (MatchObjective obj in Objectives) {
                    if (obj.ObjectiveType == MatchObjectiveType.TimeLimit) return true;
                }
                return false;
            }
        }
	}

    public class MatchObjective {
        public uint ID;
        public MatchObjectiveType ObjectiveType;
        public int CollectableGoal = -1;
        public long TimeLimit = 0;
        public string CustomTypeKey;
        public string CustomDescription;
        public AreaMode Side = AreaMode.Normal;
        public List<Tuple<PlayerID, long>> TimeLimitAdjustments = new List<Tuple<PlayerID, long>>();

        public string Description {
			get {
                if (!string.IsNullOrEmpty(CustomDescription)) return CustomDescription;
                switch (ObjectiveType) {
                    default:
                        return Dialog.Get("Head2Head_ObjectiveDescription_" + ObjectiveType.ToString());
                    case MatchObjectiveType.CustomObjective:
                        return Dialog.Get("Head2Head_ObjectiveDescription_CustomObjective");
                    case MatchObjectiveType.CustomCollectable:
                        return string.Format(Dialog.Get("Head2Head_ObjectiveDescription_CustomCollectable"),
                            CustomCollectables.GetDisplayName(CustomTypeKey), CollectableGoal);
                    case MatchObjectiveType.Strawberries:
					case MatchObjectiveType.MoonBerry:
					case MatchObjectiveType.Keys:
						return string.Format(Dialog.Get("Head2Head_ObjectiveDescription_" + ObjectiveType.ToString()), CollectableGoal);
                    case MatchObjectiveType.TimeLimit:
                        return string.Format(Dialog.Get("Head2Head_ObjectiveDescription_TimeLimit"),
                            Dialog.FileTime(AdjustedTimeLimit(PlayerID.MyIDSafe)));
                    case MatchObjectiveType.EnterRoom:
                        return string.Format(Dialog.Get("Head2Head_ObjectiveDescription_EnterRoom"), CustomTypeKey);
                }
			}
		}

		public string Label {
            get {
				return !string.IsNullOrEmpty(CustomDescription)
                    ? CustomDescription
                    : Util.TranslatedObjectiveLabel(ObjectiveType);
			}
		}

		public long AdjustedTimeLimit(PlayerID id) {
            return TimeLimit + GetAdjustment(id);
		}

        public long GetAdjustment(PlayerID id) {
            if (TimeLimitAdjustments != null) {
                foreach (Tuple<PlayerID, long> t in TimeLimitAdjustments) {
                    if (t.Item1.Equals(id)) {
                        return t.Item2;
                    }
                }
            }
            return 0;
        }

        public void SetAdjustment(PlayerID id, long adj) {
            if (TimeLimitAdjustments == null) TimeLimitAdjustments = new List<Tuple<PlayerID, long>>();
            Tuple<PlayerID, long> localtup = TimeLimitAdjustments.FirstOrDefault(
                    (Tuple<PlayerID, long> tup) => tup.Item1.Equals(id));
            if (localtup == null) {
                TimeLimitAdjustments.Add(new Tuple<PlayerID, long>(id, adj));
            }
            else if (localtup.Item2 != adj) {
                TimeLimitAdjustments.Remove(localtup);
                TimeLimitAdjustments.Add(new Tuple<PlayerID, long>(id, adj));
            }
        }

        public static MatchObjectiveType GetTypeForStrawberry(Strawberry s) {
            if (s.Golden) {
                DynamicData dd = DynamicData.For(s);
                if (dd.Data.ContainsKey("IsWingedGolden")) {
                    return MatchObjectiveType.WingedGoldenStrawberry;
                }
                else return MatchObjectiveType.GoldenStrawberry;
            }
            else if (s.Moon) {
                return MatchObjectiveType.MoonBerry;
            }
            else return MatchObjectiveType.Strawberries;
        }

		public MTexture GetIcon() {
            string path = GetIconPath();
			return string.IsNullOrEmpty(path) ? null : GFX.Gui.GetOrDefault(path, null);
		}

        public string GetIconPath() {
            return ObjectiveType switch {
				MatchObjectiveType.ChapterComplete => "Head2Head/Categories/Clear",
				MatchObjectiveType.HeartCollect => "Head2Head/Categories/BlueHeart",
				MatchObjectiveType.CassetteCollect => "Head2Head/Categories/CassetteGrab",
				MatchObjectiveType.Strawberries => "Head2Head/Categories/ARB",
				//MatchObjectiveType.Keys => "Head2Head/Categories/Custom",
				MatchObjectiveType.MoonBerry => "Head2Head/Categories/MoonBerry",
				MatchObjectiveType.GoldenStrawberry => "Head2Head/Categories/Golden",
				MatchObjectiveType.WingedGoldenStrawberry => "Head2Head/Categories/WingedGolden",
				//MatchObjectiveType.Flag => "Head2Head/Categories/Custom",
				//MatchObjectiveType.EnterRoom => "Head2Head/Categories/Custom",
				MatchObjectiveType.TimeLimit => "Head2Head/Categories/TimeLimit",
				//MatchObjectiveType.CustomCollectable => "Head2Head/Categories/Custom",
				//MatchObjectiveType.CustomObjective => "Head2Head/Categories/Custom",
				//MatchObjectiveType.UnlockChapter => "Head2Head/Categories/Custom",
				MatchObjectiveType.RandomizerClear => "Head2Head/Categories/Clear",
				_ => null,
			};
		}
	}

	public enum MatchObjectiveType {
        // Standard
        ChapterComplete = 0,
        HeartCollect = 1,
        CassetteCollect = 2,
        Strawberries = 3,
        Keys = 4,
        MoonBerry = 5,
        GoldenStrawberry = 6,
        WingedGoldenStrawberry = 7,
        Flag = 8,
        EnterRoom = 9,
        TimeLimit = 10,
		// Fullgame
		UnlockChapter = 500,
		// Special
		RandomizerClear = 900,
		// Custom
		CustomObjective = 1000,
		CustomCollectable = 1001,
    }

    /// <summary>
    /// Discrete states a Match can be in.
    /// Enum value defines priority when merging (larger number = higher priority)
    /// </summary>
    public enum MatchState {
        None = 0,
        Building = 1,
        Staged = 2,
        InProgress = 3,
        Completed = 4,
    }

    /// <summary>
    /// Restrictions that can be placed on a match
    /// </summary>
    public enum MatchRule {
        NoGrabbing,
    }

    public static class MatchExtensions {
        public static MatchDefinition ReadMatch(this CelesteNetBinaryReader reader) {
            if (!reader.ReadBoolean()) return null;
            MatchDefinition d = new MatchDefinition();
            d.MatchID = reader.ReadString();
            d.Owner = reader.ReadPlayerID();
            d.CreationInstant = reader.ReadDateTime();
            d.SetState_NoUpdate((MatchState)Enum.Parse(typeof(MatchState), reader.ReadString()));
            d.CategoryDisplayNameOverride = reader.ReadString();
            d.ChangeSavefile = reader.ReadBoolean();
            d.AllowCheatMode = reader.ReadBoolean();
            d.BeginInstant = reader.ReadDateTime();
			d.CompletedInstant = reader.ReadDateTime();
			d.RequiredRuleset = reader.ReadString();
            d.CategoryIcon = reader.ReadString();

			int numAllowedRoles = reader.ReadInt32();
            d.AllowedRoles = new List<Role>(numAllowedRoles);
            for (int i = 0; i < numAllowedRoles; i++) {
                d.AllowedRoles.Add((Role)Enum.Parse(typeof(Role), reader.ReadString()));
            }

			bool hasRandoOptions = reader.ReadBoolean();
			if (hasRandoOptions) {
                d.RandoSettingsBuilder = reader.ReadRandoSettings();
			}
			int numPlayers = reader.ReadInt32();
            d.Players.Capacity = numPlayers;
            for (int i = 0; i < numPlayers; i++) {
                d.Players.Add(reader.ReadPlayerID());
			}
			int numPhases = reader.ReadInt32();
            d.Phases.Capacity = numPhases;
            for (int i = 0; i < numPhases; i++) {
                d.Phases.Add(reader.ReadMatchPhase());
            }
            bool hasResult = reader.ReadBoolean();
            if (hasResult) {
                d.Result = reader.ReadMatchResult();
			}
			int numRules = reader.ReadInt32();
			d.Rules.Capacity = numRules;
			for (int i = 0; i < numRules; i++) {
				d.Rules.Add((MatchRule)Enum.Parse(typeof(MatchRule), reader.ReadString()));
			}
			return d;
        }

        public static void Write(this CelesteNetBinaryWriter writer, MatchDefinition m) {
            if (m == null) {
                writer.Write(false);
                return;
            }
            else writer.Write(true);
            writer.Write(m.MatchID ?? "");
            writer.Write(m.Owner);
            writer.Write(m.CreationInstant);
            writer.Write(m.State.ToString() ?? "");
            writer.Write(m.CategoryDisplayNameOverride ?? "");
            writer.Write(m.ChangeSavefile);
            writer.Write(m.AllowCheatMode);
            writer.Write(m.BeginInstant);
			writer.Write(m.CompletedInstant);
			writer.Write(m.RequiredRuleset ?? "");
            writer.Write(m.CategoryIcon ?? "");

            writer.Write(m.AllowedRoles?.Count ?? 0);
            for (int i = 0; i < (m.AllowedRoles?.Count ?? 0); i++) {
                writer.Write(m.AllowedRoles[i].ToString());
            }

            writer.Write(m.RandoSettingsBuilder != null);
            if (m.RandoSettingsBuilder != null) {
                writer.Write(m.RandoSettingsBuilder);
            }

            writer.Write(m.Players.Count);
            foreach(PlayerID pid in m.Players) {
                writer.Write(pid);
            }

            writer.Write(m.Phases.Count);
            foreach (MatchPhase ph in m.Phases) {
                writer.Write(ph);
            }

            if (m.Result == null) {
                writer.Write(false);
			}
			else {
                writer.Write(true);
                writer.Write(m.Result);
			}

            writer.Write(m.Rules.Count);
            for (int i = 0; i < m.Rules.Count; i++) {
                writer.Write(m.Rules[i].ToString());
            }
        }

        public static MatchPhase ReadMatchPhase(this CelesteNetBinaryReader reader) {
            MatchPhase p = new MatchPhase();
            p.category = (StandardCategory)Enum.Parse(typeof(StandardCategory), reader.ReadString());
            p.ID = reader.ReadUInt32();
            p.Fullgame = reader.ReadBoolean();
            p.LevelSet = reader.ReadString();
            p.Order = reader.ReadInt32();
            p.Area = reader.ReadAreaKey();
            int numObjectives = reader.ReadInt32();
            p.Objectives.Capacity = numObjectives;
            for (int i = 0; i < numObjectives; i++) {
                p.Objectives.Add(reader.ReadMatchObjective());
			}
            return p;
        }

        public static void Write(this CelesteNetBinaryWriter writer, MatchPhase mp) {
            writer.Write(mp.category.ToString() ?? "");
            writer.Write(mp.ID);
            writer.Write(mp.Fullgame);
            writer.Write(mp.LevelSet);
            writer.Write(mp.Order);
            writer.Write(mp.Area);
            writer.Write(mp.Objectives.Count);
            foreach (MatchObjective obj in mp.Objectives) {
                writer.Write(obj);
            }
        }

        public static MatchObjective ReadMatchObjective(this CelesteNetBinaryReader reader) {
            MatchObjective mo = new MatchObjective();
            mo.ID = reader.ReadUInt32();
            mo.ObjectiveType = (MatchObjectiveType)Enum.Parse(typeof(MatchObjectiveType), reader.ReadString());
            mo.CollectableGoal = reader.ReadInt32();
            mo.TimeLimit = reader.ReadInt64();
            mo.CustomTypeKey = reader.ReadString();
            mo.CustomDescription = reader.ReadString();

            int count = reader.ReadInt32();
            List<Tuple<PlayerID, long>> list = new List<Tuple<PlayerID, long>>(count);
            for (int i = 0; i < count; i++) {
                PlayerID id = reader.ReadPlayerID();
                long time = reader.ReadInt64();
                list.Add(new Tuple<PlayerID, long>(id, time));
			}
            mo.TimeLimitAdjustments = list;

            return mo;
        }

        public static void Write(this CelesteNetBinaryWriter writer, MatchObjective mo) {
            writer.Write(mo.ID);
            writer.Write(mo.ObjectiveType.ToString() ?? "");
            writer.Write(mo.CollectableGoal);
            writer.Write(mo.TimeLimit);
            writer.Write(mo.CustomTypeKey ?? "");
            writer.Write(mo.CustomDescription ?? "");
            writer.Write(mo.TimeLimitAdjustments?.Count ?? 0);
            if (mo.TimeLimitAdjustments != null) {
                foreach (Tuple<PlayerID, long> t in mo.TimeLimitAdjustments) {
                    writer.Write(t.Item1);
                    writer.Write(t.Item2);
				}
			}
        }
    }
}
