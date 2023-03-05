using Celeste.Mod.Head2Head.Shared;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.Integration {
	public static class SpeedrunToolIntegration {

		private static bool IsSpeedrunToolInstalled = false;

		private static Type StateManager;
		private static Type TeleportRoomUtils;
		private static MethodInfo StateManager_SaveState;
		private static MethodInfo StateManager_LoadState;
		private static MethodInfo TeleportRoomUtils_TeleportTo;

		private static IDetour Hook_StateManager_SaveState;
		private static IDetour Hook_StateManager_LoadState;
		private static IDetour Hook_TeleportRoomUtils_TeleportTo;

		internal static void Load() {
			try {
				// Get type info and functions
				StateManager = Type.GetType("Celeste.Mod.SpeedrunTool.SaveLoad.StateManager,SpeedrunTool");
				if (StateManager == null) {
					IsSpeedrunToolInstalled = false;
					return;
				}
				StateManager_SaveState = StateManager.GetMethod(
					"SaveState", BindingFlags.NonPublic | BindingFlags.Instance,
					Type.DefaultBinder, new Type[] { typeof(bool) }, null);
				StateManager_LoadState = StateManager.GetMethod(
					"LoadState", BindingFlags.NonPublic | BindingFlags.Instance,
					Type.DefaultBinder, new Type[] { typeof(bool) }, null);

				TeleportRoomUtils = Type.GetType("Celeste.Mod.SpeedrunTool.TeleportRoom.TeleportRoomUtils,SpeedrunTool");
				TeleportRoomUtils_TeleportTo = TeleportRoomUtils.GetMethod("TeleportTo", BindingFlags.NonPublic | BindingFlags.Static);

				// Set up hooks
				Hook_StateManager_SaveState = new Hook(StateManager_SaveState,
					typeof(SpeedrunToolIntegration).GetMethod("OnSaveState", BindingFlags.NonPublic | BindingFlags.Static));
				Hook_StateManager_LoadState = new Hook(StateManager_LoadState,
					typeof(SpeedrunToolIntegration).GetMethod("OnLoadState", BindingFlags.NonPublic | BindingFlags.Static));
				Hook_TeleportRoomUtils_TeleportTo = new Hook(TeleportRoomUtils_TeleportTo,
					typeof(SpeedrunToolIntegration).GetMethod("OnTeleportTo", BindingFlags.NonPublic | BindingFlags.Static));

				// Misc
				IsSpeedrunToolInstalled = true;
			}
			catch (Exception e) {
				IsSpeedrunToolInstalled = false;
			}
		}

		internal static void Unload() {
			Hook_StateManager_SaveState?.Dispose();
			Hook_StateManager_SaveState = null;
			Hook_StateManager_LoadState?.Dispose();
			Hook_StateManager_LoadState = null;
			Hook_TeleportRoomUtils_TeleportTo?.Dispose();
			Hook_TeleportRoomUtils_TeleportTo = null;
		}

#pragma warning disable IDE0051  // Private method is unused

		private static bool OnSaveState(Func<object, bool, bool> orig, object stateManager, bool tas) {
			bool result = orig(stateManager, tas);
			if (result && PlayerStatus.Current.IsInMatch(true)) {
				PlayerStatus.Current.CurrentMatch?.PlayerDNF(DNFReason.Savestate);
			}
			return result;
		}

		private static bool OnLoadState(Func<object, bool, bool> orig, object stateManager, bool tas) {
			bool result = orig(stateManager, tas);
			if (result && PlayerStatus.Current.IsInMatch(true)) {
				PlayerStatus.Current.CurrentMatch?.PlayerDNF(DNFReason.Savestate);
			}
			return result;
		}

		private static void OnTeleportTo(Action<Session, bool> orig, Session session, bool fromHistory) {
			if (PlayerStatus.Current.IsInMatch(true)) {
				PlayerStatus.Current.CurrentMatch?.PlayerDNF(DNFReason.SpeedrunTools);
			}
			orig(session, fromHistory);
		}

#pragma warning restore IDE0051  // Private method is unused

	}
}
