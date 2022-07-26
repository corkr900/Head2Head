using Celeste.Mod.Meta;
using MonoMod.Utils;
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

		internal static int CountMoonBerries(GlobalAreaKey area) {
			if (!area.ExistsLocal) return -1;
			var strawbs = area.Data.Mode[(int)area.Mode].MapData.Strawberries;
			int count = 0;
			foreach(var strawb in strawbs) {
				if (strawb.Values == null || !strawb.Values.ContainsKey("moon")) continue;
				object val = strawb.Values["moon"];
				if (val is bool && (bool)val) count += 1;
			}
			return count;
		}

		internal static bool HasOptionalRealHeart(GlobalAreaKey area) {
			if (!area.ExistsLocal) return false;
			MapMetaModeProperties props = area.ModeMetaProperties;
			if (props?.HeartIsEnd == true) return false;
			DynamicData dd = new DynamicData(area.Data.Mode[(int)area.Mode].MapData);
			return dd.Get<bool>("DetectedRealHeartGem");
		}

		internal static bool HasTrackedBerries(GlobalAreaKey area) {
			if (!area.ExistsLocal) return false;
			return area.Data.Mode[(int)area.Mode].MapData.DetectedStrawberries > 0;
		}

		internal static LevelSetStats GetSetStats(string levelSet) {
			if (string.IsNullOrEmpty(levelSet)) return null;
			return SaveData.Instance.GetLevelSetStatsFor(levelSet);
		}

		internal static ModContent GetModContent(AreaKey area) {
			foreach (ModContent content in Everest.Content.Mods) {
				if (content.Map.ContainsKey(area.SID)) return content;
			}
			return null;
		}

		public static string TranslatedCategoryName(StandardCategory cat) {
			return Dialog.Get(string.Format("Head2Head_CategoryName_{0}", cat.ToString()));
		}

		public static string TranslatedMatchResult(ResultCategory cat) {
			return Dialog.Get(string.Format("Head2Head_MatchResultCat_{0}", cat.ToString()));
		}

		internal static bool ContainsRealHeartGem(BinaryPacker.Element data) {
			foreach (BinaryPacker.Element child in data.Children) {
				if (child.Name == "entities" && child.Children != null) {
					foreach (BinaryPacker.Element child2 in child.Children) {
						if (child2.Name == "blackGem") {
							bool isFake = child2.AttrBool("fake", false);
							if (!isFake) return true;
						}
						if (child2.Name == "birdForsakenCityGem") return true;
						if (child2.Name == "reflectionHeartStatue") return true;
						// TODO Custom heart code entities are not handled yet
					}
				}
			}
			return false;
		}
	}
}
