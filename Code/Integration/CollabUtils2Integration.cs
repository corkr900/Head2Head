using Celeste.Mod.Head2Head.Shared;
using MonoMod.RuntimeDetour;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.Integration {
	public class CollabUtils2Integration {
		internal static bool IsCollabUtils2Installed = false;

		internal static Type LobbyHelper;
		private static MethodInfo LobbyHelper_GetLobbyLevelSet;
		private static MethodInfo LobbyHelper_IsCollabLevelSet;
		private static MethodInfo LobbyHelper_GetLobbyForLevelSet;
		private static MethodInfo LobbyHelper_GetLobbyForGym;
		private static MethodInfo LobbyHelper_GetCollabNameForSID;
		private static MethodInfo LobbyHelper_IsHeartSide;

		internal static Type MiniHeart;
		internal static Type ReturnToLobbyHelper;

		private static IDetour hook_MiniHeart_SmashRoutine;
		private static IDetour hook_ReturnToLobbyHelper_onLevelEnterGo;

		internal static void Load() {
			try {
				// Get type info and functions
				LobbyHelper = Type.GetType("Celeste.Mod.CollabUtils2.LobbyHelper,CollabUtils2");
				LobbyHelper_GetLobbyLevelSet = LobbyHelper.GetMethod("GetLobbyLevelSet", BindingFlags.Public | BindingFlags.Static);
				LobbyHelper_IsCollabLevelSet = LobbyHelper.GetMethod("IsCollabLevelSet", BindingFlags.Public | BindingFlags.Static);
				LobbyHelper_GetLobbyForLevelSet = LobbyHelper.GetMethod("GetLobbyForLevelSet", BindingFlags.Public | BindingFlags.Static);
				LobbyHelper_GetLobbyForGym = LobbyHelper.GetMethod("GetLobbyForGym", BindingFlags.Public | BindingFlags.Static);
				LobbyHelper_GetCollabNameForSID = LobbyHelper.GetMethod("GetCollabNameForSID", BindingFlags.Public | BindingFlags.Static);
				LobbyHelper_IsHeartSide = LobbyHelper.GetMethod("IsHeartSide", BindingFlags.Public | BindingFlags.Static);

				MiniHeart = Type.GetType("Celeste.Mod.CollabUtils2.Entities.MiniHeart,CollabUtils2");
				ReturnToLobbyHelper = Type.GetType("Celeste.Mod.CollabUtils2.UI.ReturnToLobbyHelper,CollabUtils2");

				// Set up hooks
				hook_MiniHeart_SmashRoutine = new Hook(
					MiniHeart.GetMethod("SmashRoutine", BindingFlags.NonPublic | BindingFlags.Instance),
					typeof(CollabUtils2Integration).GetMethod("OnMiniHeartCollect"));

				hook_ReturnToLobbyHelper_onLevelEnterGo = new Hook(
					ReturnToLobbyHelper.GetMethod("onLevelEnterGo", BindingFlags.NonPublic | BindingFlags.Static),
					typeof(CollabUtils2Integration).GetMethod("OnCollabOnLevelEnterGo"));

				// Misc
				IsCollabUtils2Installed = true;
			}
			catch(Exception e) {
				IsCollabUtils2Installed = false;
			}
		}

		#region Mod Interface

		/// <summary>
		/// Returns the level set the given lobby SID is associated to, or null if the SID given is not a lobby.
		/// </summary>
		/// <param name="sid">The SID for a map</param>
		/// <returns>The level set name for this lobby, or null if the SID given is not a lobby</returns>
		internal static string GetLobbyLevelSet(string sid) {
			if (!IsCollabUtils2Installed) return null;
			return LobbyHelper_GetLobbyLevelSet?.Invoke(null, new object[] { sid }) as string;
		}

		/// <summary>
		/// Returns true if the given level set is a hidden level set from a collab.
		/// </summary>
		internal static bool IsCollabLevelSet(string levelSet) {
			if (!IsCollabUtils2Installed) return false;
			return (bool)LobbyHelper_IsCollabLevelSet?.Invoke(null, new object[] { levelSet });
		}

		/// <summary>
		/// Returns the SID of the lobby corresponding to this level set.
		/// </summary>
		/// <param name="levelSet">The level set name</param>
		/// <returns>The SID of the lobby for this level set, or null if the given level set does not belong to a collab or has no matching lobby.</returns>
		internal static string GetLobbyForLevelSet(string levelSet) {
			if (!IsCollabUtils2Installed) return null;
			return (string)LobbyHelper_GetLobbyForLevelSet?.Invoke(null, new object[] { levelSet });
		}

		/// <summary>
		/// Returns the SID of the lobby corresponding to this gym.
		/// </summary>
		/// <param name="gymSID">The gym SID</param>
		/// <returns>The SID of the lobby for this gym, or null if the given SID does not belong to a collab or has no matching lobby.</returns>
		internal static string GetLobbyForGym(string gymSID) {
			if (!IsCollabUtils2Installed) return null;
			return (string)LobbyHelper_GetLobbyForGym?.Invoke(null, new object[] { gymSID });
		}

		/// <summary>
		/// Returns the name of the collab the level with the given SID is part of.
		/// </summary>
		/// <param name="sid">A map SID</param>
		/// <returns>The name of the collab the map is part of, or null if it is a non-collab map</returns>
		internal static string GetCollabNameForSID(string sid) {
			if (!IsCollabUtils2Installed) return null;
			return (string)LobbyHelper_GetCollabNameForSID?.Invoke(null, new object[] { sid });
		}

		/// <summary>
		/// Check if the given SID matches a collab heart side level.
		/// </summary>
		/// <param name="sid">The SID for a map</param>
		/// <returns>true if this is a collab heart side, false otherwise.</returns>
		internal static bool IsHeartSide(string sid) {
			if (!IsCollabUtils2Installed) return false;
			return (bool)LobbyHelper_IsHeartSide?.Invoke(null, new object[] { sid });
		}

		#endregion

		#region Hook code

		public static IEnumerator OnMiniHeartCollect(Func<object, Player, Level, IEnumerator> orig, object miniHeart, Player player, Level level) {
			yield return new SwapImmediately(orig(miniHeart, player, level));
			Head2HeadModule.Instance.DoPostPhaseAutoLaunch(true);
		}

		// I hate this
		public static void OnCollabOnLevelEnterGo(Action<On.Celeste.LevelEnter.orig_Go, Session, bool> orig,
			On.Celeste.LevelEnter.orig_Go orig_orig, Session session, bool fromSaveData) {
			if (PlayerStatus.Current.IsInMatch(false)) {
				// Circumnavigate session loading when we're in a match
				orig_orig(session, fromSaveData);
			}
			else {
				orig(orig_orig, session, fromSaveData);
			}
		}

		#endregion
	}
}
