using Celeste.Mod.Meta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.Shared {

	public static class VanillaSID {
		public const string Prologue = "Celeste/0-Intro";
		public const string City = "Celeste/1-ForsakenCity";
		public const string OldSite = "Celeste/2-OldSite";
		public const string Resort = "Celeste/3-CelestialResort";
		public const string Ridge = "Celeste/4-GoldenRidge";
		public const string Temple = "Celeste/5-MirrorTemple";
		public const string Reflection = "Celeste/6-Reflection";
		public const string Summit = "Celeste/7-Summit";
		public const string Epilogue = "Celeste/8-Epilogue";
		public const string Core = "Celeste/9-Core";
		public const string Farewell = "Celeste/LostLevels";
	}

	public enum MatchPhaseType {
		Clear,
		HeartCassette,
		AllRedBerries,
		FullClear,
		CassetteGrab,
	}

	public static class Util {

		/// <summary>
		/// Populates a global area key from an SID. Returns true if the SID was successfully localized, otherwise false.
		/// </summary>
		/// <param name="SID"></param>
		/// <param name="mode"></param>
		/// <param name="gak"></param>
		/// <returns>Returns true if the SID was successfully localized, otherwise false.</returns>
		public static bool TryGetAreaKey(string SID, AreaMode mode, out GlobalAreaKey gak) {
			gak = new GlobalAreaKey(SID, mode);
			return gak.Local != null;
		}

		/// <summary>
		/// Counts the number of berries in a chapter. Returns -1 if the area could not be localized or data could not be found.
		/// </summary>
		/// <param name="area"></param>
		/// <returns></returns>
		public static int CountBerries(GlobalAreaKey area) {
			if (!area.ExistsLocal) return -1;
			return area.Data.Mode[(int)area.Mode].TotalStrawberries;
		}

		internal static bool HasCassette(GlobalAreaKey area) {
			if (!area.ExistsLocal) return false;
			return area.Data.Mode[(int)area.Mode].MapData.DetectedCassette;
		}

		internal static bool HasOptionalHeart(GlobalAreaKey area) {
			if (!area.ExistsLocal) return false;
			MapMetaModeProperties props = area.ModeMetaProperties;
			if (props.HeartIsEnd == true) return false;
			return area.Data.Mode[(int)area.Mode].MapData.DetectedHeartGem;
		}

		internal static bool HasTrackedBerries(GlobalAreaKey area) {
			if (!area.ExistsLocal) return false;
			return area.Data.Mode[(int)area.Mode].MapData.DetectedStrawberries > 0;
		}

		internal static LevelSetStats GetSetStats(string levelSet) {
			if (string.IsNullOrEmpty(levelSet)) return null;
			return SaveData.Instance.GetLevelSetStatsFor(levelSet);
		}

		public static string TranslatedCategoryName(StandardCategory cat) {
			return Dialog.Get(string.Format("Head2Head_CategoryName_{0}", cat.ToString()));
		}
	}
}
