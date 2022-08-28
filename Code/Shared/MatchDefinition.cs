using Celeste.Mod.Head2Head.IO;
using Celeste.Mod.CelesteNet;
using Celeste.Mod.CelesteNet.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.Shared {

    public class MatchDefinition {
        private static uint localIDCounter = 1;

        #region Invariable Members / Properties

        public string MatchID = GenerateMatchID();
        public PlayerID Owner = PlayerID.MyIDSafe;

        public List<PlayerID> Players = new List<PlayerID>();
        public List<MatchPhase> Phases = new List<MatchPhase>();

        public string DisplayNameOverride = "";
        public string RequiredRole = "";
        public DateTime CreationInstant;

		#endregion

		#region Currently Unused

		public bool CanParticipantsStart = true;
        public bool OpenEntry = true;
        public bool RequireNewSaveFile = false;
        public bool AllowCheatMode = true;
        public bool AllowDebugView = true;
        public bool AllowDebugTeleport = false;

        #endregion

        #region Variable Members / Properties

        public MatchState State {
			get { return _state; }
			set {
                if (value == _state) return;
                _state = value;
                if (value != MatchState.Staged && PlayerStatus.Current.CurrentMatch != this) return;
                if (value == MatchState.None || value == MatchState.Building) return;
                if (!CNetComm.Instance.IsConnected) return;
                CNetComm.Instance.SendMatchUpdate(this);
			}
        }
        private MatchState _state = MatchState.Building;
        public DateTime BeginInstant = DateTime.MinValue;
        public MatchResult Result;

		#endregion

        public string DisplayName {
			get {
                if (!string.IsNullOrEmpty(DisplayNameOverride)) return DisplayNameOverride;
                if (Phases.Count > 0) return Phases[0].Title;
                return Dialog.Get("Head2Head_UntitledMatch");
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
            return string.Format("h2hmid_{0}_{1}_{2}", PlayerID.MyIDSafe.GetHashCode(), ++localIDCounter, DateTime.Now.GetHashCode());
		}

        public void SetState_NoUpdate(MatchState newState) {
            _state = newState;
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

        public void PlayerDNF() {
            PlayerID id = PlayerID.MyIDSafe;
            PlayerStatus stat = PlayerStatus.Current;
            ResultCategory cat = GetPlayerResultCat(id);
            if (cat == ResultCategory.Joined || cat == ResultCategory.InMatch) {
                if (Result == null) Result = new MatchResult();
                if (Result.players.ContainsKey(id)) {
                    Result[id].Result = ResultCategory.DNF;
                    Result[id].FileTimeEnd = stat.FileTimerAtLastObjectiveComplete;
                }
                else {
                    Result[id] = new MatchResultPlayer() {
                        ID = id,
                        Result = ResultCategory.DNF,
                        FileTimeStart = stat.FileTimerAtMatchBegin,
                        FileTimeEnd = stat.FileTimerAtLastObjectiveComplete,
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
				}
				else {
                    Result[id] = new MatchResultPlayer() {
                        ID = id,
                        Result = ResultCategory.Completed,
                        FileTimeStart = stat.FileTimerAtMatchBegin,
                        FileTimeEnd = stat.FileTimerAtLastObjectiveComplete,
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
		}

        public void MergeDynamic(MatchDefinition newer) {
            if (newer == null) return;
            // merge overall state
            _state = (MatchState)Math.Max((int)_state, (int)newer._state);
            BeginInstant = newer.BeginInstant > BeginInstant ? newer.BeginInstant : BeginInstant;

            // merge player list
            foreach (PlayerID id in newer.Players)
			{
                if (!Players.Contains(id)) {
                    Players.Add(id);
                }
			}

            // merge result object
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
        }
    }

    public class MatchPhase {
        public StandardCategory category;
        public uint ID;
        public int Order = 0;
        public GlobalAreaKey Area;
        public List<MatchObjective> Objectives = new List<MatchObjective>();

		public string Title { 
            get {
				switch (category) {
                    default:
                        return string.Format(Dialog.Get("Head2Head_MatchTitle"), Area.DisplayName, Util.TranslatedCategoryName(category));
                    case StandardCategory.OneThirdBerries:
                    case StandardCategory.OneFifthBerries:
                        int berries = Objectives.Find((MatchObjective o) => o.ObjectiveType == MatchObjectiveType.Strawberries)?.BerryGoal ?? 0;
                        return string.Format(Dialog.Get("Head2Head_MatchTitle_BerryCount"), Area.DisplayName, berries);
                }
            }
        }
	}

    public class MatchObjective {
        public uint ID;
        public MatchObjectiveType ObjectiveType;
        public int BerryGoal = -1;
	}

    public enum MatchObjectiveType {
        ChapterComplete,
        HeartCollect,
        CassetteCollect,
        Strawberries,
        MoonBerry,
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

    public static class MatchExtensions {
        public static MatchDefinition ReadMatch(this CelesteNetBinaryReader reader) {
            if (!reader.ReadBoolean()) return null;
            MatchDefinition d = new MatchDefinition();
            d.MatchID = reader.ReadString();
            d.Owner = reader.ReadPlayerID();
            d.CreationInstant = reader.ReadDateTime();
            d.SetState_NoUpdate((MatchState)Enum.Parse(typeof(MatchState), reader.ReadString()));
            d.DisplayNameOverride = reader.ReadString();
            d.RequiredRole = reader.ReadString();
            d.CanParticipantsStart = reader.ReadBoolean();
            d.OpenEntry = reader.ReadBoolean();
            d.RequireNewSaveFile = reader.ReadBoolean();
            d.AllowCheatMode = reader.ReadBoolean();
            d.AllowDebugView = reader.ReadBoolean();
            d.AllowDebugTeleport = reader.ReadBoolean();
            d.BeginInstant = reader.ReadDateTime();
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
            return d;
        }

        public static void Write(this CelesteNetBinaryWriter writer, MatchDefinition m) {
            if (m == null) {
                writer.Write(false);
                return;
            }
            else writer.Write(true);
            writer.Write(m.MatchID);
            writer.Write(m.Owner);
            writer.Write(m.CreationInstant);
            writer.Write(m.State.ToString());
            writer.Write(m.DisplayNameOverride);
            writer.Write(m.RequiredRole);
            writer.Write(m.CanParticipantsStart);
            writer.Write(m.OpenEntry);
            writer.Write(m.RequireNewSaveFile);
            writer.Write(m.AllowCheatMode);
            writer.Write(m.AllowDebugView);
            writer.Write(m.AllowDebugTeleport);
            writer.Write(m.BeginInstant);
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
        }

        public static MatchPhase ReadMatchPhase(this CelesteNetBinaryReader reader) {
            MatchPhase p = new MatchPhase();
            p.category = (StandardCategory)Enum.Parse(typeof(StandardCategory), reader.ReadString());
            p.ID = reader.ReadUInt32();
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
            writer.Write(mp.category.ToString());
            writer.Write(mp.ID);
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
            mo.BerryGoal = reader.ReadInt32();
            return mo;
        }

        public static void Write(this CelesteNetBinaryWriter writer, MatchObjective mo) {
            writer.Write(mo.ID);
            writer.Write(mo.ObjectiveType.ToString());
            writer.Write(mo.BerryGoal);
        }
    }
}
