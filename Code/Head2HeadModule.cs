using Celeste.Editor;
using Celeste.Mod;
using Celeste.Mod.Head2Head.Control;
using Celeste.Mod.Head2Head.Data;
using Celeste.Mod.Head2Head.IO;
using Celeste.Mod.Head2Head.Shared;
using Celeste.Mod.CelesteNet;
using Celeste.Mod.CelesteNet.Client;
using Celeste.Mod.CelesteNet.DataTypes;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoMod.Utils;
using Celeste.Mod.Head2Head.Entities;
using System.Collections;
using MonoMod.RuntimeDetour;
using System.Reflection;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using Celeste.Mod.UI;
using Celeste.Mod.Head2Head.UI;

// TODO maintain repository of matches in the lobby/channel
// TODO match selection from repository (future update maybe)
// TODO Auto-launch into next map in multi-phase matches (setting based?)
// TODO Prevent starting at checkpoints not already reached during the match
// TODO Freeze the chapter timer while in the lobby

// timer conversion to readable string: speedrunTimerFileString = Dialog.FileTime(SaveData.Instance.Time);

namespace Celeste.Mod.Head2Head {
	public class Head2HeadModule : EverestModule {
		private static readonly int START_TIMER_LEAD_MS = 5000;
		public static readonly string ProtocolVersion = "1_0_0";

		public static Head2HeadModule Instance { get; private set; }
		public static string AssemblyVersion { get {
				if (string.IsNullOrEmpty(_version)) {
					System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
					System.Diagnostics.FileVersionInfo fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);
					_version = fvi.FileVersion;
				}
				return _version;
			}
		}
		private static string _version = null;

		private static IDetour hook_Strawberry_orig_OnCollect;
		private static IDetour hook_OuiChapterSelectIcon_Get_IdlePosition;

		// #######################################################

		public override Type SettingsType => typeof(Head2HeadModuleSettings);
		public static Head2HeadModuleSettings Settings => (Head2HeadModuleSettings)Instance._Settings;

		public override Type SaveDataType => typeof(Head2HeadModuleSaveData);
		public static Head2HeadModuleSaveData SaveData => (Head2HeadModuleSaveData)Instance._SaveData;

		public override Type SessionType => typeof(Head2HeadModuleSession);
		public static Head2HeadModuleSession Session => (Head2HeadModuleSession)Instance._Session;

		// #######################################################

		public CNetComm Comm;
		public Session currentSession = null;
		public List<Scene> currentScenes = new List<Scene>();
		public MatchDefinition buildingMatch = null;

		// TODO discard stale data so it doesn't just grow indefinitely
		public static Dictionary<string, MatchDefinition> knownMatches = new Dictionary<string, MatchDefinition>();
		public static Dictionary<PlayerID, PlayerStatus> knownPlayers = new Dictionary<PlayerID, PlayerStatus>();

		public delegate void OnMatchStagedHandler();
		public static event OnMatchStagedHandler OnMatchStaged;
		public delegate void OnPlayerJoinedMatchHandler(PlayerID player, string matchID);
		public static event OnPlayerJoinedMatchHandler OnPlayerJoinedMatch;

		// #######################################################

		public Head2HeadModule() {
			Instance = this;
		}

		public override void Load() {
			// Annoying manual/IL hooks
			//On.Celeste.Strawberry.OnCollect += OnStrawberryCollect;
			hook_Strawberry_orig_OnCollect = new Hook(
				typeof(Strawberry).GetMethod("orig_OnCollect", BindingFlags.Public | BindingFlags.Instance),
				typeof(Head2HeadModule).GetMethod("OnStrawberryCollect"));
			hook_OuiChapterSelectIcon_Get_IdlePosition = new Hook(
				typeof(OuiChapterSelectIcon).GetProperty("IdlePosition").GetAccessors()[0],
				typeof(Head2HeadModule).GetMethod("OnOuiChapterSelectIconGetIdlePosition"));
			IL.Celeste.LevelLoader.LoadingThread += Level_LoadingThread;
			// Monocle + Celeste Hooks
			On.Monocle.Scene.Begin += OnSceneBegin;
			On.Monocle.Scene.End += OnSceneEnd;

			On.Celeste.Level.Render += OnLevelRender;
			On.Celeste.Level.Pause += OnGamePause;
			On.Celeste.Postcard.DisplayRoutine += OnPostcardDisplayRoutine;
			On.Celeste.SaveData.RegisterCassette += OnCassetteCollected;
			On.Celeste.SaveData.RegisterHeartGem += OnHeartCollected;
			On.Celeste.Editor.MapEditor.ctor += onDebugScreenOpened;
			On.Celeste.LevelLoader.StartLevel += OnLevelLoaderStart;
			On.Celeste.Editor.MapEditor.LoadLevel += onDebugTeleport;
			On.Celeste.OuiChapterSelectIcon.Show += OnOuiChapterSelectIconShow;
			On.Celeste.Mod.UI.OuiMapSearch.cleanExit += OnMapSearchCleanExit;
			On.Celeste.Mod.UI.OuiMapSearch.Inspect += OnMapSearchInspect;
			On.Celeste.Mod.UI.OuiMapList.Update += OnMapListUpdate;
			On.Celeste.Mod.UI.OuiMapList.Inspect += OnMapListInspect;
			// Everest Events
			Everest.Events.Level.OnEnter += onLevelEnter;
			Everest.Events.Level.OnExit += onLevelExit;
			Everest.Events.Level.OnTransitionTo += onRoomTransition;
			Everest.Events.Celeste.OnExiting += OnExiting;
			// CelesteNet events
			CNetComm.OnConnected += OnConnected;
			CNetComm.OnDisconnected += OnDisconnected;
			CNetComm.OnReceiveChannelMove += OnChannelMove;
			// Head2Head events
			CNetComm.OnReceiveMatchJoin += OnMatchJoinReceived;
			CNetComm.OnReceiveMatchUpdate += OnMatchUpdate;
			CNetComm.OnReceivePlayerStatus += OnPlayerStatusUpdate;
			CNetComm.OnReceiveMatchReset += OnMatchReset;
			// Misc other setup
			Celeste.Instance.Components.Add(Comm = new CNetComm(Celeste.Instance));
		}

		public override void Unload() {
			// Manual/IL hooks
			hook_Strawberry_orig_OnCollect?.Dispose();
			hook_Strawberry_orig_OnCollect = null;
			hook_OuiChapterSelectIcon_Get_IdlePosition?.Dispose();
			hook_OuiChapterSelectIcon_Get_IdlePosition = null;
			IL.Celeste.LevelLoader.LoadingThread -= Level_LoadingThread;
			// Monocle + Celeste Hooks
			On.Monocle.Scene.Begin -= OnSceneBegin;
			On.Monocle.Scene.End -= OnSceneEnd;

			On.Celeste.Level.Pause -= OnGamePause;
			On.Celeste.Postcard.DisplayRoutine -= OnPostcardDisplayRoutine;
			On.Celeste.SaveData.RegisterCassette -= OnCassetteCollected;
			On.Celeste.SaveData.RegisterHeartGem -= OnHeartCollected;
			On.Celeste.Editor.MapEditor.ctor -= onDebugScreenOpened;
			On.Celeste.Editor.MapEditor.LoadLevel -= onDebugTeleport;
			On.Celeste.OuiChapterSelectIcon.Show -= OnOuiChapterSelectIconShow;
			On.Celeste.Mod.UI.OuiMapSearch.cleanExit -= OnMapSearchCleanExit;
			On.Celeste.Mod.UI.OuiMapSearch.Inspect -= OnMapSearchInspect;
			On.Celeste.Mod.UI.OuiMapList.Update -= OnMapListUpdate;
			On.Celeste.Mod.UI.OuiMapList.Inspect -= OnMapListInspect;
			// Everest Events
			Everest.Events.Level.OnEnter -= onLevelEnter;
			Everest.Events.Level.OnExit -= onLevelExit;
			Everest.Events.Level.OnTransitionTo -= onRoomTransition;
			Everest.Events.Celeste.OnExiting -= OnExiting;
			// CelesteNet events
			CNetComm.OnConnected -= OnConnected;
			CNetComm.OnDisconnected -= OnDisconnected;
			CNetComm.OnReceiveChannelMove -= OnChannelMove;
			// Head2Head events
			CNetComm.OnReceiveMatchJoin -= OnMatchJoinReceived;
			CNetComm.OnReceiveMatchUpdate -= OnMatchUpdate;
			CNetComm.OnReceivePlayerStatus -= OnPlayerStatusUpdate;
			CNetComm.OnReceiveMatchReset -= OnMatchReset;
			// Misc other cleanup
			if (Celeste.Instance.Components.Contains(Comm))
				Celeste.Instance.Components.Remove(Comm);
		}

		// ###############################################

		private static void Level_LoadingThread(ILContext il) {
			ILCursor cursor = new ILCursor(il);
			cursor.GotoNext(instr => instr.MatchRet());
			if (cursor.TryGotoPrev(instr => instr.MatchLdarg(0), instr => instr.MatchLdcI4(1), instr => instr.MatchCallvirt<LevelLoader>("set_Loaded"))) {
				cursor.Emit(OpCodes.Ldarg_0);
				cursor.Emit(OpCodes.Call, typeof(Head2HeadModule).GetMethod("OnAddRenderers"));
			}
		}

		private void onLevelEnter(Session session, bool fromSaveData) {
			ActionLogger.StartingChapter(session.Area.SID + " (" + session.LevelData.Name + ")");
			currentSession = session;
			PlayerStatus.Current.ChapterEntered(new GlobalAreaKey(session.Area), session);
		}

		private void onLevelExit(Level level, LevelExit exit, LevelExit.Mode mode, Session session, HiresSnow snow) {
			ActionLogger.EndingChapter(session.Area.SID);
			currentSession = null;
			PlayerStatus.Current.ChapterExited(level, exit, mode, session, snow);
		}

		private void OnLevelLoaderStart(On.Celeste.LevelLoader.orig_StartLevel orig, LevelLoader self) {
			orig(self);
			PlayerStatus.Current.OnLevelLoaderStart(self);
		}

		private void onRoomTransition(Level level, LevelData next, Vector2 direction) {
			ActionLogger.EnteringRoom(next.Name);
			PlayerStatus.Current.RoomEntered(level, next, direction);
		}

		private void onDebugScreenOpened(On.Celeste.Editor.MapEditor.orig_ctor orig, MapEditor self, AreaKey area, bool reloadMapData) {
			ActionLogger.DebugView(area.SID);
			orig(self, area, reloadMapData);
			PlayerStatus.Current.DebugOpened(self, new GlobalAreaKey(area), reloadMapData);
		}
		
		private void onDebugTeleport(On.Celeste.Editor.MapEditor.orig_LoadLevel orig, MapEditor self, LevelTemplate level, Vector2 at) {
			ActionLogger.DebugEnter(level.Name);
			orig(self, level, at);
			PlayerStatus.Current.DebugTeleport(self, level, at);
		}

		private void OnExiting() {
			ActionLogger.ClosingApplication("Exiting application normally");
		}

		private void OnGamePause(On.Celeste.Level.orig_Pause orig, Level self, int startIndex, bool minimal, bool quickReset) {
			ILSelector.OnPause(orig, self, startIndex, minimal, quickReset);
			orig(self, startIndex, minimal, quickReset);
		}

		private IEnumerator OnPostcardDisplayRoutine(On.Celeste.Postcard.orig_DisplayRoutine orig, Postcard self) {
			// TODO be smarter about not preventing error messages from Everest
			if (PlayerStatus.Current.CurrentMatch?.State == MatchState.InProgress) yield break;
			else yield return new SwapImmediately(orig(self));
		}

		public static void OnStrawberryCollect(Action<Strawberry> orig, Strawberry self){
			orig(self);
			Level level = self.Scene as Level;
			GlobalAreaKey gak = new GlobalAreaKey(level.Session.Area);
			if (self.Golden) {
				// TODO handle golden strawberries
			}
			else {
				// TODO special handling for moon berries
				PlayerStatus.Current.StrawberryCollected(gak, self);
			}
		}

		public static Vector2 OnOuiChapterSelectIconGetIdlePosition(Func<OuiChapterSelectIcon, Vector2> orig, OuiChapterSelectIcon self) {
			return self is OuiRunSelectILChapterIcon icon ? icon.IdlePositionOverride : orig(self);
		}

		private void OnHeartCollected(On.Celeste.SaveData.orig_RegisterHeartGem orig, SaveData self, AreaKey area) {
			orig(self, area);
			PlayerStatus.Current.HeartCollected(new GlobalAreaKey(area));
		}

		private void OnCassetteCollected(On.Celeste.SaveData.orig_RegisterCassette orig, SaveData self, AreaKey area) {
			orig(self, area);
			PlayerStatus.Current.CassetteCollected(new GlobalAreaKey(area));
		}

		private void OnMapSearchCleanExit(On.Celeste.Mod.UI.OuiMapSearch.orig_cleanExit orig, OuiMapSearch self) {
			ILSelector.OnMapSearchCleanExit(orig, self);
		}

		private void OnMapSearchInspect(On.Celeste.Mod.UI.OuiMapSearch.orig_Inspect orig, OuiMapSearch self, AreaData area, AreaMode mode) {
			ILSelector.OnMapSearchInspect(orig, self, area, mode);
		}

		private void OnMapListUpdate(On.Celeste.Mod.UI.OuiMapList.orig_Update orig, OuiMapList self) {
			ILSelector.OnMapListUpdate(orig, self);
		}

		private void OnMapListInspect(On.Celeste.Mod.UI.OuiMapList.orig_Inspect orig, OuiMapList self, AreaData area, AreaMode mode) {
			ILSelector.OnMapListInspect(orig, self, area, mode);
		}

		private void OnOuiChapterSelectIconShow(On.Celeste.OuiChapterSelectIcon.orig_Show orig, OuiChapterSelectIcon self) {
			orig(self);
			if (self is OuiRunSelectILChapterIcon icon)
				icon.OnAfterShow();
		}

		private void OnSceneBegin(On.Monocle.Scene.orig_Begin orig, Scene self) {
			currentScenes.Add(self);
			orig(self);
			if (self is Overworld) {
				self.Add(new H2HHudRenderer());
			}
		}

		private void OnSceneEnd(On.Monocle.Scene.orig_End orig, Scene self) {
			orig(self);
			currentScenes.Remove(self);
		}

		private void OnLevelRender(On.Celeste.Level.orig_Render orig, Level self) {
			orig(self);
			DynamicData dd = new DynamicData(self);
			H2HHudRenderer hud = dd.Get<H2HHudRenderer>("H2HHudRenderer");
			hud?.Render(self);
		}

		public static void OnAddRenderers(LevelLoader loader) {
			H2HHudRenderer ren = new H2HHudRenderer();
			loader.Level.Add(ren);
			DynamicData dd = new DynamicData(loader.Level);
			dd.Set("H2HHudRenderer", ren);
		}

		// ########################################

		private void OnPlayerStatusUpdate(DataH2HPlayerStatus data) {
			if (!data.playerID.Equals(PlayerID.MyID)) {
				if (knownPlayers.ContainsKey(data.playerID)) {
					knownPlayers[data.playerID] = data.Status;
				}
				else knownPlayers.Add(data.playerID, data.Status);
			}
			if (data.Status.State == PlayerStateCategory.FinishedMatch
				&& PlayerStatus.Current.MatchState == MatchState.InProgress
				&& data.Status.CurrentMatch.MatchID == PlayerStatus.Current.CurrentMatch.MatchID) {
				bool playersFinished = true;
				foreach (PlayerStatus p in knownPlayers.Values) {
					if (p.CurrentMatch == null) continue;
					if (p.CurrentMatch.MatchID != PlayerStatus.Current.CurrentMatch.MatchID) continue;
					if (p.State == PlayerStateCategory.InMatch) playersFinished = false;
				}
				if (playersFinished) {
					Engine.Commands.Log("Match Completed!!!!!");
					PlayerStatus.Current.CurrentMatch.State = MatchState.Completed;
					// TODO handle match completion
				}
			}
		}

		private void OnMatchJoinReceived(DataH2HMatchJoin data) {
			if (PlayerStatus.Current.CurrentMatch.MatchID != data.MatchID) {
				Engine.Commands.Log(data.playerID.Name + " cannot join the current match: match ID doesn't match");
				return;
			}
			if (PlayerStatus.Current.MatchState != MatchState.Staged) {
				Engine.Commands.Log(data.playerID.Name + " cannot join a match: match is't staged");
				return;
			}
			if (PlayerStatus.Current.CurrentMatch.Players.Contains(data.playerID)) {
				Engine.Commands.Log("Player already joined: " + data.playerID.Name);
				return;
			}
			PlayerStatus.Current.CurrentMatch.Players.Add(data.playerID);
			Engine.Commands.Log(data.player.FullName + " joined the match!");
			OnPlayerJoinedMatch?.Invoke(data.playerID, data.MatchID);
		}

		private void OnMatchReset(DataH2HMatchReset data) {
			if (PlayerStatus.Current.CurrentMatch?.MatchID == data.MatchID) {
				PlayerStatus.Current.State = PlayerStateCategory.Idle;
				if (PlayerStatus.Current.CurrentMatch != null) {
					CNetComm.Instance.SendMatchReset(PlayerStatus.Current.CurrentMatch.MatchID);
					PlayerStatus.Current.CurrentMatch = null;
				}
			}
		}

		private void OnMatchUpdate(DataH2HMatchUpdate data) {
			OnMatchUpdated(data.NewDef);
		}

		private void OnChannelMove(DataChannelMove data) {
			// TODO handle channel moves
			Engine.Commands.Log("Channel Move Received for " + data.Player?.FullName);
		}

		private void OnConnected(CelesteNetClientContext cxt) {
			//PlayerStatus.Current.CNetConnected();
		}

		private void OnDisconnected(CelesteNetConnection con) {
			//PlayerStatus.Current.CNetDisconnected();
		}

		// #######################################################

		public bool CanStageMatch() {
			if (!CNetComm.Instance.IsConnected) return false;
			if (buildingMatch == null) return false;
			if (buildingMatch.Phases.Count == 0) return false;
			return PlayerStatus.Current.CanStageMatch();
		}

		public bool CanJoinMatch() {
			if (PlayerStatus.Current.CurrentMatch == null) return false;
			if (PlayerStatus.Current.MatchState != MatchState.Staged) return false;
			if (PlayerStatus.Current.CurrentMatch.Players.Contains(PlayerID.MyIDSafe)) return false;
			return true;
		}

		public bool CanStartMatch() {
			if (PlayerStatus.Current.CurrentMatch == null) return false;
			if (PlayerStatus.Current.MatchState != MatchState.Staged) return false;
			if (!PlayerStatus.Current.CurrentMatch.Players.Contains(PlayerID.MyIDSafe)) return false;
			return true;
		}

		public void StartMatchBuild() {
			if (!(CNetComm.Instance?.IsConnected ?? false)) {
				Engine.Commands.Log("Connect to CelesteNet before building a match");
				return;
			}
			Engine.Commands.Log("Starting new match build...");
			buildingMatch = new MatchDefinition() {
				Owner = PlayerID.MyID ?? PlayerID.Default,
			};
		}

		public void AddMatchPhase(StandardCategory category, GlobalAreaKey? areakey = null) {
			MatchPhase mp = null;
			GlobalAreaKey area = areakey ?? new GlobalAreaKey(currentSession.Area);
			switch (category) {
				default:
					break;
				case StandardCategory.Clear:
					mp = StandardMatches.ILClear(area);
					break;
				case StandardCategory.FullClear:
					mp = StandardMatches.ILFullClear(area);
					break;
				case StandardCategory.HeartCassette:
					mp = StandardMatches.ILHeartCassette(area);
					break;
				case StandardCategory.ARB:
					mp = StandardMatches.ILAllRedBerries(area);
					break;
				case StandardCategory.ARBHeart:
					mp = StandardMatches.ILAllRedBerriesHeart(area);
					break;
				case StandardCategory.CassetteGrab:
					mp = StandardMatches.ILCassetteGrab(area);
					break;
			}
			if (mp == null) {
				Engine.Commands.Log(string.Format("Couldn't add {0} ({1}) - Category is not valid for this chapter", area.DisplayName, category));
				return;
			}
			else {
				AddMatchPhase(mp);
			}
			if (buildingMatch != null) {
				Engine.Commands.Log(string.Format("Added {0} ({1})", area.DisplayName, category));
			}
		}

		public void AddMatchPhase(MatchPhase mp) {
			if (buildingMatch == null) {
				StartMatchBuild();
			}
			if (buildingMatch == null) {
				Engine.Commands.Log("Unable to add the match phase...");
				return;
			}
			buildingMatch.Phases.Add(mp);
		}

		public void StageMatch() {
			if (buildingMatch == null) {
				Engine.Commands.Log("You need to build a match first...");
				return;
			}
			if (buildingMatch.Phases.Count == 0) {
				Engine.Commands.Log("You need to add a phase first...");
				return;
			}
			if (PlayerStatus.Current.MatchState == MatchState.InProgress) {
				Engine.Commands.Log("You're already in a match...");
				return;
			}
			buildingMatch.AssignIDs();
			buildingMatch.State = MatchState.Staged;  // Sends update
			buildingMatch = null;
		}

		public void JoinStagedMatch() {
			if (PlayerStatus.Current.CurrentMatch == null) {
				Engine.Commands.Log("There is no staged match!");
				return;
			}
			if (PlayerStatus.Current.MatchState != MatchState.Staged) {
				Engine.Commands.Log("Current match is not staged!");
				return;
			}
			CNetComm.Instance.SendMatchJoin(PlayerStatus.Current.CurrentMatch.MatchID);
			PlayerStatus.Current.MatchJoined();
		}

		public void BeginStagedMatch() {
			MatchDefinition def = PlayerStatus.Current.CurrentMatch;
			if (def == null) {
				Engine.Commands.Log("There is no staged match!");
				return;
			}
			if (def.State != MatchState.Staged) {
				Engine.Commands.Log("Current match is not staged!");
				return;
			}
			if (!def.Owner.Equals(PlayerID.MyIDSafe)
				&& !def.Players.Contains(PlayerID.MyIDSafe)) {
				Engine.Commands.Log("Cannot start a match I neither own nor have joined");
				return;
			}
			def.BeginInstant = DateTime.Now + new TimeSpan(0, 0, 0, 0, START_TIMER_LEAD_MS);
			def.State = MatchState.InProgress;
			MatchStarted(def);  // OnMatchUpdated doesn't detect a status change locally
		}

		public void ResetCurrentMatch() {
			buildingMatch = null;
			PlayerStatus.Current.State = CNetComm.Instance.IsConnected ? PlayerStateCategory.Idle : PlayerStateCategory.None;
			if (PlayerStatus.Current.CurrentMatch != null) {
				CNetComm.Instance.SendMatchReset(PlayerStatus.Current.CurrentMatch.MatchID);
			}
		}

		// #######################################################

		public void OnMatchUpdated(MatchDefinition def) {
			bool isNew = !knownMatches.ContainsKey(def.MatchID);
			if (isNew) {
				if (def.State == MatchState.Completed) return;
				knownMatches.Add(def.MatchID, def);
			}
			MatchDefinition old = isNew ? null : knownMatches[def.MatchID];
			knownMatches[def.MatchID] = def;
			if (def.State == MatchState.Staged && (isNew || old.State != MatchState.Staged)) {
				MatchStaged(def);
			}
			else if (def.State == MatchState.InProgress
				&& (isNew || old.State != MatchState.InProgress)) {
				MatchStarted(def);
			}
		}

		private static void MatchStaged(MatchDefinition def) {
			MatchDefinition current = PlayerStatus.Current.CurrentMatch;
			if (current != null) {
				if (current.State == MatchState.InProgress) return;  // In a match...
				if (current.State == MatchState.Staged && current.Players.Contains(PlayerID.MyIDSafe)) return;  // Joined a match...
			}
			PlayerStatus.Current.CurrentMatch = def;
			PlayerStatus.Current.MatchStaged(PlayerStatus.Current.CurrentMatch);
			OnMatchStaged?.Invoke();
		}

		private bool MatchStarted(MatchDefinition def) {
			foreach (MatchPhase ph in def.Phases) {
				if (!ph.Area.ExistsLocal) {
					return false;
				}
			}
			if (currentScenes.Count == 0) {
				Engine.Commands.Log("Cannot start match: there is no scene");
				return false;
			}
			if (PlayerStatus.Current.CurrentMatch == null) {  // Out of sync (maybe a disconnect / reconnect?)
				if (!def.Players.Contains(PlayerID.MyIDSafe)) return false;
				PlayerStatus.Current.CurrentMatch = def;
				// TODO if the player has not entered a savefile and reconnects and hits this, it could load you into the level without a savefile
			}
			else if (PlayerStatus.Current.CurrentMatch.MatchID != def.MatchID) {  // Not a match we care about
				return false;
			}
			else if (!def.Players.Contains(PlayerID.MyIDSafe)) {  // Player did not join the match
				PlayerStatus.Current.CurrentMatch = null;
				PlayerStatus.Current.State = PlayerStateCategory.Idle;
				return true;
			}
			// Begin!
			// TODO prevent this from getting broken by stuff like closing the OUI
			Entity wrapper = new Entity();
			currentScenes.Last().Add(wrapper);
			wrapper.Add(new Coroutine(StartMatchCoroutine(def.Phases[0].Area)));
			return true;
		}

		private IEnumerator StartMatchCoroutine(GlobalAreaKey gak) {
			if (PlayerStatus.Current.CurrentMatch == null) yield break;
			string idCheck = PlayerStatus.Current.CurrentMatchID;
			DateTime startInstant = PlayerStatus.Current.CurrentMatch.BeginInstant;
			DateTime now = DateTime.Now;
			if (startInstant > now + new TimeSpan(0, 0, 15)) {
				Engine.Commands.Log("Match begins more than 15 seconds in the future; skipping countdown.");
			}
			else if (startInstant < now) {
				Engine.Commands.Log("Match begins in the past...");
			}
			else {
				// TODO countdown animation
				yield return (float)((startInstant - now).TotalSeconds);
			}

			if (PlayerStatus.Current.CurrentMatch == null) yield break;
			if (PlayerStatus.Current.CurrentMatchID != idCheck) yield break;
			PlayerStatus.Current.CurrentMatch.SetState_NoUpdate(MatchState.InProgress);  // Probably duplicative but just to be safe
			PlayerStatus.Current.MatchStarted();
			new FadeWipe(currentScenes.Last(), false, () => {
				LevelEnter.Go(new Session(gak.Local.Value), false);
				// TODO send a confirmation message on load-in?
				// TODO set the beginning file timer
			});
		}

	}

	public class Head2HeadModuleSettings : EverestModuleSettings {

	}

	public class Head2HeadModuleSaveData : EverestModuleSaveData {

	}

	public class Head2HeadModuleSession : EverestModuleSession {

	}
}
