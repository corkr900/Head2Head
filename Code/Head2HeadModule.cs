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
using Celeste.Mod.Head2Head.Integration;
using MonoMod.ModInterop;

// TODO Force DNF if a player intentionally closes the game
// TODO Make the start-match and return-to-lobby sequences more robust

namespace Celeste.Mod.Head2Head {
	public class Head2HeadModule : EverestModule {
		// Constants
		private const int START_TIMER_LEAD_MS = 5000;
		internal const string BTA_MATCH_PASS = "BTAMatchPass";
		internal int MatchTimeoutMinutes = 15;

		// Constants that might change in the future
		public static readonly string ProtocolVersion = "1_1_0";

		// Other static stuff
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
		private static IDetour hook_SaveData_Get_UnlockedAreas_Safe;
		private static IDetour hook_SaveData_Set_UnlockedAreas_Safe;
		private static IDetour hook_HeartGemDoor_Get_HeartGems;

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
		private int returnToSlot = -2;
		public bool PlayerEnteredAMap { get; private set; } = false;
		public bool PlayerCompletedARoom { get; private set; } = false;

		public static Dictionary<string, MatchDefinition> knownMatches = new Dictionary<string, MatchDefinition>();
		public static Dictionary<PlayerID, PlayerStatus> knownPlayers = new Dictionary<PlayerID, PlayerStatus>();

		public delegate void OnMatchCurrentMatchUpdatedHandler();
		public static event OnMatchCurrentMatchUpdatedHandler OnMatchCurrentMatchUpdated;

		// #######################################################

		public Head2HeadModule() {
			Instance = this;
		}

		public override void Load() {
			// Manual Hooks
			hook_Strawberry_orig_OnCollect = new Hook(
				typeof(Strawberry).GetMethod("orig_OnCollect", BindingFlags.Public | BindingFlags.Instance),
				typeof(Head2HeadModule).GetMethod("OnStrawberryCollect"));
			hook_OuiChapterSelectIcon_Get_IdlePosition = new Hook(
				typeof(OuiChapterSelectIcon).GetProperty("IdlePosition").GetAccessors()[0],
				typeof(Head2HeadModule).GetMethod("OnOuiChapterSelectIconGetIdlePosition"));
			hook_SaveData_Get_UnlockedAreas_Safe = new Hook(
				typeof(SaveData).GetProperty("UnlockedAreas_Safe").GetGetMethod(),
				typeof(Head2HeadModule).GetMethod("OnSaveDataGetUnlockedAreas_Safe"));
			hook_SaveData_Set_UnlockedAreas_Safe = new Hook(
				typeof(SaveData).GetProperty("UnlockedAreas_Safe").GetSetMethod(),
				typeof(Head2HeadModule).GetMethod("OnSaveDataSetUnlockedAreas_Safe"));
			hook_HeartGemDoor_Get_HeartGems = new Hook(
				typeof(HeartGemDoor).GetProperty("HeartGems").GetGetMethod(),
				typeof(Head2HeadModule).GetMethod("OnHeartGemDoorGetHeartGems"));

			// IL Hooks
			IL.Celeste.LevelLoader.LoadingThread += Level_LoadingThread;
			IL.Celeste.Level.CompleteArea_bool_bool_bool += Level_CompleteArea;

			// Monocle + Celeste Hooks
			On.Monocle.Scene.Begin += OnSceneBegin;
			On.Monocle.Scene.End += OnSceneEnd;

			On.Celeste.Level.Render += OnLevelRender;
			On.Celeste.Level.Pause += OnGamePause;
			On.Celeste.Level.UpdateTime += OnLevelUpdateTime;
			On.Celeste.Level.RegisterAreaComplete += OnLevelRegisterAreaComplete;
			On.Celeste.Level.CompleteArea_bool_bool_bool += OnLevelAreaComplete;
			On.Celeste.Player.Update += OnPlayerUpdate;
			On.Celeste.MapData.ctor += OnMapDataCtor;
			On.Celeste.Session.SetFlag += OnSessionSetFlag;
			On.Celeste.Celeste.CriticalFailureHandler += OnCelesteCriticalFailure;
			On.Celeste.Postcard.DisplayRoutine += OnPostcardDisplayRoutine;
			On.Celeste.SaveData.RegisterCassette += OnCassetteCollected;
			On.Celeste.SaveData.RegisterHeartGem += OnHeartCollected;
			On.Celeste.SaveData.Start += OnSaveDataStart;
			On.Celeste.SaveData.TryDelete += OnSaveDataTryDelete;
			On.Celeste.SaveData.BeforeSave += OnSaveDataBeforeSave;
			On.Celeste.SaveData.FoundAnyCheckpoints += OnSaveDataFoundAnyCheckpoints;
			On.Celeste.LevelData.CreateEntityData += OnLevelDataCreateEntityData;
			On.Celeste.Strawberry.ctor += OnStrawberryCtor;
			On.Celeste.LevelLoader.StartLevel += OnLevelLoaderStart;
			On.Celeste.AreaComplete.Update += OnAreaCompleteUpdate;
			On.Celeste.OverworldLoader.Begin += OnOverworldLoaderBegin;
			On.Celeste.OuiChapterPanel._GetCheckpoints += OnOUIChapterPanel_GetCheckpoints;
			On.Celeste.OuiChapterSelectIcon.Show += OnOuiChapterSelectIconShow;
			On.Celeste.UnlockEverythingThingy.EnteredCheat += OnUnlockEverythingThingyEnteredCheat;
			On.Celeste.Editor.MapEditor.ctor += onDebugScreenOpened;
			On.Celeste.Editor.MapEditor.LoadLevel += onDebugTeleport;
			On.Celeste.Mod.UI.OuiMapList.Update += OnMapListUpdate;
			On.Celeste.Mod.UI.OuiMapList.Inspect += OnMapListInspect;
			On.Celeste.Mod.UI.OuiMapSearch.Inspect += OnMapSearchInspect;
			On.Celeste.Mod.UI.OuiMapSearch.cleanExit += OnMapSearchCleanExit;
			// Everest Events
			Everest.Events.Level.OnLoadEntity += OnLevelLoadEntity;
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
			CNetComm.OnReceiveMisc += OnMiscMessage;
			PlayerStatus.OnMatchPhaseCompleted += OnCompletedMatchPhase;
			// Misc other setup
			Celeste.Instance.Components.Add(Comm = new CNetComm(Celeste.Instance));
			Logger.SetLogLevel("Head2Head", LogLevel.Info);
			Logger.SetLogLevel("Head2Head.Error", LogLevel.Error);
			Logger.SetLogLevel("Head2Head.Warn", LogLevel.Warn);
			Logger.SetLogLevel("Head2Head.Custom", LogLevel.Warn);
			typeof(Head2HeadAPI).ModInterop();
			CollabUtils2Integration.Load();
		}

		public override void Unload() {
			// Manual Hooks
			hook_Strawberry_orig_OnCollect?.Dispose();
			hook_Strawberry_orig_OnCollect = null;
			hook_OuiChapterSelectIcon_Get_IdlePosition?.Dispose();
			hook_OuiChapterSelectIcon_Get_IdlePosition = null;
			hook_SaveData_Get_UnlockedAreas_Safe?.Dispose();
			hook_SaveData_Get_UnlockedAreas_Safe = null;
			hook_SaveData_Set_UnlockedAreas_Safe?.Dispose();
			hook_SaveData_Set_UnlockedAreas_Safe = null;
			hook_HeartGemDoor_Get_HeartGems?.Dispose();
			hook_HeartGemDoor_Get_HeartGems = null;

			// IL Hooks
			IL.Celeste.LevelLoader.LoadingThread -= Level_LoadingThread;
			IL.Celeste.Level.CompleteArea_bool_bool_bool -= Level_CompleteArea;

			// Monocle + Celeste Hooks
			On.Monocle.Scene.Begin -= OnSceneBegin;
			On.Monocle.Scene.End -= OnSceneEnd;

			On.Celeste.Level.Render -= OnLevelRender;
			On.Celeste.Level.Pause -= OnGamePause;
			On.Celeste.Level.UpdateTime -= OnLevelUpdateTime;
			On.Celeste.Level.RegisterAreaComplete -= OnLevelRegisterAreaComplete;
			On.Celeste.Level.CompleteArea_bool_bool_bool -= OnLevelAreaComplete;
			On.Celeste.Player.Update -= OnPlayerUpdate;
			On.Celeste.MapData.ctor -= OnMapDataCtor;
			On.Celeste.Session.SetFlag -= OnSessionSetFlag;
			On.Celeste.Celeste.CriticalFailureHandler -= OnCelesteCriticalFailure;
			On.Celeste.Postcard.DisplayRoutine -= OnPostcardDisplayRoutine;
			On.Celeste.SaveData.RegisterCassette -= OnCassetteCollected;
			On.Celeste.SaveData.RegisterHeartGem -= OnHeartCollected;
			On.Celeste.SaveData.Start -= OnSaveDataStart;
			On.Celeste.SaveData.FoundAnyCheckpoints -= OnSaveDataFoundAnyCheckpoints;
			On.Celeste.SaveData.TryDelete -= OnSaveDataTryDelete;
			On.Celeste.SaveData.BeforeSave -= OnSaveDataBeforeSave;
			On.Celeste.LevelData.CreateEntityData -= OnLevelDataCreateEntityData;
			On.Celeste.Strawberry.ctor -= OnStrawberryCtor;
			On.Celeste.LevelLoader.StartLevel -= OnLevelLoaderStart;
			On.Celeste.AreaComplete.Update -= OnAreaCompleteUpdate;
			On.Celeste.OverworldLoader.Begin -= OnOverworldLoaderBegin;
			On.Celeste.OuiChapterPanel._GetCheckpoints -= OnOUIChapterPanel_GetCheckpoints;
			On.Celeste.OuiChapterSelectIcon.Show -= OnOuiChapterSelectIconShow;
			On.Celeste.UnlockEverythingThingy.EnteredCheat -= OnUnlockEverythingThingyEnteredCheat;
			On.Celeste.Editor.MapEditor.ctor -= onDebugScreenOpened;
			On.Celeste.Editor.MapEditor.LoadLevel -= onDebugTeleport;
			On.Celeste.Mod.UI.OuiMapList.Update -= OnMapListUpdate;
			On.Celeste.Mod.UI.OuiMapList.Inspect -= OnMapListInspect;
			On.Celeste.Mod.UI.OuiMapSearch.cleanExit -= OnMapSearchCleanExit;
			On.Celeste.Mod.UI.OuiMapSearch.Inspect -= OnMapSearchInspect;
			// Everest Events
			Everest.Events.Level.OnLoadEntity -= OnLevelLoadEntity;
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
			CNetComm.OnReceiveMisc -= OnMiscMessage;
			PlayerStatus.OnMatchPhaseCompleted -= OnCompletedMatchPhase;
			// Misc other cleanup
			if (Celeste.Instance.Components.Contains(Comm))
				Celeste.Instance.Components.Remove(Comm);
		}

		public override void CreateModMenuSection(TextMenu menu, bool inGame, FMOD.Studio.EventInstance snapshot)
		{
			base.CreateModMenuSection(menu, inGame, snapshot);
			Settings.CreateOptions(menu, inGame, snapshot);
		}

		// ###############################################

		/// <summary>
		/// Manipulates entity loading to hide golden berries when not in a head 2 head match
		/// (or to force them to appear if the match requires it)
		/// </summary>
		/// <returns>false to let entity loading continue as normal, true to intercept the other loading logic</returns>
		private bool OnLevelLoadEntity(Level level, LevelData levelData, Vector2 offset, EntityData entityData) {
			if (entityData.Name != "goldenBerry") return false;  // We only care about golden berries
			if (!PlayerStatus.Current.IsInMatch(false)) return false;  // Don't mess with anything if we're not in a match
			MatchObjective ob = PlayerStatus.Current.FindObjective(MatchObjectiveType.GoldenStrawberry, new GlobalAreaKey(level.Session.Area));
			if (ob == null) return true;  // If we don't need the golden, do nothing but say it's handled; prevents the berry from loading in.
			level.Add(new Strawberry(entityData, offset, new EntityID(levelData.Name, entityData.ID)));  // Force it to appear if we need it
			return true;
		}

		private void OnUnlockEverythingThingyEnteredCheat(On.Celeste.UnlockEverythingThingy.orig_EnteredCheat orig, UnlockEverythingThingy self) {
			if (PlayerStatus.Current.IsInMatch(true)) {
				MatchDefinition def = PlayerStatus.Current.CurrentMatch;
				if (def?.AllowCheatMode == false) {
					def.PlayerDNF(DNFReason.CheatMode);
				}
			}
			orig(self);
		}

		private void OnCelesteCriticalFailure(On.Celeste.Celeste.orig_CriticalFailureHandler orig, Exception e) {
			orig(e);
			// TODO Cache off recovery data locally instead of relying on peers' info
		}

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
			currentSession = session;
			GlobalAreaKey area = new GlobalAreaKey(session.Area);
			PlayerStatus.Current.ChapterEntered(area, session);
			if (!area.Equals(GlobalAreaKey.Head2HeadLobby) && !area.Equals(GlobalAreaKey.Overworld)) {
				PlayerEnteredAMap = true;
			}
			ActionLogger.EnteringArea();
		}

		private void onLevelExit(Level level, LevelExit exit, LevelExit.Mode mode, Session session, HiresSnow snow) {
			currentSession = null;
			ActionLogger.ExitingArea(mode);
			PlayerStatus.Current.ChapterExited(mode, session);
		}

		private void OnLevelLoaderStart(On.Celeste.LevelLoader.orig_StartLevel orig, LevelLoader self) {
			orig(self);
			PlayerStatus.Current.OnLevelLoaderStart(self);
		}

		private void onRoomTransition(Level level, LevelData next, Vector2 direction) {
			PlayerStatus.Current.RoomEntered(level, next, direction);
			if (PlayerEnteredAMap && !(new GlobalAreaKey(level.Session.Area).Equals(GlobalAreaKey.Head2HeadLobby))) {
				PlayerCompletedARoom = true;
			}
			//ActionLogger.EnteringRoom();
		}

		private void onDebugScreenOpened(On.Celeste.Editor.MapEditor.orig_ctor orig, MapEditor self, AreaKey area, bool reloadMapData) {
			orig(self, area, reloadMapData);
			PlayerStatus.Current.DebugOpened(self, new GlobalAreaKey(area), reloadMapData);
			ActionLogger.DebugView();
		}
		
		private void onDebugTeleport(On.Celeste.Editor.MapEditor.orig_LoadLevel orig, MapEditor self, LevelTemplate level, Vector2 at) {
			orig(self, level, at);
			PlayerStatus.Current.DebugTeleport(self, level, at);
			MatchDefinition def = PlayerStatus.Current.CurrentMatch;
			if (def != null) {
				ResultCategory cat = def.GetPlayerResultCat(PlayerID.MyIDSafe);
				if (cat == ResultCategory.InMatch) {
					def.PlayerDNF(DNFReason.DebugTeleport);
				}
			}
			ActionLogger.DebugTeleport();
		}

		private void OnExiting() {
			ActionLogger.ClosingApplication();
			ActionLogger.PurgeOldLogs();
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
			PlayerStatus.Current.StrawberryCollected(gak, self);
			Instance.DoPostPhaseAutoLaunch(true, MatchObjective.GetTypeForStrawberry(self));
		}

		public static Vector2 OnOuiChapterSelectIconGetIdlePosition(Func<OuiChapterSelectIcon, Vector2> orig, OuiChapterSelectIcon self) {
			return self is OuiRunSelectILChapterIcon icon ? icon.IdlePositionOverride : orig(self);
		}

		private void OnHeartCollected(On.Celeste.SaveData.orig_RegisterHeartGem orig, SaveData self, AreaKey area) {
			orig(self, area);
			PlayerStatus.Current.HeartCollected(new GlobalAreaKey(area));
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
			// heart
			if (!dd.Data.ContainsKey("HasRealHeartGem")) {
				dd.Set("HasRealHeartGem", Util.EntityIsRealHeartGem(entity));
			}
			else if(!dd.Get<bool>("HasRealHeartGem") && Util.EntityIsRealHeartGem(entity)) {
				dd.Set("HasRealHeartGem", true);
			}
			// cassette
			if (!dd.Data.ContainsKey("HasCassette")) {
				dd.Set("HasCassette", Util.EntityIsCassette(entity));
			}
			else if (!dd.Get<bool>("HasCassette") && Util.EntityIsCassette(entity)) {
				dd.Set("HasCassette", true);
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
			switch (Settings.ShowHelpdeskInPause) {
				case Head2HeadModuleSettings.ShowHelpdeskInPauseMenu.Always:
					break;
				case Head2HeadModuleSettings.ShowHelpdeskInPauseMenu.Online:
					if (!CNetComm.Instance.IsConnected) return;
					break;
				case Head2HeadModuleSettings.ShowHelpdeskInPauseMenu.InMatchOrLobby:
					if (PlayerStatus.Current.CurrentMatch == null
						&& !PlayerStatus.Current.CurrentArea.Equals(GlobalAreaKey.Head2HeadLobby)) return;
					break;
				case Head2HeadModuleSettings.ShowHelpdeskInPauseMenu.InMatch:
					if (PlayerStatus.Current.CurrentMatch == null) return;
					break;
				default:
					return;
			}
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
			TextMenu.Button h2hHelpdeskButton = new TextMenu.Button(Dialog.Clean("Head2Head_menu_helpdesk"));
			h2hHelpdeskButton.Pressed(() => {
				level.PauseMainMenuOpen = false;
				new Menus.HelpdeskMenuContext(level, menu.Selection, true).GoTo(Menus.Helpdesk, menu);
			});
			h2hHelpdeskButton.ConfirmSfx = "event:/ui/main/message_confirm";
			menu.Insert(returnToMapIndex + 1, h2hHelpdeskButton);
		}

		private void OnSaveDataStart(On.Celeste.SaveData.orig_Start orig, SaveData data, int slot) {
			if (PlayerStatus.Current.IsInMatch(false)) {
				int matchslot = PlayerStatus.Current.GetMatchSaveFile();
				if (matchslot != int.MinValue && matchslot != slot) {
					PlayerStatus.Current.CurrentMatch?.PlayerDNF(DNFReason.ChangeFile);
				}
			}
			orig(data, slot);
			ActionLogger.EnteredSavefile();
		}

		private bool OnSaveDataTryDelete(On.Celeste.SaveData.orig_TryDelete orig, int slot) {
			if (PlayerStatus.Current.IsInMatch(false)) {
				PlayerStatus.Current.CurrentMatch?.PlayerDNF(DNFReason.DeleteFile);
			}
			if (orig(slot)) {
				ActionLogger.DeletedSavefile();
				return true;
			}
			return false;
		}

		private bool OnSaveDataFoundAnyCheckpoints(On.Celeste.SaveData.orig_FoundAnyCheckpoints orig, SaveData self, AreaKey area) {
			// This might interact weirdly with CollabUtils. Shouldn't be an issue though because you won't use a lobby in a match.
			MatchDefinition def = PlayerStatus.Current.CurrentMatch;
			if (def != null && def.GetPlayerResultCat(PlayerID.MyIDSafe) == ResultCategory.InMatch) {
				return PlayerStatus.Current.reachedCheckpoints.Any((Tuple<GlobalAreaKey, string> t) => t.Item1.Equals(new GlobalAreaKey(area)));
			}
			else return orig(self, area);
		}

		private HashSet<string> OnOUIChapterPanel_GetCheckpoints(On.Celeste.OuiChapterPanel.orig__GetCheckpoints orig, SaveData save, AreaKey area) {
			// This might interact weirdly with CollabUtils. Shouldn't be an issue though because you won't use a lobby in a match.
			MatchDefinition def = PlayerStatus.Current.CurrentMatch;
			if (def != null && def.GetPlayerResultCat(PlayerID.MyIDSafe) == ResultCategory.InMatch) {
				HashSet<string> set = new HashSet<string>();
				GlobalAreaKey gak = new GlobalAreaKey(area);
				foreach (Tuple<GlobalAreaKey, string> t in PlayerStatus.Current.reachedCheckpoints) {
					if (t.Item1.Equals(gak)) set.Add(t.Item2);
				}
				return set;
			}
			else return orig(save, area);
		}

		public static int OnSaveDataGetUnlockedAreas_Safe(Func<SaveData, int> orig, SaveData self) {
			// Show all chapters in file select when in IL match so that RTM doesnt cause issues in a fresh savefile
			MatchDefinition def = PlayerStatus.Current.CurrentMatch;
			if (def != null && !def.UseFreshSavefile && def.GetPlayerResultCat(PlayerID.MyIDSafe) == ResultCategory.InMatch) {
				return self.LevelSetStats.AreaOffset + self.LevelSetStats.MaxArea;
			}
			else return orig(self);
		}

		public static void OnSaveDataSetUnlockedAreas_Safe(Action<SaveData, int> orig, SaveData self, int val) {
			MatchDefinition def = PlayerStatus.Current.CurrentMatch;
			if (def != null && def.GetPlayerResultCat(PlayerID.MyIDSafe) == ResultCategory.InMatch) {
				// Don't update the unlocked areas while in IL match because we're exposing all of them anyway
				if (!def.UseFreshSavefile) return;
				// Figure out what was unlocked and send it to PlayerStatus
				int minCheck = self.UnlockedAreas_Safe + 1 + self.LevelSetStats.AreaOffset;
				int maxCheck = Calc.Clamp(val, 0, self.LevelSetStats.MaxArea - 1) + self.LevelSetStats.AreaOffset;
				orig(self, val);  // Write the update before processing match progress
				for (int id = minCheck; id <= maxCheck; id++) {
					string SID = AreaData.Areas[id].SID;
					PlayerStatus.Current.ChapterUnlocked(new GlobalAreaKey(id));
				}
			}
			else orig(self, val);
		}

		public static int OnHeartGemDoorGetHeartGems(Func<HeartGemDoor, int> orig, HeartGemDoor self) {
			// Allow all heart gem doors to open while in a match
			MatchDefinition def = PlayerStatus.Current.CurrentMatch;
			if (def != null && def.GetPlayerResultCat(PlayerID.MyIDSafe) == ResultCategory.InMatch) {
				return self.Requires;
			}
			else return orig(self);
		}

		private void OnLevelRegisterAreaComplete(On.Celeste.Level.orig_RegisterAreaComplete orig, Level self) {
			ActionLogger.AreaCompleted();
			PlayerStatus.Current.ChapterCompleted(new GlobalAreaKey(self.Session.Area));
			orig(self);
		}

		private void OnOverworldLoaderBegin(On.Celeste.OverworldLoader.orig_Begin orig, OverworldLoader self) {
			if (!DoPostPhaseAutoLaunch(false)) {
				orig(self);
			}
		}

		private void OnSaveDataBeforeSave(On.Celeste.SaveData.orig_BeforeSave orig, SaveData self) {
			orig(self);
			ActionLogger.WriteLog();
		}

		private void OnPlayerUpdate(On.Celeste.Player.orig_Update orig, Player self) {
			if (self.Scene is Level level) {
				PlayerStatus.Current.CheckForTimeLimit(new GlobalAreaKey(level.Session.Area));
			}
			orig(self);
		}

		private void OnSessionSetFlag(On.Celeste.Session.orig_SetFlag orig, Session self, string flag, bool setTo) {
			orig(self, flag, setTo);
			if (setTo) {
				PlayerStatus.Current.CheckFlagObjective(flag, new GlobalAreaKey(self.Area));
			}
		}

		private void OnStrawberryCtor(On.Celeste.Strawberry.orig_ctor orig, Strawberry self, EntityData data, Vector2 offset, EntityID gid) {
			orig(self, data, offset, gid);
			if (self.Golden && self.Winged) {
				DynamicData dd = new DynamicData(self);
				dd.Set("IsWingedGolden", true);
			}
		}

		// ########################################

		private void OnPlayerStatusUpdate(DataH2HPlayerStatus data) {
			if (!data.playerID.Equals(PlayerID.MyID)) {
				if (knownPlayers.ContainsKey(data.playerID)) {
					knownPlayers[data.playerID] = data.Status;
				}
				else knownPlayers.Add(data.playerID, data.Status);
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
			bool isNew = !knownMatches.ContainsKey(data.NewDef.MatchID);
			MatchState oldState = isNew ? MatchState.None : knownMatches[data.NewDef.MatchID].State;
			if (isNew) {
				if (data.NewDef.State == MatchState.Completed) return;
				knownMatches.Add(data.NewDef.MatchID, data.NewDef);
			}
			else {
				knownMatches[data.NewDef.MatchID].MergeDynamic(data.NewDef);
			}
			MatchDefinition def = knownMatches[data.NewDef.MatchID];
			if (def.State == MatchState.Staged && (isNew || oldState != MatchState.Staged)) {
				MatchStaged(def, data.playerID.Equals(PlayerID.MyIDSafe));
			}
			else if (def.State == MatchState.InProgress && (isNew || oldState < MatchState.InProgress)) {
				if (MatchStarted(def)) {
					ClearAutoLaunchInfo();
				}
			}
			if (def.State == MatchState.InProgress) {
				// Everyone dropped out
				def.CompleteIfNoRunners();
			}
			OnMatchCurrentMatchUpdated?.Invoke();
			DiscardStaleData();
		}

		private void OnScanRequest(DataH2HScanRequest data) {
			if (data.playerID.Equals(PlayerID.MyIDSafe)) return;
			MatchDefinition def = PlayerStatus.Current.CurrentMatch;
			if (def == null) return;
			CNetComm.Instance.SendScanResponse(data.playerID, def,
				!data.AutoRejoin ? null
				: knownPlayers.ContainsKey(data.playerID) ? knownPlayers[data.playerID] : null);
		}

		private void OnScanResponse(DataH2HScanResponse data) {
			if (!data.Requestor.Equals(PlayerID.MyID)) return;

			// sync data
			if (knownPlayers.ContainsKey(data.playerID)) knownPlayers[data.playerID] = data.SenderStatus;
			else knownPlayers.Add(data.playerID, data.SenderStatus);
			MatchDefinition def = data.MatchDef;
			if (knownMatches.ContainsKey(def.MatchID)) knownMatches[def.MatchID].MergeDynamic(def);
			else knownMatches.Add(def.MatchID, def);

			MatchDefinition curdef = PlayerStatus.Current.CurrentMatch;
			bool tryJoin = data.RequestorStatus != null && (curdef == null || curdef.PlayerCanLeaveFreely(PlayerID.MyIDSafe));
			if (!tryJoin) return;

			// try join (in progress takes priority)
			if (RejoinMatch(def, data.RequestorStatus)) return;
			// try join (joined, not started)
			ResultCategory cat = def.GetPlayerResultCat(PlayerID.MyIDSafe);
			if (cat == ResultCategory.Joined) {
				PlayerStatus.Current.CurrentMatch = def;
				PlayerStatus.Current.Updated();
			}
		}

		private void OnMiscMessage(DataH2HMisc data) {
			switch (data.message) {
				default:
					return;

				case BTA_MATCH_PASS:
					if (data.targetPlayer.Equals(PlayerID.MyID)) {
						Role.GiveBTAMatchPass();
						OnMatchCurrentMatchUpdated?.Invoke();
					}
					return;
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
			if (data.Player?.ID == CelesteNetClientModule.Instance?.Client?.PlayerInfo?.ID) {
				MatchDefinition def = PlayerStatus.Current.CurrentMatch;
				if (def != null && def.GetPlayerResultCat(PlayerID.MyIDSafe) == ResultCategory.InMatch) {
					def.PlayerDNF(DNFReason.ChangeChannel);
				}
				PurgeAllData();
			}
			else {
				DiscardStaleData();
			}
			CNetComm.Instance.SendScanRequest(false);
			OnMatchCurrentMatchUpdated?.Invoke();
		}

		private void OnConnected(CelesteNetClientContext cxt) {
			OnMatchCurrentMatchUpdated?.Invoke();
			CNetComm.Instance.SendScanRequest(false);
		}

		private void OnDisconnected(CelesteNetConnection con) {
			OnMatchCurrentMatchUpdated?.Invoke();
		}

		// #######################################################

		public void ScanModsForIntegrationMeta() {
			foreach (ModContent mod in Everest.Content.Mods) {
				if (mod.Map.ContainsKey("Head2Head")) {
					ModAsset asset = mod.Map["Head2Head"];
					if (asset.TryDeserialize(out ModIntegrationMeta meta)) {
						ProcessIntegrationMeta(meta);
					}
				}
			}
		}

		private void ProcessIntegrationMeta(ModIntegrationMeta meta) {
			if (meta.IndividualLevels != null) {
				foreach (ILMeta il in meta.IndividualLevels) {
					// Ensure the area is valid
					GlobalAreaKey area = new GlobalAreaKey(il.Map, CustomMatchTemplate.GetAreaMode(il.Side));
					if (!area.ExistsLocal || area.IsOverworld) continue;

					// Handle IL removals
					if (il.RemoveCategories != null) {
						foreach (string s in il.RemoveCategories) {
							StandardCategory cat;
							if (Enum.TryParse(s, out cat) && cat != StandardCategory.Custom) {
								ILSelector.SuppressCategory(area, cat);
							}
						}
					}

					// Handle IL additions
					if (il.AddCategories != null) {
						foreach (CategoryMeta newcat in il.AddCategories) {
							CustomMatchTemplate.AddTemplateFromMeta(newcat, area);
						}
					}
				}
			}
		}

		public bool CanBuildMatch() {
			if (!CNetComm.Instance.IsConnected) return false;
			if (CNetComm.Instance.CurrentChannelIsMain) return false;
			if (!Role.AllowMatchCreate()) return false;
			if (Util.IsUpdateAvailable()) return false;
			return PlayerStatus.Current.CanStageMatch();
		}

		public bool CanStageMatch() {
			if (!CanBuildMatch()) return false;
			if (buildingMatch == null) return false;
			if (buildingMatch.Phases.Count == 0) return false;
			return true;
		}

		public bool CanJoinMatch() {
			if (Util.IsUpdateAvailable()) return false;
			MatchDefinition def = PlayerStatus.Current.CurrentMatch;
			if (def == null) return false;
			if (def.State != MatchState.Staged) return false;
			if (def.Players.Contains(PlayerID.MyIDSafe)) return false;
			if (!Role.AllowMatchJoin(def)) return false;
			if (def.VersionCheck() != null) return false;
			return true;
		}

		public bool CanStartMatch() {
			if (Util.IsUpdateAvailable()) return false;
			if (PlayerStatus.Current.CurrentMatch == null) return false;
			if (PlayerStatus.Current.MatchState != MatchState.Staged) return false;
			bool hasJoined = PlayerStatus.Current.CurrentMatch.Players.Contains(PlayerID.MyIDSafe);
			if (!Role.AllowMatchStart(hasJoined)) return false;
			return true;
		}

		public void StartMatchBuild() {
			if (Util.IsUpdateAvailable()) {
				Logger.Log("Head2Head.Warn", "Cannot build match: you are using an outdated version of Head 2 Head");
				return;
			}
			if (!Role.AllowMatchCreate()) {
				Logger.Log("Head2Head", "Your role prevents building a match");
				return;
			}
			if (!(CNetComm.Instance?.IsConnected ?? false)) {
				//Logger.Log("Head2Head", "Connect to CelesteNet before building a match");
				return;
			}
			buildingMatch = new MatchDefinition() {
				Owner = PlayerID.MyID ?? PlayerID.Default,
				CreationInstant = DateTime.Now,
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

				// Specialty Categories
				case StandardCategory.OneThirdBerries:
					mp = StandardMatches.ILOneThirdBerries(area);
					break;
				case StandardCategory.OneFifthBerries:
					mp = StandardMatches.ILOneFifthBerries(area);
					break;
				case StandardCategory.TimeLimit:
					mp = StandardMatches.ILTimeLimit(area, Util.TimeValueInternal(MatchTimeoutMinutes, 0));
					break;
			}
			if (mp == null) {
				Logger.Log("Head2Head", string.Format("Couldn't add {0} ({1}) - Category is not valid for this chapter", area.DisplayName, category));
				return;
			}
			else {
				AddMatchPhase(mp);
			}
		}

		public void AddMatchPhase(MatchPhase mp) {
			if (buildingMatch == null) {
				StartMatchBuild();
			}
			if (buildingMatch == null) {
				return;
			}
			buildingMatch.Phases.Add(mp);
		}

		public void NameBuildingMatch(string name) {
			if (buildingMatch == null) {
				return;
			}
			buildingMatch.CategoryDisplayNameOverride = Util.TranslatedIfAvailable(name);
		}

		public void StageMatch() {
			if (Util.IsUpdateAvailable()) {
				Logger.Log("Head2Head.Warn", "Cannot stage match: you are using an outdated version of Head 2 Head");
				return;
			}
			if (!Role.AllowMatchCreate()) {
				Logger.Log("Head2Head", "Your role prevents creating a match");
				return;
			}
			if (buildingMatch == null) {
				Logger.Log("Head2Head", "You need to build a match first");
				return;
			}
			if (buildingMatch.Phases.Count == 0) {
				Logger.Log("Head2Head", "You need to add a phase first");
				return;
			}
			MatchDefinition def = PlayerStatus.Current.CurrentMatch;
			if (def != null && !def.PlayerCanLeaveFreely(PlayerID.MyIDSafe)) {
				//Logger.Log("Head2Head", "Drop out of your current match before creating a new one");
				return;
			}
			buildingMatch.AssignIDs();
			Role.HandleMatchCreation(buildingMatch);
			buildingMatch.State = MatchState.Staged;  // Sends update
			buildingMatch = null;
			ClearAutoLaunchInfo();
		}

		public void StageMatch(MatchDefinition def) {
			if (Util.IsUpdateAvailable()) {
				Logger.Log("Head2Head.Warn", "Cannot stage match: you are using an outdated version of Head 2 Head");
				return;
			}
			if (def == null)
			{
				Logger.Log("Head2Head", "You need to build a match before staging a match");
				return;
			}
			if (def.Phases.Count == 0)
			{
				Logger.Log("Head2Head", "You need to add a phase before staging a match");
				return;
			}
			if (!PlayerStatus.Current.CanStageMatch())
			{
				Logger.Log("Head2Head", "Player status prevents staging a match (are you already in one?)");
				return;
			}
			MatchStaged(def, true);
			ClearAutoLaunchInfo();
		}

		private static void MatchStaged(MatchDefinition def, bool overrideSoftChecks) {
			if (!overrideSoftChecks) {
				if (Settings.AutoStageNewMatches == Head2HeadModuleSettings.AutoStageSetting.Never) return;
				if (Settings.AutoStageNewMatches == Head2HeadModuleSettings.AutoStageSetting.OnlyInLobby
					&& !PlayerStatus.Current.CurrentArea.Equals(GlobalAreaKey.Head2HeadLobby)) return;
				if (!Role.AllowAutoStage(def)) return;
			}
			MatchDefinition current = PlayerStatus.Current.CurrentMatch;
			if (current != null) {
				if (!current.PlayerCanLeaveFreely(PlayerID.MyIDSafe)) return;
				if (!overrideSoftChecks
					&& current.State <= MatchState.InProgress
					&& current.GetPlayerResultCat(PlayerID.MyIDSafe) == ResultCategory.NotJoined) return;
			}
			// Actually stage it locally
			PlayerStatus.Current.CurrentMatch = def;
			PlayerStatus.Current.MatchStaged(PlayerStatus.Current.CurrentMatch);
			OnMatchCurrentMatchUpdated?.Invoke();
			Instance.ClearAutoLaunchInfo();
		}

		public void JoinStagedMatch() {
			if (Util.IsUpdateAvailable()) {
				Logger.Log("Head2Head.Warn", "Cannot join match: you are using an outdated version of Head 2 Head");
				return;
			}
			MatchDefinition def = PlayerStatus.Current.CurrentMatch;
			if (def == null) {
				Logger.Log("Head2Head", "Couldn't join match - there is no staged match");
				return;
			}
			if (PlayerStatus.Current.MatchState != MatchState.Staged) {
				Logger.Log("Head2Head", "Couldn't join match - current match is not staged status");
				return;
			}
			foreach (MatchPhase ph in def.Phases) {
				if (!ph.Area.ExistsLocal) {
					Engine.Commands.Log(string.Format("Couldn't join match - map not installed: {0}", ph.Area.SID));
					return;
				}
				if (!ph.Area.VersionMatchesLocal) {
					Engine.Commands.Log(string.Format("Couldn't join match - map version mismatch: {0} (match initator has {1}, but {2} is installed)",
						ph.Area.DisplayName, ph.Area.Version, ph.Area.LocalVersion));
					return;
				}
			}
			if (!Role.AllowMatchJoin(def)) {
				Engine.Commands.Log("Your role prevents joining this match");
				return;
			}
			if (!def.Players.Contains(PlayerID.MyIDSafe)) {
				def.Players.Add(PlayerID.MyIDSafe);
				PlayerStatus.Current.MatchJoined();
				def.BroadcastUpdate();
			}
		}

		public void BeginStagedMatch() {
			if (Util.IsUpdateAvailable()) {
				Logger.Log("Head2Head.Warn", "Cannot begin match: you are using an outdated version of Head 2 Head");
				return;
			}
			MatchDefinition def = PlayerStatus.Current.CurrentMatch;
			if (def == null) {
				Logger.Log("Head2Head", "There is no staged match!");
				return;
			}
			if (def.State != MatchState.Staged) {
				Logger.Log("Head2Head", "Current match is not staged!");
				return;
			}
			bool hasJoined = def.Players.Contains(PlayerID.MyIDSafe);
			if (!Role.AllowMatchStart(hasJoined)) {
				Logger.Log("Head2Head", "Your role prevents starting this match");
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
			Role.RemoveBTAPass();
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
			if (def.State == MatchState.Staged && (DateTime.Now - def.CreationInstant).TotalMinutes > 20) return true;
			return false;
		}

		// #######################################################

		private bool MatchStarted(MatchDefinition def) {
			if (def == null) return false;
			foreach (MatchPhase ph in def.Phases) {
				if (ph == null) {
					Logger.Log("Head2Head", "Cannot start match: match contains a null phase");
					return false;
				}
				if (!ph.Area.ExistsLocal || !ph.Area.VersionMatchesLocal) {
					return false;
				}
			}
			if (currentScenes.Count == 0) {
				Logger.Log("Head2Head", "Cannot start match: there is no scene");
				return false;
			}
			if (PlayerStatus.Current.CurrentMatch == null) { 
				// Out of sync (data purge, crash/reload, channel switching)
				ResultCategory cat = def.GetPlayerResultCat(PlayerID.MyIDSafe);
				if (cat != ResultCategory.InMatch) return false;
				if (def.Result == null) {
					def.RegisterSaveFile();
				}
				if (def.Result[PlayerID.MyIDSafe]?.SaveFile != global::Celeste.SaveData.Instance?.FileSlot) return false;
				PlayerStatus.Current.CurrentMatch = def;
			}
			else if (PlayerStatus.Current.CurrentMatch.MatchID != def.MatchID) {  // Not a match we care about
				return false;
			}
			else if (!def.Players.Contains(PlayerID.MyIDSafe)) {  // Player did not join the match
				if (Role.LeaveUnjoinedMatchOnStart()) {
					PlayerStatus.Current.CurrentMatch = null;
					PlayerStatus.Current.Updated();
					return true;
				}
			}
			// Begin!
			Level level = GetLevelForCoroutine();
			ForceUnpause(level);
			Entity wrapper = new Entity();
			wrapper.AddTag(Tags.Persistent);
			level?.Add(wrapper);
			wrapper.Add(new Coroutine(StartMatchCoroutine(def.Phases[0].Area, false)));
			return true;
		}

		internal bool RejoinMatch(MatchDefinition def, PlayerStatus requestorStatus) {
			ResultCategory cat = def.GetPlayerResultCat(PlayerID.MyIDSafe);
			if (cat == ResultCategory.InMatch
					&& def.Result[PlayerID.MyIDSafe]?.SaveFile == global::Celeste.SaveData.Instance.FileSlot) {
				PlayerStatus.Current.CurrentMatch = def;
				PlayerStatus.Current.Merge(requestorStatus);
				PlayerStatus.Current.Updated();

				if (global::Celeste.SaveData.Instance.Time < requestorStatus.FileTimerAtLastCheckpoint) {
					global::Celeste.SaveData.Instance.Time = requestorStatus.FileTimerAtLastCheckpoint;
				}
				Level level = GetLevelForCoroutine();
				ForceUnpause(level);
				Entity wrapper = new Entity();
				wrapper.AddTag(Tags.Persistent);
				level?.Add(wrapper);
				GlobalAreaKey key;
				string cp;
				GetLastAreaCP(requestorStatus, def, out key, out cp);
				ActionLogger.RejoinMatch(def.MatchID);
				wrapper.Add(new Coroutine(StartMatchCoroutine(key, true, cp)));
				return true;
			}
			return false;
		}

		private Level GetLevelForCoroutine() {
			for (int i = currentScenes.Count - 1; i >= 0; i--) {
				Scene s = currentScenes[i];
				if (s == null) continue;
				if (s is Level) return s as Level;
			}
			return null;
		}

		private void ForceUnpause(Level level) {
			if (level == null) return;
			DynamicData dd = new DynamicData(level);
			dd.Set("unpauseTimer", 0.15f);
			level.Paused = false;
			foreach (Entity e in level.Entities) {
				if (e is TextMenu menu) {
					menu.RemoveSelf();
				}

			}
		}

		private IEnumerator StartMatchCoroutine(GlobalAreaKey gak, bool isRejoin, string startRoom = null) {
			if (PlayerStatus.Current.CurrentMatch == null) yield break;
			string idCheck = PlayerStatus.Current.CurrentMatchID;
			DateTime startInstant = PlayerStatus.Current.CurrentMatch.BeginInstant;
			DateTime now = DateTime.Now;
			if (startInstant > now + new TimeSpan(0, 0, 15)) {
				Logger.Log("Head2Head", "Match begins more than 15 seconds in the future; skipping countdown (try syncing your system's clock)");
			}
			else if (startInstant < now) {
				Logger.Log("Head2Head", "Match begins in the past; skipping countdown (if this is not a rejoin, try syncing your system's clock)");
			}
			else if (!Role.SkipCountdown()) {
				Level level = GetLevelForCoroutine();
				if (level != null) level.PauseLock = true;
				yield return (float)((startInstant - now).TotalSeconds);
				if (level != null) level.PauseLock = false;
			}

			if (PlayerStatus.Current.CurrentMatchID != idCheck) yield break;
			MatchDefinition def = PlayerStatus.Current.CurrentMatch;
			if (def == null) yield break;
			if (def.State != MatchState.InProgress) yield break;
			if (!isRejoin) {
				PlayerStatus.Current.FileSlotBeforeMatchStart = global::Celeste.SaveData.Instance.FileSlot;
				if (def.UseFreshSavefile) {
					int slot = FindNextUnusedSlot();
					if (slot >= 0) {
						// Create new savefile
						global::Celeste.SaveData.Start(new SaveData {
							Name = "[H2H] " + PlayerID.MyIDSafe.Name,
							AssistMode = false,
							VariantMode = false,
						}, slot);
					}
					else {
						Logger.Log("Head2Head.Warn", "Could not find a valid savefile slot for fullgame head 2 head match");
						yield break;
					}
				}
				PlayerStatus.Current.MatchStarted();
			}
			def.RegisterSaveFile();
			ActionLogger.StartingMatch(def);
			new FadeWipe(GetLevelForCoroutine(), false, () => {
				LevelEnter.Go(new Session(gak.Local.Value, startRoom), false);
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
				ActionLogger.CompletedMatch();
				if (args.MatchDef.UseFreshSavefile) {
					returnToSlot = PlayerStatus.Current.FileSlotBeforeMatchStart;
				}
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

		internal bool DoAutolaunchImmediate(GlobalAreaKey area, int fileSlot, bool doFadeWipe = true) {
			doAutoLaunch = true;
			autoLaunchArea = area;
			returnToSlot = fileSlot;
			return DoPostPhaseAutoLaunch(doFadeWipe);
		}

		internal bool DoPostPhaseAutoLaunch(bool doFadeWipe, MatchObjectiveType? ifType = null)
		{
			if (!doAutoLaunch) return false;
			if (ifType != null && lastObjectiveType != ifType.Value) return false;
			GlobalAreaKey area = autoLaunchArea;
			int oldSlot = global::Celeste.SaveData.Instance.FileSlot;
			int newSlot = returnToSlot;
			ClearAutoLaunchInfo();
			if (newSlot > -2) {  // This will only be set to a valid slot when finishing a full-game run
				SaveData saveData = UserIO.Load<SaveData>(global::Celeste.SaveData.GetFilename(newSlot), backup: false);
				if (saveData != null) {
					saveData.AfterInitialize();
					global::Celeste.SaveData.Start(saveData, newSlot);
					global::Celeste.SaveData.TryDelete(oldSlot);
				}
			}
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
			returnToSlot = -2;
		}

		// #######################################################

		/// <summary>
		/// Searches for a save slot with no data (max slot number 99)
		/// </summary>
		/// <returns>index of open slot or -2 if none was found or UserIO could not be opened</returns>
		internal int FindNextUnusedSlot() {
			if (UserIO.Open(UserIO.Mode.Read)) {
				for (int i = 3; i < 100; i++) {
					if (!UserIO.Exists(global::Celeste.SaveData.GetFilename(i))) {
						UserIO.Close();
						return i;
					}
				}
				UserIO.Close();
			}
			return -2;
		}
	}

	public class Head2HeadModuleSaveData : EverestModuleSaveData {

	}

	public class Head2HeadModuleSession : EverestModuleSession {

	}
}
