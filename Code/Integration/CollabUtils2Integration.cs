using Celeste.Mod.Head2Head.Shared;
using MonoMod.ModInterop;
using MonoMod.RuntimeDetour;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.Integration {
	[ModImportName("CollabUtils2.LobbyHelper")]
	public class CollabUtils2Integration {
		internal static bool IsCollabUtils2Installed = false;

		internal static Type MiniHeart;
		internal static Type ReturnToLobbyHelper;

		private static Hook hook_MiniHeart_SmashRoutine;
		private static Hook hook_ReturnToLobbyHelper_onLevelEnterGo;

		internal static void Load() {
			try {
				typeof(CollabUtils2Integration).ModInterop();

				MiniHeart = Type.GetType("Celeste.Mod.CollabUtils2.Entities.MiniHeart,CollabUtils2");
				ReturnToLobbyHelper = Type.GetType("Celeste.Mod.CollabUtils2.UI.ReturnToLobbyHelper,CollabUtils2");

				// Set up hooks
				if (MiniHeart != null) {
					hook_MiniHeart_SmashRoutine = new Hook(
						MiniHeart.GetMethod("SmashRoutine", BindingFlags.NonPublic | BindingFlags.Instance),
						typeof(CollabUtils2Integration).GetMethod(nameof(OnMiniHeartCollect)));
				}
				if (ReturnToLobbyHelper != null) {
					hook_ReturnToLobbyHelper_onLevelEnterGo = new Hook(
						ReturnToLobbyHelper.GetMethod("onLevelEnterGo", BindingFlags.NonPublic | BindingFlags.Static),
						typeof(CollabUtils2Integration).GetMethod(nameof(OnCollabOnLevelEnterGo)));
				}

				// Misc
				IsCollabUtils2Installed = MiniHeart != null || ReturnToLobbyHelper != null;
			}
			catch(Exception) {
				IsCollabUtils2Installed = false;
			}
		}

		internal static void Unload() {
			hook_MiniHeart_SmashRoutine?.Dispose();
			hook_MiniHeart_SmashRoutine = null;
			hook_ReturnToLobbyHelper_onLevelEnterGo?.Dispose();
			hook_ReturnToLobbyHelper_onLevelEnterGo = null;
		}

		#region Mod Interface

		/// <summary>
		/// Returns true if the given level set is a hidden level set from a collab.
		/// </summary>
		public static Func<string, bool> IsCollabLevelSet;

		/// <summary>
		/// Check if the SID is a collab map
		/// </summary>
		public static Func<string, bool> IsCollabMap;

		/// <summary>
		/// Check if the SID is a collab lobby
		/// </summary>
		public static Func<string, bool> IsCollabLobby;

		/// <summary>
		/// Check if the SID is a collab gym
		/// </summary>
		public static Func<string, bool> IsCollabGym;

		/// <summary>
		/// Check if the given SID matches a collab heart side level.
		/// </summary>
		/// <param name="sid">The SID for a map</param>
		/// <returns>true if this is a collab heart side, false otherwise.</returns>
		public static Func<string, bool> IsHeartSide;

		/// <summary>
		/// Returns the SID of the lobby associated with the map
		/// </summary>
		public static Func<string, string> GetLobbyForMap;

		/// <summary>
		/// Returns the level set the given lobby SID is associated to, or null if the SID given is not a lobby.
		/// </summary>
		/// <param name="sid">The SID for a map</param>
		/// <returns>The level set name for this lobby, or null if the SID given is not a lobby</returns>
		public static Func<string, string> GetLobbyLevelSet;

		/// <summary>
		/// Returns the SID of the lobby corresponding to this level set.
		/// </summary>
		/// <param name="levelSet">The level set name</param>
		/// <returns>The SID of the lobby for this level set, or null if the given level set does not belong to a collab or has no matching lobby.</returns>
		public static Func<string, string> GetLobbyForLevelSet;

		/// <summary>
		/// Returns the SID of the lobby corresponding to this gym.
		/// </summary>
		/// <param name="gymSID">The gym SID</param>
		/// <returns>The SID of the lobby for this gym, or null if the given SID does not belong to a collab or has no matching lobby.</returns>
		public static Func<string, string> GetLobbyForGym;

		/// <summary>
		/// Returns the name of the collab the level with the given SID is part of.
		/// </summary>
		/// <param name="sid">A map SID</param>
		/// <returns>The name of the collab the map is part of, or null if it is a non-collab map</returns>
		public static Func<string, string> GetCollabNameForSID;

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
