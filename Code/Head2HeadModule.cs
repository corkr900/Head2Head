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
using FMOD.Studio;

// TODO (!!!) Prevent starting at checkpoints not already reached during the match
// TODO Force DNF if a player intentionally closes the game

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

		private GlobalAreaKey autoLaunchArea;
		private MatchObjectiveType lastObjectiveType;
		private bool doAutoLaunch;

		public static Dictionary<string, MatchDefinition> knownMatches = new Dictionary<string, MatchDefinition>();
		public static Dictionary<PlayerID, PlayerStatus> knownPlayers = new Dictionary<PlayerID, PlayerStatus>();

		public delegate void OnMatchCurrentMatchUpdatedHandler();
		public static event OnMatchCurrentMatchUpdatedHandler OnMatchCurrentMatchUpdated;

		// #######################################################

		public Head2HeadModule() {
			Instance = this;
		}

		public override void Load() {
			// Annoying manual/IL hooks
			hook_Strawberry_orig_OnCollect = new Hook(
				typeof(Strawberry).GetMethod("orig_OnCollect", BindingFlags.Public | BindingFlags.Instance),
				typeof(Head2HeadModule).GetMethod("OnStrawberryCollect"));
			hook_OuiChapterSelectIcon_Get_IdlePosition = new Hook(
				typeof(OuiChapterSelectIcon).GetProperty("IdlePosition").GetAccessors()[0],
				typeof(Head2HeadModule).GetMethod("OnOuiChapterSelectIconGetIdlePosition"));
			IL.Celeste.LevelLoader.LoadingThread += Level_LoadingThread;
			IL.Celeste.Level.CompleteArea_bool_bool_bool += Level_CompleteArea;
			// Monocle + Celeste Hooks
			On.Monocle.Scene.Begin += OnSceneBegin;
			On.Monocle.Scene.End += OnSceneEnd;

			On.Celeste.Level.Render += OnLevelRender;
			On.Celeste.Level.Pause += OnGamePause;
			On.Celeste.Level.UpdateTime += OnLevelUpdateTime;
			On.Celeste.Level.CompleteArea_bool_bool_bool += OnLevelAreaComplete;
			On.Celeste.MapData.ctor += OnMapDataCtor;
			On.Celeste.Postcard.DisplayRoutine += OnPostcardDisplayRoutine;
			On.Celeste.SaveData.RegisterCassette += OnCassetteCollected;
			On.Celeste.SaveData.RegisterHeartGem += OnHeartCollected;
			On.Celeste.SaveData.Start += OnSaveDataStart;
			On.Celeste.SaveData.TryDelete += OnSaveDataTryDelete;
			On.Celeste.LevelData.CreateEntityData += OnLevelDataCreateEntityData;
			On.Celeste.AreaComplete.Update += OnAreaCompleteUpdate;
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
			Everest.Events.Level.OnCreatePauseMenuButtons += OnCreatePauseMenuButtons;
			Everest.Events.Celeste.OnExiting += OnExiting;
			// CelesteNet events
			CNetComm.OnConnected += OnConnected;
			CNetComm.OnDisconnected += OnDisconnected;
			CNetComm.OnReceiveChannelMove += OnChannelMove;
			// Head2Head events
			CNetComm.OnReceiveMatchUpdate += OnMatchUpdate;
			CNetComm.OnReceivePlayerStatus += OnPlayerStatusUpdate;
			CNetComm.OnReceiveMatchReset += OnMatchReset;
			CNetComm.OnReceiveScanRequest += OnScanRequest;
			CNetComm.OnReceiveScanResponse += OnScanResponse;
			PlayerStatus.OnMatchPhaseCompleted += OnCompletedMatchPhase;
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
			IL.Celeste.Level.CompleteArea_bool_bool_bool -= Level_CompleteArea;
			// Monocle + Celeste Hooks
			On.Monocle.Scene.Begin -= OnSceneBegin;
			On.Monocle.Scene.End -= OnSceneEnd;

			On.Celeste.Level.Render -= OnLevelRender;
			On.Celeste.Level.Pause -= OnGamePause;
			On.Celeste.Level.UpdateTime -= OnLevelUpdateTime;
			On.Celeste.Level.CompleteArea_bool_bool_bool -= OnLevelAreaComplete;
			On.Celeste.MapData.ctor -= OnMapDataCtor;
			On.Celeste.Postcard.DisplayRoutine -= OnPostcardDisplayRoutine;
			On.Celeste.SaveData.RegisterCassette -= OnCassetteCollected;
			On.Celeste.SaveData.RegisterHeartGem -= OnHeartCollected;
			On.Celeste.SaveData.Start += OnSaveDataStart;
			On.Celeste.SaveData.TryDelete -= OnSaveDataTryDelete;
			On.Celeste.LevelData.CreateEntityData -= OnLevelDataCreateEntityData;
			On.Celeste.AreaComplete.Update -= OnAreaCompleteUpdate;
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
			Everest.Events.Level.OnCreatePauseMenuButtons -= OnCreatePauseMenuButtons;
			Everest.Events.Celeste.OnExiting -= OnExiting;
			// CelesteNet events
			CNetComm.OnConnected -= OnConnected;
			CNetComm.OnDisconnected -= OnDisconnected;
			CNetComm.OnReceiveChannelMove -= OnChannelMove;
			// Head2Head events
			CNetComm.OnReceiveMatchUpdate -= OnMatchUpdate;
			CNetComm.OnReceivePlayerStatus -= OnPlayerStatusUpdate;
			CNetComm.OnReceiveMatchReset -= OnMatchReset;
			CNetComm.OnReceiveScanRequest -= OnScanRequest;
			CNetComm.OnReceiveScanResponse -= OnScanResponse;
			PlayerStatus.OnMatchPhaseCompleted -= OnCompletedMatchPhase;
			// Misc other cleanup
			if (Celeste.Instance.Components.Contains(Comm))
				Celeste.Instance.Components.Remove(Comm);
		}

		public override void CreateModMenuSection(TextMenu menu, bool inGame, EventInstance snapshot)
		{
			base.CreateModMenuSection(menu, inGame, snapshot);
			Settings.CreateOptions(menu, inGame, snapshot);
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

		private ScreenWipe OnLevelAreaComplete(On.Celeste.Level.orig_CompleteArea_bool_bool_bool orig, Level self, bool spotlightWipe, bool skipScreenWipe, bool skipCompleteScreen) {
			ScreenWipe sw = orig(self, spotlightWipe, skipScreenWipe, skipCompleteScreen);
			if (sw != null) {
				sw.OnComplete = BeforeLevelCompleteAreaActionExecuted(sw.OnComplete, self);
			}
			return sw;
		}

		private static void Level_CompleteArea(ILContext il) {
			// This IL hook covers the code path that OnLevelAreaComplete can't handle
			ILCursor cursor = new ILCursor(il);
			if (cursor.TryGotoNext(instr => instr.MatchCallvirt(typeof(System.Action), "Invoke"))) {
				// "orig" parameter already loaded
				cursor.Emit(OpCodes.Ldarg_0);  // load "self" parameter
				cursor.Emit(OpCodes.Call, typeof(Head2HeadModule).GetMethod("BeforeLevelCompleteAreaActionExecuted"));
			}
		}

		public static Action BeforeLevelCompleteAreaActionExecuted(Action orig, Level level) {
			if (AreaData.Get(level.Session).Interlude_Safe) {
				return () => {
					PlayerStatus.Current.ChapterExited(LevelExit.Mode.CompletedInterlude, level.Session);
					if (!Instance.DoPostPhaseAutoLaunch(false, MatchObjectiveType.ChapterComplete)) {
						orig();
					}
				};
			}
			return orig;
		}

		private void onLevelEnter(Session session, bool fromSaveData) {
			ActionLogger.StartingChapter(session.Area.SID + " (" + session.LevelData.Name + ")");
			currentSession = session;
			PlayerStatus.Current.ChapterEntered(new GlobalAreaKey(session.Area), session);
		}

		private void onLevelExit(Level level, LevelExit exit, LevelExit.Mode mode, Session session, HiresSnow snow) {
			ActionLogger.EndingChapter(session.Area.SID);
			currentSession = null;
			PlayerStatus.Current.ChapterExited(mode, session);
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
			MatchDefinition def = PlayerStatus.Current.CurrentMatch;
			if (def != null) {
				ResultCategory cat = def.GetPlayerResultCat(PlayerID.MyIDSafe);
				if (cat == ResultCategory.InMatch) {
					def.PlayerDNF();
				}
			}
		}

		private void OnExiting() {
			ActionLogger.ClosingApplication("Exiting application normally");
		}

		private void OnGamePause(On.Celeste.Level.orig_Pause orig, Level self, int startIndex, bool minimal, bool quickReset) {
			ILSelector.OnPause(orig, self, startIndex, minimal, quickReset);
			orig(self, startIndex, minimal, quickReset);
		}

		private IEnumerator OnPostcardDisplayRoutine(On.Celeste.Postcard.orig_DisplayRoutine orig, Postcard self) {
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
				// Moon berries work this way too
				PlayerStatus.Current.StrawberryCollected(gak, self);
				Instance.DoPostPhaseAutoLaunch(true, self.Moon ? MatchObjectiveType.MoonBerry : MatchObjectiveType.Strawberries);
			}
		}

		public static Vector2 OnOuiChapterSelectIconGetIdlePosition(Func<OuiChapterSelectIcon, Vector2> orig, OuiChapterSelectIcon self) {
			return self is OuiRunSelectILChapterIcon icon ? icon.IdlePositionOverride : orig(self);
		}

		private void OnHeartCollected(On.Celeste.SaveData.orig_RegisterHeartGem orig, SaveData self, AreaKey area) {
			orig(self, area);
			PlayerStatus.Current.HeartCollected(new GlobalAreaKey(area));  // TODO find a different hook for hearts; This one fires after tapping past the poem, not when the IL timer stops (BUT this *is* consistent with file timer based full runs...)
			DoPostPhaseAutoLaunch(true, MatchObjectiveType.HeartCollect);  // TODO find a better hook for returning to lobby after heart collection
		}

		private void OnCassetteCollected(On.Celeste.SaveData.orig_RegisterCassette orig, SaveData self, AreaKey area) {
			orig(self, area);
			PlayerStatus.Current.CassetteCollected(new GlobalAreaKey(area));
			DoPostPhaseAutoLaunch(true, MatchObjectiveType.CassetteCollect);  // TODO find a better hook for returning to lobby after cassette collection
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

		public static void OnLevelUpdateTime(On.Celeste.Level.orig_UpdateTime orig, Level self) {
			// Freeze the timer in the lobby
			if (self.Session.Area.SID == GlobalAreaKey.Head2HeadLobby.SID) return;
			orig(self);
		}

		private EntityData OnLevelDataCreateEntityData(On.Celeste.LevelData.orig_CreateEntityData orig, LevelData self, BinaryPacker.Element entity) {
			EntityData data = orig(self, entity);
			DynamicData dd = new DynamicData(self);
			if (!dd.Data.ContainsKey("HasRealHeartGem") || !dd.Get<bool>("HasRealHeartGem")) {
				dd.Set("HasRealHeartGem", Shared.Util.EntityIsRealHeartGem(entity));
			}
			return data;
		}

		public static void OnMapDataCtor(On.Celeste.MapData.orig_ctor orig, MapData self, AreaKey area) {
			orig(self, area);
			DynamicData ddself = new DynamicData(self);
			bool found = false;
			foreach (LevelData lev in self.Levels) {
				DynamicData ddlev = new DynamicData(lev);
				if (!ddlev.Data.ContainsKey("HasRealHeartGem")) ddlev.Set("HasRealHeartGem", false);
				if (ddlev.Get<bool>("HasRealHeartGem")) {
					found = true;
					break;
				}
			}
			ddself.Set("DetectedRealHeartGem", found);
		}

		private void OnAreaCompleteUpdate(On.Celeste.AreaComplete.orig_Update orig, AreaComplete self)
		{
			DynamicData dd = new DynamicData(self);
			if (doAutoLaunch && Input.MenuConfirm.Pressed && dd.Get<bool>("finishedSlide") && dd.Get<bool>("canConfirm"))
			{
				if (DoPostPhaseAutoLaunch(true, MatchObjectiveType.ChapterComplete))
				{
					dd.Set("canConfirm", false);
				}
			}
			orig(self);
		}

		private void OnCreatePauseMenuButtons(Level level, TextMenu menu, bool minimal)
		{
			if (!CNetComm.Instance.IsConnected) return;
			// find the position just under "Mod Options"
			int returnToMapIndex = menu.GetItems().FindIndex(item =>
				item.GetType() == typeof(TextMenu.Button) && ((TextMenu.Button)item).Label == Dialog.Clean("MENU_MODOPTIONS"));

			if (returnToMapIndex == -1)
			{
				// fall back to just after "Resume"
				returnToMapIndex = 1;
			}

			// add the "Head 2 Head Helpdesk" button
			TextMenu.Button returnToLobbyButton = new TextMenu.Button(Dialog.Clean("Head2Head_menu_helpdesk"));
			returnToLobbyButton.Pressed(() => {
				level.PauseMainMenuOpen = false;
				new Menus.HelpdeskMenuContext(level, menu.Selection, true).GoTo(Menus.Helpdesk, menu);
			});
			returnToLobbyButton.ConfirmSfx = "event:/ui/main/message_confirm";
			menu.Insert(returnToMapIndex + 1, returnToLobbyButton);
		}

		private void OnSaveDataStart(On.Celeste.SaveData.orig_Start orig, SaveData data, int slot) {
			if (PlayerStatus.Current.IsInMatch(false)) {
				int matchslot = PlayerStatus.Current.GetMatchSaveFile();
				if (matchslot != int.MinValue && matchslot != slot) {
					PlayerStatus.Current.CurrentMatch?.PlayerDNF();
				}
			}
			orig(data, slot);
		}

		private bool OnSaveDataTryDelete(On.Celeste.SaveData.orig_TryDelete orig, int slot) {
			if (PlayerStatus.Current.IsInMatch(false)) {
				PlayerStatus.Current.CurrentMatch?.PlayerDNF();
			}
			return orig(slot);
		}

		// ########################################

		private void OnPlayerStatusUpdate(DataH2HPlayerStatus data) {
			if (!data.playerID.Equals(PlayerID.MyID)) {
				if (knownPlayers.ContainsKey(data.playerID)) {
					knownPlayers[data.playerID] = data.Status;
				}
				else knownPlayers.Add(data.playerID, data.Status);
			}
			if (data.Status.HasCompletedMatch() && knownMatches.ContainsKey(data.Status.CurrentMatchID)) {
				// TODO (!!!) Is this really necessary???
				MatchDefinition def = knownMatches[data.Status.CurrentMatchID];
				if (def.State == MatchState.InProgress) {
					if (def.GetPlayerResultCat(data.playerID) == ResultCategory.InMatch) {
						def.PlayerFinished(data.playerID, data.Status);
					}
					bool playersFinished = true;
					foreach (PlayerID id in def.Players) {
						ResultCategory cat = def.GetPlayerResultCat(id);
						if (cat <= ResultCategory.InMatch) {
							playersFinished = false;
							break;
						}
					}
					if (playersFinished) {
						PlayerStatus.Current.CurrentMatch.State = MatchState.Completed;
					}
				}
			}
			OnMatchCurrentMatchUpdated?.Invoke();
		}

		private void OnMatchReset(DataH2HMatchReset data) {
			if (PlayerStatus.Current.CurrentMatch?.MatchID == data.MatchID) {
				PlayerStatus.Current.MatchReset();
			}
			OnMatchCurrentMatchUpdated?.Invoke();
			ClearAutoLaunchInfo();
		}

		private void OnMatchUpdate(DataH2HMatchUpdate data) {
			MatchDefinition def = data.NewDef;
			bool isNew = !knownMatches.ContainsKey(def.MatchID);
			MatchState oldState = isNew ? MatchState.None : knownMatches[def.MatchID].State;
			if (isNew) {
				if (def.State == MatchState.Completed) return;
				knownMatches.Add(def.MatchID, def);
			}
			else {
				knownMatches[def.MatchID].MergeDynamic(def);
			}
			if (def.State == MatchState.Staged && (isNew || oldState != MatchState.Staged)) {
				MatchStaged(def, data.playerID.Equals(PlayerID.MyIDSafe));
			}
			else if (def.State == MatchState.InProgress && (isNew || oldState != MatchState.InProgress)) {
				if (MatchStarted(def)) {
					ClearAutoLaunchInfo();
				}
			}
			else if (def.State == MatchState.InProgress && oldState == MatchState.InProgress) {
				// Everyone dropped out
				def.CompleteIfNoRunners(false);
			}
			OnMatchCurrentMatchUpdated?.Invoke();
			DiscardStaleData();
		}

		private void OnScanRequest(DataH2HScanRequest data) {
			if (data.playerID.Equals(PlayerID.MyIDSafe)) return;
			CNetComm.Instance.SendScanResponse(data.playerID,
				!data.AutoRejoin ? null
				: knownPlayers.ContainsKey(data.playerID) ? knownPlayers[data.playerID] : null);
		}

		private void OnScanResponse(DataH2HScanResponse data) {
			if (!data.Requestor.Equals(PlayerID.MyID)) return;

			// sync data
			if (knownPlayers.ContainsKey(data.playerID)) knownPlayers[data.playerID] = data.SenderStatus;
			else knownPlayers.Add(data.playerID, data.SenderStatus);
			foreach (MatchDefinition def in data.Matches) {
				if (knownMatches.ContainsKey(def.MatchID)) knownMatches[def.MatchID].MergeDynamic(def);
				else knownMatches.Add(def.MatchID, def);
			}

			MatchDefinition curdef = PlayerStatus.Current.CurrentMatch;
			bool tryJoin = data.RequestorStatus != null && (curdef == null || curdef.PlayerCanLeaveFreely(PlayerID.MyIDSafe));
			if (!tryJoin) return;

			// try join (in progress takes priority)
			foreach (MatchDefinition def in knownMatches.Values) {
				ResultCategory cat = def.GetPlayerResultCat(PlayerID.MyIDSafe);
				if (cat == ResultCategory.InMatch
					&& def.Result[PlayerID.MyIDSafe]?.SaveFile == global::Celeste.SaveData.Instance.FileSlot)
				{
					PlayerStatus.Current.CurrentMatch = def;
					PlayerStatus.Current.Merge(data.RequestorStatus);
					if (global::Celeste.SaveData.Instance != null) {
						global::Celeste.SaveData.Instance.Time = Math.Max(
							global::Celeste.SaveData.Instance.Time,
							data.RequestorStatus.CurrentFileTimer);
					}
					PlayerStatus.Current.Updated();

					Entity wrapper = new Entity();
					wrapper.AddTag(Tags.Persistent);
					currentScenes.Last().Add(wrapper);
					GlobalAreaKey key;
					string cp;
					GetLastAreaCP(data.RequestorStatus, def, out key, out cp);
					wrapper.Add(new Coroutine(StartMatchCoroutine(key, cp)));
					return;
				}
			}
			// try join (joined, not started)
			foreach (MatchDefinition def in knownMatches.Values) {
				ResultCategory cat = def.GetPlayerResultCat(PlayerID.MyIDSafe);
				if (cat == ResultCategory.Joined) {
					PlayerStatus.Current.CurrentMatch = def;
					PlayerStatus.Current.Updated();
				}
			}
		}

		private void GetLastAreaCP(PlayerStatus stat, MatchDefinition def, out GlobalAreaKey key, out string cp) {
			key = GlobalAreaKey.Overworld;
			cp = null;
			if (stat.CurrentArea.IsOverworld) return;
			stat.CurrentMatch = def;
			int phase = stat.CurrentPhase();
			phase = Math.Min(phase, def.Phases.Count - 1);
			if (phase < 0) return;
			key = def.Phases[phase].Area;
			cp = string.IsNullOrEmpty(stat.LastCheckpoint) ? null : stat.LastCheckpoint;
		}

		private void OnChannelMove(DataChannelMove data) {
			// TODO handle channel moves
			Engine.Commands.Log("Channel Move Received for " + data.Player?.FullName);
			OnMatchCurrentMatchUpdated?.Invoke();
			DiscardStaleData();
		}

		private void OnConnected(CelesteNetClientContext cxt) {
			OnMatchCurrentMatchUpdated?.Invoke();
			//PlayerStatus.Current.CNetConnected();
		}

		private void OnDisconnected(CelesteNetConnection con) {
			OnMatchCurrentMatchUpdated?.Invoke();
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
				case StandardCategory.MoonBerry:
					mp = StandardMatches.ILMoonBerry(area);
					break;
				case StandardCategory.FullClearMoonBerry:
					mp = StandardMatches.ILFCMoonBerry(area);
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
			MatchDefinition def = PlayerStatus.Current.CurrentMatch;
			if (def != null && !def.PlayerCanLeaveFreely(PlayerID.MyIDSafe)) {
				Engine.Commands.Log("Drop out of your current match before creating a new one");
				return;
			}
			buildingMatch.AssignIDs();
			buildingMatch.State = MatchState.Staged;  // Sends update
			buildingMatch = null;
			ClearAutoLaunchInfo();
		}

		public void StageMatch(MatchDefinition def)
		{
			if (def == null)
			{
				Engine.Commands.Log("You need to build a match first...");
				return;
			}
			if (def.Phases.Count == 0)
			{
				Engine.Commands.Log("You need to add a phase first...");
				return;
			}
			if (!PlayerStatus.Current.CanStageMatch())
			{
				Engine.Commands.Log("Player status prevents staging a match (are you already in one?)");
				return;
			}
			MatchStaged(def, true);
			ClearAutoLaunchInfo();
		}

		private static void MatchStaged(MatchDefinition def, bool overrideStaged = true) {
			MatchDefinition current = PlayerStatus.Current.CurrentMatch;
			if (current != null) {
				if (!current.PlayerCanLeaveFreely(PlayerID.MyIDSafe)) return;
				if (!overrideStaged && current.GetPlayerResultCat(PlayerID.MyIDSafe) == ResultCategory.NotJoined) return;
			}
			PlayerStatus.Current.CurrentMatch = def;
			PlayerStatus.Current.MatchStaged(PlayerStatus.Current.CurrentMatch);
			OnMatchCurrentMatchUpdated?.Invoke();
			Instance.ClearAutoLaunchInfo();
		}

		public void JoinStagedMatch() {
			MatchDefinition def = PlayerStatus.Current.CurrentMatch;
			if (def == null) {
				Engine.Commands.Log("There is no staged match!");
				return;
			}
			if (PlayerStatus.Current.MatchState != MatchState.Staged) {
				Engine.Commands.Log("Current match is not staged!");
				return;
			}
			foreach (MatchPhase ph in def.Phases) {
				if (!ph.Area.ExistsLocal) {
					Engine.Commands.Log(string.Format("Could not join match - map not installed: {0}", ph.Area.SID));
					return;
				}
				if (!ph.Area.VersionMatchesLocal) {
					Engine.Commands.Log(string.Format(
						"Could not join match - map version mismatch: {0} (match initator has {1}, but {2} is installed)",
						ph.Area.DisplayName, ph.Area.Version, ph.Area.LocalVersion));
					return;
				}
			}
			if (!def.Players.Contains(PlayerID.MyIDSafe)) {
				def.Players.Add(PlayerID.MyIDSafe);
				PlayerStatus.Current.MatchJoined();
				def.BroadcastUpdate();
			}
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
			if (PlayerStatus.Current.CurrentMatch != null) {
				if (CNetComm.Instance.IsConnected) {
					CNetComm.Instance.SendMatchReset(PlayerStatus.Current.CurrentMatch.MatchID);
				}
				else {
					PlayerStatus.Current.MatchReset();
				}
			}
		}

		public void DiscardStaleData()
		{
			List<MatchDefinition> defsToRemove = new List<MatchDefinition>();
			foreach (MatchDefinition def in knownMatches.Values)
			{
				if (MatchIsStale(def))
				{
					defsToRemove.Add(def);
				}
			}
			foreach (MatchDefinition def in defsToRemove) knownMatches.Remove(def.MatchID);

			List<PlayerID> playersToRemove = new List<PlayerID>();
			foreach (KeyValuePair<PlayerID, PlayerStatus> kvp in knownPlayers)
			{
				if (kvp.Key.Equals(PlayerID.MyIDSafe)) continue;
				if (knownMatches.Any((def) => def.Value?.Players?.Contains(kvp.Key) ?? false))
				{
					playersToRemove.Add(kvp.Key);
				}
			}
			foreach (PlayerID id in playersToRemove) knownPlayers.Remove(id);
		}

		public void PurgeAllData() {
			MatchDefinition curdef = PlayerStatus.Current.CurrentMatch;
			knownMatches.Clear();
			knownPlayers.Clear();
			Instance.ClearAutoLaunchInfo();
			Instance.buildingMatch = null;
			if (curdef != null && !curdef.PlayerCanLeaveFreely(PlayerID.MyIDSafe)) {
				knownMatches.Add(curdef.MatchID, curdef);
			}
			else {
				PlayerStatus.Current.Cleanup();
			}
		}

		private bool MatchIsStale(MatchDefinition def)
		{
			if (def == null) return false;
			if (!knownMatches.ContainsKey(def.MatchID)) return false;
			if (def.MatchID == PlayerStatus.Current.CurrentMatchID) return false;
			if (def.State == MatchState.None) return true;
			if (def.State == MatchState.Completed) return true;
			if (def.State == MatchState.Building && buildingMatch.MatchID != def.MatchID) return true;
			return false;
		}

		// #######################################################

		private bool MatchStarted(MatchDefinition def) {
			foreach (MatchPhase ph in def.Phases) {
				if (!ph.Area.ExistsLocal || !ph.Area.VersionMatchesLocal) {
					return false;
				}
			}
			if (currentScenes.Count == 0) {
				Engine.Commands.Log("Cannot start match: there is no scene");
				return false;
			}
			if (PlayerStatus.Current.CurrentMatch == null) { 
				// Out of sync (data purge, crash/reload, channel switching)
				ResultCategory cat = def.GetPlayerResultCat(PlayerID.MyIDSafe);
				if (cat != ResultCategory.InMatch) return false;
				if (def.Result[PlayerID.MyIDSafe]?.SaveFile != global::Celeste.SaveData.Instance.FileSlot) return false;
				PlayerStatus.Current.CurrentMatch = def;
			}
			else if (PlayerStatus.Current.CurrentMatch.MatchID != def.MatchID) {  // Not a match we care about
				return false;
			}
			else if (!def.Players.Contains(PlayerID.MyIDSafe)) {  // Player did not join the match
				PlayerStatus.Current.CurrentMatch = null;
				PlayerStatus.Current.Updated();
				return true;
			}
			// Begin!
			Entity wrapper = new Entity();
			wrapper.AddTag(Tags.Persistent);
			currentScenes.Last().Add(wrapper);
			wrapper.Add(new Coroutine(StartMatchCoroutine(def.Phases[0].Area)));
			return true;
		}

		private IEnumerator StartMatchCoroutine(GlobalAreaKey gak, string startRoom = null) {
			if (PlayerStatus.Current.CurrentMatch == null) yield break;
			if (gak.IsOverworld) yield break;
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
				yield return (float)((startInstant - now).TotalSeconds);
			}

			if (PlayerStatus.Current.CurrentMatchID != idCheck) yield break;
			MatchDefinition def = PlayerStatus.Current.CurrentMatch;
			if (def == null) yield break;
			if (def.State != MatchState.InProgress) yield break;
			PlayerStatus.Current.MatchStarted();
			def.RegisterSaveFile();
			new FadeWipe(currentScenes.Last(), false, () => {
				LevelEnter.Go(new Session(gak.Local.Value, startRoom), false);
				// TODO send a confirmation message on load-in?

			});
		}

		public static PlayerStatus GetPlayerStatus(PlayerID id) {
			if (id.Equals(PlayerID.MyID)) return PlayerStatus.Current;
			if (knownPlayers.ContainsKey(id)) return knownPlayers[id];
			return null;
		}

		private void OnCompletedMatchPhase(PlayerStatus.OnMatchPhaseCompletedArgs args)
		{
			if (args.MatchCompleted)
			{
				if (!Settings.ReturnToLobby) return;
				autoLaunchArea = GlobalAreaKey.Head2HeadLobby;
			}
			else
			{
				if (!Settings.AutoLaunchNextPhase) return;
				if (args.NextPhase == null) return;
				if (!args.NextPhase.Area.ExistsLocal) return;
				if (!args.NextPhase.Area.VersionMatchesLocal) return;
				if (args.NextPhase.Area.Equals(GlobalAreaKey.Overworld)) return;
				autoLaunchArea = args.NextPhase.Area;
			}
			lastObjectiveType = args.CompletedObjective.ObjectiveType;
			doAutoLaunch = true;
		}

		private bool DoPostPhaseAutoLaunch(bool doFadeWipe, MatchObjectiveType ifType)
		{
			if (!doAutoLaunch) return false;
			if (lastObjectiveType != ifType) return false;
			GlobalAreaKey area = autoLaunchArea;
			ClearAutoLaunchInfo();
			if (doFadeWipe)
			{
				new FadeWipe(currentScenes.Last(), false, () => {
					LevelEnter.Go(new Session(area.Local.Value), false);
				});
			}
			else LevelEnter.Go(new Session(area.Local.Value), false);
			ClearAutoLaunchInfo();
			return true;
		}

		public void ClearAutoLaunchInfo()
		{
			lastObjectiveType = MatchObjectiveType.ChapterComplete;
			doAutoLaunch = false;
			autoLaunchArea = GlobalAreaKey.Overworld;
		}
	}

	public class Head2HeadModuleSaveData : EverestModuleSaveData {

	}

	public class Head2HeadModuleSession : EverestModuleSession {

	}
}
