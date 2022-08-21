using Celeste.Editor;
using Celeste.Mod.Head2Head.IO;
using Celeste.Mod.CelesteNet;
using Celeste.Mod.CelesteNet.DataTypes;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Monocle;
using Celeste.Mod.Head2Head.Control;

namespace Celeste.Mod.Head2Head.Shared {
	public class PlayerStatus {
		public static PlayerStatus Current {
			get {
				if (_current == null) {
					_current = new PlayerStatus() {
						CurrentArea = GlobalAreaKey.Overworld,
						CurrentRoom = "",
						CurrentFileTimer = SaveData.Instance?.Time ?? 0,
					};
				}
				return _current;
			}
			set { _current = value; }
		}
		private static PlayerStatus _current;

		public delegate void OnMatchPhaseCompletedHandler(OnMatchPhaseCompletedArgs args);
		public static event OnMatchPhaseCompletedHandler OnMatchPhaseCompleted;
		public class OnMatchPhaseCompletedArgs {
			public OnMatchPhaseCompletedArgs(MatchDefinition def, MatchObjective lastOb, MatchPhase lastPh, MatchPhase nextPh, bool matchComp) {
				MatchDef = def;
				CompletedObjective = lastOb;
				CompletedPhase = lastPh;
				NextPhase = nextPh;
				MatchCompleted = matchComp;
			}
			public readonly MatchDefinition MatchDef;
			public readonly MatchObjective CompletedObjective;
			public readonly MatchPhase CompletedPhase;
			public readonly MatchPhase NextPhase;
			public readonly bool MatchCompleted;
		}

		public MatchDefinition CurrentMatch {
			get {
				return string.IsNullOrEmpty(CurrentMatchID) ? null
					: !Head2HeadModule.knownMatches.ContainsKey(CurrentMatchID) ? null
					: Head2HeadModule.knownMatches[CurrentMatchID];
			}
			internal set {
				if (value == null) {
					CurrentMatchID = null;
					return;
				}
				if (!Head2HeadModule.knownMatches.ContainsKey(value.MatchID)) Head2HeadModule.knownMatches.Add(value.MatchID, value);
				CurrentMatchID = value.MatchID;
			}
		}
		public bool IsInMatch(bool includeJoined, PlayerID? id = null) {
			if (string.IsNullOrEmpty(CurrentMatchID)) return false;
			MatchDefinition def = CurrentMatch;
			if (def == null) return true;  // Remote player but we have forgotten their match. This is not likely to ever get hit
			ResultCategory cat = def.GetPlayerResultCat(id ?? PlayerID.MyIDSafe);
			return cat == ResultCategory.InMatch || (includeJoined && cat == ResultCategory.Joined);
		}

		public string CurrentMatchID { get; private set; }
		public GlobalAreaKey CurrentArea { get; internal set; }
		public string CurrentRoom { get; internal set; }
		public string LastCheckpoint { get; internal set; } = null;
		public bool IsInDebug { get; internal set; }
		public long CurrentFileTimer { get; internal set; }
		public long FileTimerAtMatchBegin { get; internal set; }
		public long FileTimerAtLastObjectiveComplete { get; internal set; }
		public List<H2HMatchPhaseState> phases { get; internal set; } = new List<H2HMatchPhaseState>();
		public List<H2HMatchObjectiveState> objectives { get; internal set; } = new List<H2HMatchObjectiveState>();
		public List<Tuple<GlobalAreaKey, string>> reachedCheckpoints = new List<Tuple<GlobalAreaKey, string>>();

		/// <summary>
		/// This is not sent over the network. It is to be set on receipt of an update.
		/// </summary>
		public DateTime ReceivedAt = DateTime.Now;
		public MatchState MatchState {
			get {
				return CurrentMatch?.State ?? MatchState.None;
			}
		}

		// Lifecycle events
		public void ChapterEntered(GlobalAreaKey area, Session session) {
			CurrentArea = area;
			CurrentRoom = session.Level;
			LastCheckpoint = session.LevelData.HasCheckpoint ? session.LevelData.Name : null;
			Updated();
		}
		public void RoomEntered(Level level, LevelData next, Vector2 direction) {
			CurrentRoom = next.Name;
			if (next.HasCheckpoint) {
				LastCheckpoint = next.Name;
				GlobalAreaKey area = new GlobalAreaKey(level.Session.Area);
				int index = reachedCheckpoints.FindIndex((Tuple<GlobalAreaKey, string> tpred) => {
					return tpred.Item1.Equals(area) && tpred.Item2 == LastCheckpoint;
				});
				if (index < 0 || index >= reachedCheckpoints.Count) {
					reachedCheckpoints.Add(new Tuple<GlobalAreaKey, string>(area, LastCheckpoint));
				}
				ActionLogger.EnteringRoom();
			}
			Updated();
		}
		internal void DebugOpened(MapEditor screen, GlobalAreaKey globalAreaKey, bool reloadMapData) {
			IsInDebug = true;
			Updated();
		}
		internal void OnLevelLoaderStart(LevelLoader loader) {
			if (IsInDebug) {
				IsInDebug = false;
				Updated();
			}
		}
		internal void DebugTeleport(MapEditor screen, LevelTemplate level, Vector2 at) {
			IsInDebug = false;
			Updated();
		}
		public void ChapterExited(LevelExit.Mode mode, Session session) {
			CurrentArea = GlobalAreaKey.Overworld;
			CurrentRoom = "";
			Updated();
		}

		public void MatchStaged(MatchDefinition def) {
			if (CurrentMatch?.MatchID != def.MatchID) {
				CurrentMatch = def;
				phases.Clear();
				objectives.Clear();
				Updated();
			}
		}
		public void MatchJoined() {
			phases.Clear();
			objectives.Clear();
			Updated();
		}
		public void MatchStarted() {
			phases.Clear();
			objectives.Clear();
			reachedCheckpoints.Clear();
			FileTimerAtMatchBegin = SaveData.Instance.Time;
			Updated();
		}
		public void MatchReset() {
			phases.Clear();
			objectives.Clear();
			CurrentMatch = null;
			Updated();
		}

		public void Cleanup() {
			phases.Clear();
			objectives.Clear();
			CurrentMatch = null;
			Updated();
		}

		// CHECKING OFF OBJECTIVES/PHASES

		public void ChapterCompleted(GlobalAreaKey area) {
			MatchObjective ob = FindObjective(MatchObjectiveType.ChapterComplete, area, false);
			if (ob != null && MarkObjectiveComplete(ob)) Updated();
		}
		public void HeartCollected(GlobalAreaKey area) {
			MatchObjective ob = FindObjective(MatchObjectiveType.HeartCollect, area, false);
			if (ob != null && MarkObjectiveComplete(ob)) Updated();
		}
		public void CassetteCollected(GlobalAreaKey area) {
			MatchObjective ob = FindObjective(MatchObjectiveType.CassetteCollect, area, false);
			if (ob != null && MarkObjectiveComplete(ob)) Updated();
		}

		public void StrawberryCollected(GlobalAreaKey area, Strawberry strawb) {
			bool updated = false;
			MatchObjectiveType objtype = strawb.Moon ? MatchObjectiveType.MoonBerry : MatchObjectiveType.Strawberries;
			MatchObjective ob = FindObjective(objtype, area, false);
			if (ob == null) return;
			int stateIndex = objectives.FindIndex((H2HMatchObjectiveState s) => s.ObjectiveID == ob.ID);
			if (stateIndex < 0) {
				objectives.Add(new H2HMatchObjectiveState() {
					ObjectiveID = ob.ID,
					CollectedStrawbs = new List<EntityID>() {
						strawb.ID,
					},
				});
				stateIndex = objectives.Count - 1;
				updated |= true;
			}
			else {
				if (objectives[stateIndex].CollectedStrawbs == null) {
					Engine.Commands.Log("This is weird... a strawberries objective doesn't have a list of strawberries?");
					return;
				}
				if (objectives[stateIndex].CollectedStrawbs.Contains(strawb.ID)) return;  // already collected
				objectives[stateIndex].CollectedStrawbs.Add(strawb.ID);
				updated |= true;
			}
			if (objectives[stateIndex].CollectedStrawbs.Count >= ob.BerryGoal) {
				updated |= MarkObjectiveComplete(ob);
			}
			if (updated) Updated();
		}

		// objective help

		internal int CurrentPhase() {
			if (CurrentMatch == null) return -1;
			if (!IsInMatch(false)) return -1;
			int max = 0;
			foreach (H2HMatchPhaseState s in phases) {
				int order = CurrentMatch.GetPhase(s.PhaseID)?.Order ?? -1;
				if (s.Completed && order > max) {
					max = order;
				}
			}
			return max;
		}

		private MatchObjective FindObjective(MatchObjectiveType type, GlobalAreaKey area, bool includeFinished = false) {
			if (CurrentMatch == null) return null;
			int currentp = CurrentPhase();
			foreach (MatchPhase ph in CurrentMatch.Phases) {
				if ((ph.Order == currentp || (includeFinished && ph.Order <= currentp))
					&& (ph.Area.Equals(area))) {
					foreach (MatchObjective ob in ph.Objectives) {
						if (ob.ObjectiveType == type) {
							return ob;
						}
					}
				}
			}
			return null;
		}

		public bool IsObjectiveComplete(uint objid) {
			foreach (H2HMatchObjectiveState st in objectives) {
				if (st.ObjectiveID == objid) return st.Completed;
			}
			return false;
		}

		public bool IsPhaseComplete(uint phid) {
			foreach (H2HMatchPhaseState st in phases) {
				if (st.PhaseID == phid) return st.Completed;
			}
			return false;
		}

		private bool MarkObjectiveComplete(MatchObjective ob) {
			if (!IsInMatch(false)) return false;
			bool found = false;
			for (int i = 0; i < objectives.Count; i++) {
				H2HMatchObjectiveState st = objectives[i];
				if (st.ObjectiveID == ob.ID) {
					if (st.Completed) {
						return false;
					}
					else {
						found = true;
						st.Completed = true;
						objectives[i] = st;
						FileTimerAtLastObjectiveComplete = SaveData.Instance.Time;
						break;
					}
				}
			}
			if (!found) {
				objectives.Add(new H2HMatchObjectiveState() {
					ObjectiveID = ob.ID,
					Completed = true,
				});
				FileTimerAtLastObjectiveComplete = SaveData.Instance.Time;
			}
			TryMarkPhaseComplete(ob);
			return true;
		}

		private bool TryMarkPhaseComplete(MatchObjective lastOB) {
			// TODO enforce phase order
			bool anychanges = false;
			MatchPhase lastPhase = null;
			foreach (MatchPhase ph in CurrentMatch.Phases) {
				if (IsPhaseComplete(ph.ID)) continue;
				bool complete = true;
				foreach (MatchObjective ob in ph.Objectives) {
					if (!IsObjectiveComplete(ob.ID)) {
						complete = false;
						break;
					}
				}
				if (complete) {
					anychanges = true;
					lastPhase = ph;
					bool found = false;
					for (int i = 0; i < phases.Count; i++) {
						H2HMatchPhaseState st = phases[i];
						if (st.PhaseID == ph.ID) {
							if (st.Completed) {
								return false;
							}
							else {
								found = true;
								st.Completed = true;
								phases[i] = st;
								break;
							}
						}
					}
					if (!found) {
						phases.Add(new H2HMatchPhaseState() {
							PhaseID = ph.ID,
							Completed = true,
						});
					}
				}
			}
			if (anychanges) {
				bool matchFinished = true;
				MatchPhase nextPhase = null;
				foreach (MatchPhase ph in CurrentMatch.Phases) {
					if (!IsPhaseComplete(ph.ID)) {
						matchFinished = false;
						nextPhase = ph;
						break;
					}
				}
				if (matchFinished) {  // All phases are complete
					CurrentMatch.PlayerFinished(PlayerID.MyIDSafe, this);
				}
				OnMatchPhaseCompleted?.Invoke(new OnMatchPhaseCompletedArgs(
					CurrentMatch,
					lastOB,
					lastPhase,
					nextPhase,
					matchFinished
				));
				Updated();
			}
			return anychanges;
		}

		internal void Updated() {
			CurrentFileTimer = SaveData.Instance?.Time ?? 0;
			if (this == Current && CNetComm.Instance.IsConnected) {
				CNetComm.Instance.SendPlayerStatus(this);
			}
		}

		public bool CanStageMatch() {
			if (Current != this) return false;
			if (string.IsNullOrEmpty(CurrentMatchID)) return true;
			MatchDefinition def = CurrentMatch;
			ResultCategory cat = def.GetPlayerResultCat(PlayerID.MyIDSafe);
			return cat != ResultCategory.Joined && cat != ResultCategory.InMatch;
		}

		public int GetMatchSaveFile() {
			MatchDefinition def = CurrentMatch;
			if (def == null) return int.MinValue;
			MatchResultPlayer res = def.Result?[PlayerID.MyIDSafe];
			if (res == null) return int.MinValue;
			return res.SaveFile;
		}

		public void Merge(PlayerStatus other) {
			phases = other.phases;
			objectives = other.objectives;
			CurrentFileTimer = other.CurrentFileTimer;
			FileTimerAtMatchBegin = other.FileTimerAtMatchBegin;
			FileTimerAtLastObjectiveComplete = other.FileTimerAtLastObjectiveComplete;
		}
	}

	public struct H2HMatchPhaseState {
		public uint PhaseID;
		public bool Completed;
	}

	public struct H2HMatchObjectiveState {
		public uint ObjectiveID;
		public List<EntityID> CollectedStrawbs;
		public bool Completed;
	}

	public enum PlayerStateCategory {
		None,
		Idle,
		Joined,
		InMatch,
		FinishedMatch,
	}

	public static class PlayerStateExt {
		public static PlayerStatus ReadPlayerState(this CelesteNetBinaryReader r) {
			if (!r.ReadBoolean()) return null;
			PlayerStatus pms = new PlayerStatus();
			pms.IsInDebug = r.ReadBoolean();
			pms.CurrentMatch = r.ReadMatch();
			pms.CurrentArea = r.ReadAreaKey();
			pms.CurrentRoom = r.ReadString();
			pms.LastCheckpoint = r.ReadString();
			if (string.IsNullOrEmpty(pms.LastCheckpoint)) pms.LastCheckpoint = null;
			pms.CurrentFileTimer = r.ReadInt64();
			pms.FileTimerAtMatchBegin = r.ReadInt64();
			pms.FileTimerAtLastObjectiveComplete = r.ReadInt64();
			int numPhases = r.ReadInt32();
			for (int i = 0; i < numPhases; i++) {
				pms.phases.Add(r.ReadMatchPhaseState());
			}
			int numObjectives = r.ReadInt32();
			for (int i = 0; i < numObjectives; i++) {
				pms.objectives.Add(r.ReadMatchObjectiveState());
			}
			return pms;
		}

		public static void Write(this CelesteNetBinaryWriter w, PlayerStatus s) {
			if (s == null) {
				w.Write(false);
				return;
			}
			w.Write(true);
			w.Write(s.IsInDebug);
			w.Write(s.CurrentMatch);
			w.Write(s.CurrentArea);
			w.Write(s.CurrentRoom);
			w.Write(s.LastCheckpoint ?? "");
			w.Write(s.CurrentFileTimer);
			w.Write(s.FileTimerAtMatchBegin);
			w.Write(s.FileTimerAtLastObjectiveComplete);
			w.Write(s.phases.Count);
			foreach (H2HMatchPhaseState st in s.phases) {
				w.Write(st);
			}
			w.Write(s.objectives.Count);
			foreach (H2HMatchObjectiveState st in s.objectives) {
				w.Write(st);
			}
		}

		public static H2HMatchPhaseState ReadMatchPhaseState(this CelesteNetBinaryReader r) {
			H2HMatchPhaseState s = new H2HMatchPhaseState();
			s.PhaseID = r.ReadUInt32();
			s.Completed = r.ReadBoolean();
			return s;
		}

		public static void Write(this CelesteNetBinaryWriter w, H2HMatchPhaseState s) {
			w.Write(s.PhaseID);
			w.Write(s.Completed);
		}

		public static H2HMatchObjectiveState ReadMatchObjectiveState(this CelesteNetBinaryReader r) {
			H2HMatchObjectiveState s = new H2HMatchObjectiveState();
			s.ObjectiveID = r.ReadUInt32();
			s.Completed = r.ReadBoolean();
			int berries = r.ReadInt32();
			if (berries >= 0) {
				s.CollectedStrawbs = new List<EntityID>();
				for (int i = 0; i < berries; i++) {
					EntityID eid = new EntityID();
					eid.Key = r.ReadString();
					s.CollectedStrawbs.Add(eid);
				}
			}
			return s;
		}

		public static void Write(this CelesteNetBinaryWriter w, H2HMatchObjectiveState s) {
			w.Write(s.ObjectiveID);
			w.Write(s.Completed);
			if (s.CollectedStrawbs == null) {
				w.Write(-1);
			}
			else {
				w.Write(s.CollectedStrawbs.Count);
				foreach (EntityID id in s.CollectedStrawbs) {
					w.Write(id.Key);
				}
			}
		}
	}
}
