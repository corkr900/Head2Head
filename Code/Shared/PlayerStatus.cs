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
					Logger.Log(LogLevel.Info, "Head2Head", "Entering overworld (default - new player status)");
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

		public string CurrentMatchID { get; internal set; }
		public GlobalAreaKey CurrentArea { get; internal set; }
		public GlobalAreaKey? RandomizerArea { get; internal set; } = null;
		public string CurrentRoom { get; internal set; }
		public int FileSlotBeforeMatchStart { get; internal set; }
		public string LastCheckpoint { get; internal set; } = null;
		public bool IsInDebug { get; internal set; }
		public long CurrentFileTimer { get; internal set; }
		public long FileTimerAtMatchBegin { get; internal set; }
		public long FileTimerAtLastCheckpoint { get; internal set; }
		public long FileTimerAtLastObjectiveComplete { get; internal set; }
		public List<H2HMatchPhaseState> phases { get; internal set; } = new List<H2HMatchPhaseState>();
		public List<H2HMatchObjectiveState> objectives { get; internal set; } = new List<H2HMatchObjectiveState>();
		public List<Tuple<GlobalAreaKey, string>> reachedCheckpoints { get; internal set; } = new List<Tuple<GlobalAreaKey, string>>();

		#region Non-synchronized data

		/// <summary>
		/// Keeps track of gems collected in vanilla summit.
		/// </summary>
		public bool[] SummitGems { get; internal set; } = new bool[6];

		/// <summary>
		/// This is set on receipt of an update.
		/// </summary>
		public DateTime ReceivedAt = SyncedClock.Now;

		/// <summary>
		/// Assists & variants active prior to the match starting.
		/// The system will restore them after the player finishes the match.
		/// null means restoring isn't necessary.
		/// </summary>
		public Assists? ActiveAssistsBeforeMatch = null;

		#endregion

		public MatchState MatchState {
			get {
				return CurrentMatch?.State ?? MatchState.None;
			}
		}

		// Lobby Race Stuff

		public bool StartedLobbyRace { get { return InTimeTrial; } }
		public bool RunningLobbyRace { get { return lobbyCP >= 0; } }
		internal int lobbyCP = -1;
		internal long lobbyTimer = 0;
		public long RecordLobbyTime = -1;
		internal bool InTimeTrial = false;

		internal void ResetLobbyRace() {
			InTimeTrial = false;
			lobbyCP = -1;
			lobbyTimer = 0;
			RecordLobbyTime = -1;
		}

		internal void StartLobbyRace() {
			InTimeTrial = true;
			lobbyCP = -1;
			lobbyTimer = 0;
		}

		internal void StartLobbyRaceTimer() {
			lobbyCP = 0;
		}

		internal void FinishLobbyRace() {
			InTimeTrial = false;
			lobbyCP = -1;
			if (RecordLobbyTime < 0 || lobbyTimer < RecordLobbyTime) {
				RecordLobbyTime = lobbyTimer;
				Updated();
			}
		}

		// Lifecycle events
		public void ChapterEntered(GlobalAreaKey area, Session session) {
			ResetLobbyRace();
			if (IsInDebug) {
				IsInDebug = false;
				Updated();
			}
			Logger.Log(LogLevel.Info, "Head2Head", "Entering " + area.SID + " (chapter entered)");
			CurrentArea = area;
			CurrentRoom = session.Level;
			if (IsInMatch(false)) {
				LastCheckpoint = session.LevelData.HasCheckpoint ? session.LevelData.Name : null;
				FileTimerAtLastCheckpoint = SaveData.Instance?.Time ?? FileTimerAtLastCheckpoint;
				CheckRoomEnterObjective(CurrentRoom, area);
			}
			Updated();
		}

		public void RoomEntered(Level level, LevelData next, Vector2 direction) {
			CurrentRoom = next.Name;
			if (next.HasCheckpoint) {
				if (IsInMatch(false)) {
					LastCheckpoint = next.Name;
					FileTimerAtLastCheckpoint = SaveData.Instance?.Time ?? FileTimerAtLastCheckpoint;
					GlobalAreaKey area = new GlobalAreaKey(level.Session.Area);
					int index = reachedCheckpoints.FindIndex((Tuple<GlobalAreaKey, string> tpred) => {
						return tpred.Item1.Equals(area) && tpred.Item2 == LastCheckpoint;
					});
					if (index < 0 || index >= reachedCheckpoints.Count) {
						reachedCheckpoints.Add(new Tuple<GlobalAreaKey, string>(area, LastCheckpoint));
					}
				}
				ActionLogger.EnteringRoom();
			}
			if (IsInMatch(false)) {
				CheckRoomEnterObjective(CurrentRoom, new GlobalAreaKey(level.Session.Area));
			}
			Updated();
		}

		internal void DebugOpened(MapEditor screen, GlobalAreaKey globalAreaKey, bool reloadMapData) {
			IsInDebug = true;
			Updated();
		}

		internal void DebugTeleport(MapEditor screen, LevelTemplate level, Vector2 at) {
			IsInDebug = false;
			Updated();
		}

		public void ChapterExited(LevelExit.Mode mode, Session session) {
			ResetLobbyRace();
			Logger.Log(LogLevel.Info, "Head2Head", "Entering Overworld (chapter exited)");
			CurrentArea = GlobalAreaKey.Overworld;
			CurrentRoom = "";
			if (IsInMatch(false)) {
				LastCheckpoint = null;
				FileTimerAtLastCheckpoint = SaveData.Instance?.Time ?? FileTimerAtLastCheckpoint;
			}
			Updated();
		}

		public void ChapterRestarted(Session session) {

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
			SummitGems = new bool[6];
			Updated();
		}

		public void MatchStarted() {
			phases.Clear();
			objectives.Clear();
			reachedCheckpoints.Clear();
			FileTimerAtMatchBegin = SaveData.Instance.Time;
			FileTimerAtLastCheckpoint = SaveData.Instance.Time;
			SummitGems = new bool[6];
			Updated();
		}

		public void MatchReset() {
			phases.Clear();
			objectives.Clear();
			CurrentMatch = null;
			SummitGems = new bool[6];
			Updated();
		}

		public void Cleanup() {
			phases.Clear();
			objectives.Clear();
			CurrentMatch = null;
			SummitGems = new bool[6];
			Updated();
		}

		public void BreakRule(MatchRule ruleBroken, DNFReason dnfReason) {
			if (!IsInMatch(false)) return;
			MatchDefinition def = CurrentMatch;
			foreach (MatchRule rule in def.Rules) {
				if (ruleBroken == rule) {
					def.PlayerDNF(dnfReason);
					return;
				}
			}
		}

		// CHECKING OFF OBJECTIVES/PHASES

		internal void ChapterCompleted(GlobalAreaKey area) {
			if (!IsInMatch(false)) return;
			bool changes = false;
			// Handler Chapter Complete objectives
			MatchObjective ob = FindObjective(MatchObjectiveType.ChapterComplete, area, false, area.SID);
			if (ob != null && MarkObjective(ob, area)) changes = true;
			// Handle Time Limit objectives
			ob = FindObjective(MatchObjectiveType.TimeLimit, area, false);
			if (ob != null) {
				string tmp = CurrentRoom;
				CurrentRoom = "h2h_chapter_completed";  // Signals to the overlay to show time instead of final room
				if (MarkObjective(ob, area)) changes = true;
				CurrentRoom = tmp;  // restore to the actual real value
			}
			// Handle Randomizer Complete objectives
			ob = FindObjective(MatchObjectiveType.RandomizerClear, area);
			if (ob != null && MarkObjective(ob, area)) changes = true;
			// Publish changes
			if (changes) Updated();
		}
		internal void HeartCollected(GlobalAreaKey area) {
			if (!IsInMatch(false)) return;
			MatchObjective ob = FindObjective(MatchObjectiveType.HeartCollect, area, false);
			if (ob != null && MarkObjective(ob, area)) Updated();
		}
		internal void CassetteCollected(GlobalAreaKey area) {
			if (!IsInMatch(false)) return;
			MatchObjective ob = FindObjective(MatchObjectiveType.CassetteCollect, area, false);
			if (ob != null && MarkObjective(ob, area)) Updated();
		}
		internal void StrawberryCollected(GlobalAreaKey area, Strawberry strawb) {
			CollectableCollected(area, strawb.ID, MatchObjective.GetTypeForStrawberry(strawb));
		}
		internal void CustomCollectableCollected(string entityTypeID, GlobalAreaKey area, EntityID id) {
			CollectableCollected(area, id, MatchObjectiveType.CustomCollectable, entityTypeID);
		}
		internal void CollectableCollected(GlobalAreaKey area, EntityID id, MatchObjectiveType objtype, string entityTypeID = "") {
			if (!IsInMatch(false)) return;
			MatchObjective ob = FindObjective(objtype, area, false, entityTypeID);
			if (ob != null && MarkObjective(ob, area, id)) Updated();
		}
		internal void CheckForTimeLimit(GlobalAreaKey area) {
			if (!IsInMatch(false)) return;
			MatchObjective ob = FindObjective(MatchObjectiveType.TimeLimit, area, false);
			if (ob == null) return;
			MatchResultPlayer res = CurrentMatch?.Result?[PlayerID.MyIDSafe];
			if (res == null) return;
			long endTime = res.FileTimeStart + ob.AdjustedTimeLimit(PlayerID.MyIDSafe);
			if (SaveData.Instance?.Time >= endTime) {
				if (MarkObjective(ob, area)) Updated();
			}
		}
		internal void CustomObjectiveCompleted(string objectiveTypeID, GlobalAreaKey area) {
			if (!IsInMatch(false)) return;
			MatchObjective ob = FindObjective(MatchObjectiveType.CustomObjective, area, false, objectiveTypeID);
			if (ob != null && MarkObjective(ob, area)) Updated();
		}
		internal void CheckRoomEnterObjective(string room, GlobalAreaKey area) {
			if (!IsInMatch(false)) return;
			MatchObjective ob = FindObjective(MatchObjectiveType.EnterRoom, area, false, room);
			if (ob != null && MarkObjective(ob, area)) Updated();
		}
		internal void CheckFlagObjective(string flag, GlobalAreaKey area) {
			if (!IsInMatch(false)) return;
			MatchObjective ob = FindObjective(MatchObjectiveType.Flag, area, false, flag);
			if (ob != null && MarkObjective(ob, area)) Updated();
		}
		internal void SummitHeartGemCollected(int gid) {
			if ((SummitGems?.Length ?? 0) < gid) {
				SummitGems = new bool[(int)Calc.Max(6, gid)];
			}
			SummitGems[gid] = true;
		}
		internal void ChapterUnlocked(GlobalAreaKey area) {
			if (!IsInMatch(false)) return;
			MatchObjective ob = FindObjective(MatchObjectiveType.UnlockChapter, area, false, area.SID);
			if (ob != null && MarkObjective(ob, area)) Updated();
		}

		// objective help

		internal int CurrentPhase(PlayerID? id = null) {
			if (CurrentMatch == null) return -1;
			if (!IsInMatch(false, id)) return -1;
			int max = -1;
			foreach (H2HMatchPhaseState s in phases) {
				MatchPhase ph = CurrentMatch.GetPhase(s.PhaseID);
				if (ph == null) continue;
				if (s.Completed && ph.Order > max) {
					max = ph.Order;
				}
			}
			return max + 1;
		}

		/// <summary>
		/// Searches the current match for an objective meeting the given criteria.
		/// Does not search phases that have not been reached yet (for multi-phase matches)
		/// </summary>
		/// <param name="type">The objective type to search for</param>
		/// <param name="area">The area associated with the objective. Pass null to skip the area check (fullgame objectives)</param>
		/// <param name="includeFinished">If true, okay to return objectives that have already been finished</param>
		/// <param name="entityTypeID">Additional parameter for objective types that need it. Null, empty, or not providing this parameter will skip the check.</param>
		/// <returns>Returns the first-found objective fitting the provided criteria, or null if there is none.</returns>
		internal MatchObjective FindObjective(MatchObjectiveType type, GlobalAreaKey area, bool includeFinished = false, string entityTypeID = "") {
			if (CurrentMatch == null) return null;
			int currentp = CurrentPhase();
			foreach (MatchPhase ph in CurrentMatch.Phases) {
				if (IsPhaseEligible(ph, currentp, area, includeFinished)) {
					foreach (MatchObjective ob in ph.Objectives) {
						if (IsObjectiveEligible(ob, type, entityTypeID, area)) {
							return ob;
						}
					}
				}
			}
			return null;
		}

		private bool IsPhaseEligible(MatchPhase ph, int currentp, GlobalAreaKey area, bool includeFinished) {
			if (ph.Fullgame) return string.IsNullOrEmpty(ph.LevelSet) || ph.LevelSet == area.Local_Safe.LevelSet;
			if (includeFinished) {
				return ph.Order <= currentp && ph.Area.Equals(area);
			}
			return ph.Order == currentp && (ph.Area.Equals(area) || area.Equals(RandomizerArea));
		}

		private bool IsObjectiveEligible(MatchObjective ob, MatchObjectiveType type, string entityTypeID, GlobalAreaKey area) {
			if (ob.ObjectiveType != type) return false;
			if (ob.CollectableGoal > 0) return true;
			if (string.IsNullOrEmpty(ob.CustomTypeKey)) return true;
			if (type == MatchObjectiveType.ChapterComplete) {
				if (ob.Side != area.Mode) return false;
			}
			return ob.CustomTypeKey == entityTypeID;
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

		internal bool MarkObjective(MatchObjective ob, GlobalAreaKey area, EntityID? id = null) {
			id = id ?? EntityID.None;
			if (ob.CollectableGoal > 0) return MarkCountableObjective(ob, area, id.Value);
			else return MarkObjectiveComplete(ob);
		}

		internal bool MarkCountableObjective(MatchObjective ob, GlobalAreaKey area, EntityID id) {
			if (!IsInMatch(false)) return false;
			if (ob == null) return false;
			bool updated = false;
			int stateIndex = objectives.FindIndex((H2HMatchObjectiveState s) => s.ObjectiveID == ob.ID);
			if (stateIndex < 0) {
				Dictionary<GlobalAreaKey, List<EntityID>> itmDict = new Dictionary<GlobalAreaKey, List<EntityID>>();
				itmDict.Add(area, new List<EntityID>() { id });
				objectives.Add(new H2HMatchObjectiveState() {
					ObjectiveID = ob.ID,
					CollectedItems = itmDict,
				});
				stateIndex = objectives.Count - 1;
				updated |= true;
			}
			else {
				if (objectives[stateIndex].CollectedItems == null) {
					H2HMatchObjectiveState objst = objectives[stateIndex];
					objst.CollectedItems = new Dictionary<GlobalAreaKey, List<EntityID>>();
					objectives[stateIndex] = objst;
				}
				Dictionary<GlobalAreaKey, List<EntityID>> items = objectives[stateIndex].CollectedItems;
				if (items.ContainsKey(area) && items[area].Contains(id)) return false;  // Already collected
				// Add it to collected items
				if (!items.ContainsKey(area)) items.Add(area, new List<EntityID>());
				items[area].Add(id);
				updated |= true;
			}
			if (objectives[stateIndex].CountCollectables() >= ob.CollectableGoal) {
				updated |= MarkObjectiveComplete(ob);
			}
			return updated;
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
						st.FinalRoom = CurrentRoom;
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
					FinalRoom = CurrentRoom,
				});
				FileTimerAtLastObjectiveComplete = SaveData.Instance.Time;
			}
			TryMarkPhaseComplete(ob);
			return true;
		}

		private bool TryMarkPhaseComplete(MatchObjective lastOB) {
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
					// This would be problematic if we stored assists in a file-changing match. but we don't so it's fine:
					RestoreOriginalAssists(SaveData.Instance);
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

		// MISC

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
			reachedCheckpoints = other.reachedCheckpoints;
			FileSlotBeforeMatchStart = other.FileSlotBeforeMatchStart;
			for (int i = 0; i < Calc.Min(SummitGems?.Length ?? 0, other.SummitGems?.Length ?? 0); i++) {
				SummitGems[i] |= other.SummitGems[i];
			}
		}

		public HashSet<EntityID> GetAllCollectedStrawbs(GlobalAreaKey gak) {
			HashSet<EntityID> strawbs = new HashSet<EntityID>();
			foreach (H2HMatchObjectiveState ob in objectives) {
				if (ob.CollectedItems?.ContainsKey(gak) != true) continue;
				foreach (EntityID x in ob.CollectedItems[gak]) {
					strawbs.Add(x);
				}
			}
			return strawbs;
		}

		public void ApplyMatchDefinedAssists(SaveData saveData) {
			if (saveData == null || !IsInMatch(true)) return;
			ActiveAssistsBeforeMatch = saveData.Assists;
			saveData.Assists = Assists.Default;
			MatchDefinition def = CurrentMatch;
			if (def.Rules.Contains(MatchRule.NoGrabbing)) saveData.Assists.NoGrabbing = true;
		}

		public void RestoreOriginalAssists(SaveData saveData) {
			if (ActiveAssistsBeforeMatch == null) return;
			saveData.Assists = ActiveAssistsBeforeMatch.Value;
			ActiveAssistsBeforeMatch = null;
		}

	}

	public struct H2HMatchPhaseState {
		public uint PhaseID;
		public bool Completed;
	}

	public struct H2HMatchObjectiveState {
		public uint ObjectiveID;
		public bool Completed;
		public Dictionary<GlobalAreaKey, List<EntityID>> CollectedItems;
		public string FinalRoom;

		public int CountCollectables() {
			if (CollectedItems == null) return 0;
			int count = 0;
			foreach (KeyValuePair<GlobalAreaKey, List<EntityID>> area in CollectedItems) {
				count += area.Value?.Count ?? 0;
			}
			return count;
		}
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
			pms.CurrentMatchID = r.ReadString();
			pms.FileSlotBeforeMatchStart = r.ReadInt32();
			pms.CurrentArea = r.ReadAreaKey();
			pms.CurrentRoom = r.ReadString();
			pms.LastCheckpoint = r.ReadString();
			if (string.IsNullOrEmpty(pms.LastCheckpoint)) pms.LastCheckpoint = null;
			pms.CurrentFileTimer = r.ReadInt64();
			pms.FileTimerAtMatchBegin = r.ReadInt64();
			pms.FileTimerAtLastCheckpoint = r.ReadInt64();
			pms.FileTimerAtLastObjectiveComplete = r.ReadInt64();
			pms.RecordLobbyTime = r.ReadInt64();
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
			w.Write(s.CurrentMatchID ?? "");
			w.Write(s.FileSlotBeforeMatchStart);
			w.Write(s.CurrentArea);
			w.Write(s.CurrentRoom ?? "");
			w.Write(s.LastCheckpoint ?? "");
			w.Write(s.CurrentFileTimer);
			w.Write(s.FileTimerAtMatchBegin);
			w.Write(s.FileTimerAtLastCheckpoint);
			w.Write(s.FileTimerAtLastObjectiveComplete);
			w.Write(s.RecordLobbyTime);
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
			s.FinalRoom = r.ReadString();

			int collectableAreas = r.ReadInt32();
			if (collectableAreas > 0) s.CollectedItems = new Dictionary<GlobalAreaKey, List<EntityID>>();
			for (int i = 0; i < collectableAreas; i++) {
				GlobalAreaKey area = r.ReadAreaKey();
				s.CollectedItems.Add(area, new List<EntityID>());
				int berries = r.ReadInt32();
				for (int j = 0; j < berries; j++) {
					EntityID eid = new EntityID();
					eid.Key = r.ReadString();
					s.CollectedItems[area].Add(eid);
				}
			}
			return s;
		}

		public static void Write(this CelesteNetBinaryWriter w, H2HMatchObjectiveState s) {
			w.Write(s.ObjectiveID);
			w.Write(s.Completed);
			w.Write(s.FinalRoom ?? "");
			w.Write(s.CollectedItems?.Count ?? 0);
			if (s.CollectedItems != null) {
				foreach (KeyValuePair<GlobalAreaKey, List<EntityID>> kvp in s.CollectedItems) {
					w.Write(kvp.Key);
					w.Write(kvp.Value.Count);
					foreach (EntityID id in kvp.Value) {
						w.Write(id.Key);
					}
				}
			}
		}
	}
}
