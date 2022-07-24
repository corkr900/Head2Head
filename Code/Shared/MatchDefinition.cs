﻿using Celeste.Mod.Head2Head.IO;
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
        public DateTime BeginInstant;
        public MatchResult Result;

		#endregion

        public string DisplayName {
			get {
                if (!string.IsNullOrEmpty(DisplayNameOverride)) return DisplayNameOverride;
                if (Phases.Count > 0) return Phases[0].Title;
                return Dialog.Get("Head2Head_UntitledMatch");
			}
		}

		private static string GenerateMatchID() {
            return string.Format("p_{0}_c_{1}", PlayerID.MyIDSafe.GetHashCode(), ++localIDCounter);
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
            if (Result != null  && Result.players.ContainsKey(id)) return Result[id].Result;
            if (State == MatchState.InProgress) return ResultCategory.InMatch;
            if (State == MatchState.Completed) return ResultCategory.DNF;
            return ResultCategory.Joined;
		}

        public void PlayerFinished(PlayerID id, PlayerStatus stat) {
            // TODO this is extremely likely to cause desync & race condition issues.
            // Find a better pattern or implement robust state merging.
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
                if (id.Equals(PlayerID.MyIDSafe)) BroadcastUpdate();
            }
        }
    }

    public class MatchPhase {
        public StandardCategory category;
        public uint ID;
        public int Order = 0;
        public GlobalAreaKey Area;
        public List<MatchObjective> Objectives = new List<MatchObjective>();

		public string Title { get { return string.Format("{0} ({1})", Area.DisplayName, Util.TranslatedCategoryName(category)); } }
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
    }

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
            d.SetState_NoUpdate((MatchState)Enum.Parse(typeof(MatchState), reader.ReadString()));
            d.DisplayNameOverride = reader.ReadString();
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
            writer.Write(m.State.ToString());
            writer.Write(m.DisplayNameOverride);
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
