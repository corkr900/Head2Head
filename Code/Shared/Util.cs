using Celeste.Mod.Helpers;
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

		internal static string DLL { get { return CleanDLL(Head2HeadModule.Instance.Metadata); } }

		/// <summary>
		/// Counts the number of berries in a chapter. Returns -1 if the area could not be localized or data could not be found.
		/// </summary>
		/// <param name="area"></param>
		/// <returns></returns>
		public static int CountBerries(GlobalAreaKey area) {
			if (!area.ExistsLocal) return -1;
			return area.Data.Mode[(int)area.Mode].TotalStrawberries;
		}

		internal static int CountMoonBerries(GlobalAreaKey area) {
			if (!area.ExistsLocal) return -1;
			var strawbs = area.Data.Mode[(int)area.Mode].MapData.Strawberries;
			int count = 0;
			foreach (var strawb in strawbs) {
				if (strawb.Values == null || !strawb.Values.ContainsKey("moon")) continue;
				object val = strawb.Values["moon"];
				if (val is bool && (bool)val) count += 1;
			}
			return count;
		}

		internal static bool HasCassette(GlobalAreaKey area) {
			if (!area.ExistsLocal) return false;
			if (area.Data.Mode[(int)area.Mode].MapData.DetectedCassette) return true;
			MapData md = GetMapDataForMode(area);
			if (md == null) return false;
			DynamicData dd = new DynamicData(md);
			if (!dd.Data.ContainsKey("HasCassette")) return false;
			return dd.Get<bool>("HasCassette");
		}

		internal static bool HasOptionalRealHeart(GlobalAreaKey area) {
			if (!area.ExistsLocal) return false;
			MapMetaModeProperties props = area.ModeMetaProperties;
			if (props?.HeartIsEnd == true) return false;
			DynamicData dd = new DynamicData(GetMapDataForMode(area));
			if (!dd.Data.ContainsKey("DetectedRealHeartGem")) return false;
			return dd.Get<bool>("DetectedRealHeartGem");
		}

		internal static MapData GetMapDataForMode(GlobalAreaKey area) {
			if (!area.ExistsLocal) return null;
			return area.Data.Mode[(int)area.Mode].MapData;
		}

		internal static bool HasTrackedBerries(GlobalAreaKey area) {
			if (!area.ExistsLocal) return false;
			return area.Data.Mode[(int)area.Mode].MapData.DetectedStrawberries > 0;
		}

		internal static LevelSetStats GetSetStats(string levelSet) {
			if (string.IsNullOrEmpty(levelSet)) return null;
			return SaveData.Instance.GetLevelSetStatsFor(levelSet);
		}

		internal static ModContent GetModContent(GlobalAreaKey area) {
			string path = string.Format("Maps/{0}", area.Data.Mode[(int)area.Mode].Path);
			foreach (ModContent content in Everest.Content.Mods) {
				if (content.Map.ContainsKey(path)) return content;
			}
			return null;
		}

		public static string TranslatedCategoryName(StandardCategory cat) {
			return Dialog.Get(string.Format("Head2Head_CategoryName_{0}", cat.ToString()));
		}

		public static string TranslatedMatchResult(ResultCategory cat) {
			return Dialog.Get(string.Format("Head2Head_MatchResultCat_{0}", cat.ToString()));
		}

		public static string TranslatedMatchState(MatchState cat)
		{
			return Dialog.Get(string.Format("Head2Head_MatchState_{0}", cat.ToString()));
		}

		internal static bool EntityIsRealHeartGem(BinaryPacker.Element entity) {
			if (entity.Name == "blackGem") {
				bool isFake = entity.AttrBool("fake", false);
				if (!isFake) return true;
			}
			if (entity.Name == "birdForsakenCityGem") return true;
			if (entity.Name == "reflectionHeartStatue") return true;

			// TODO Bug mod owners to update their entities to use the API to register their heart types

			// Known custom heart types:
			//	AdventureHelper Custom Crystal Heart
			//	Arphimigon's D-Sides Heart?
			//	Arphimigon's D-Sides Recolourable Heart?
			//	communalhelper Crystal Heart
			//	communalhelper Custom Crystal Heart
			//	FactoryHelper machine heart?
			//	Frozen Waterfall Boss Heart?
			//	max480hand Reskinnable Crystal Heart
			//	Memorial Helper Flag Crystal Heart
			//	P sides heart gem??
			//	Vivhelper Dash Code Heart Controller
			if (CustomCollectables.CustomHeartTypes.ContainsKey(entity.Name)) {
				CustomCollectableInfo info = CustomCollectables.CustomHeartTypes[entity.Name];
				return info.Condition?.Invoke(entity) ?? true;
			}

			return false;
		}

		internal static bool EntityIsCassette(BinaryPacker.Element entity) {
			if (entity.Name == "cassette") return true;
			if (CustomCollectables.CustomCassetteTypes.ContainsKey(entity.Name)) {
				CustomCollectableInfo info = CustomCollectables.CustomCassetteTypes[entity.Name];
				return info.Condition?.Invoke(entity) ?? true;
			}
			return false;
		}

		internal static long TimeValueInternal(int minutes, int seconds, int milliseconds = 0) {
			return new TimeSpan(0, 0, minutes, seconds, milliseconds).Ticks;
		}

		internal static string ReadableTimeSpanTitle(long ticks) {
			TimeSpan timeSpan = TimeSpan.FromTicks(ticks);
			if (timeSpan.TotalHours >= 1.0) {
				return (int)timeSpan.TotalHours + timeSpan.ToString("h\\:mm\\:ss");
			}
			return timeSpan.ToString("m\\:ss");
		}

		internal static double TimeToSeconds(long ticks) {
			TimeSpan timeSpan = TimeSpan.FromTicks(ticks);
			return timeSpan.TotalSeconds;
		}

		internal static string CleanDLL(EverestModuleMetadata meta) {
			string ret;
			if (string.IsNullOrEmpty(meta.DLL)) ret = meta.DLL;
			else if (string.IsNullOrEmpty(meta.PathDirectory)) ret = meta.DLL;
			else if (meta.PathDirectory.Length + 1 >= meta.DLL.Length) ret = meta.DLL;  // Probably impossible. But probably is not a promise.
			else ret = meta.DLL.Substring(meta.PathDirectory.Length + 1);
			return ret?.Replace('\\', '/');
		}

		internal static bool IsUpdateAvailable() {
			SortedDictionary<ModUpdateInfo, EverestModuleMetadata> updates = ModUpdaterHelper.GetAsyncLoadedModUpdates();
			foreach(EverestModuleMetadata meta in updates.Values) {
				if (CleanDLL(meta) == DLL) {
					return true;
				}
			}
			return false;
		}

		public static string CategoryToIcon(StandardCategory cat) {
			return string.Format("Head2Head/Categories/{0}", cat.ToString());
		}
	}
}
