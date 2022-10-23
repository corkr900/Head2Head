using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.Shared {
	public static class CustomCollectables {
		internal static Dictionary<string, CustomCollectableInfo> CustomHeartTypes = new Dictionary<string, CustomCollectableInfo>();
		internal static Dictionary<string, CustomCollectableInfo> CustomCassetteTypes = new Dictionary<string, CustomCollectableInfo>();
		internal static Dictionary<string, CustomCollectableInfo> CustomOtherCollectableTypes = new Dictionary<string, CustomCollectableInfo>();

		public static void AddHeart(string entityTypeID, Func<BinaryPacker.Element, bool> condition) {
			CustomHeartTypes.Add(entityTypeID, new CustomCollectableInfo() {
				Name = entityTypeID,
				Condition = condition,
				DisplayName = "Crystal Heart", // Can't use Dialog.Clean - languages not initialized yet
			});
		}

		public static void AddCassette(string entityTypeID, Func<BinaryPacker.Element, bool> condition) {
			CustomCassetteTypes.Add(entityTypeID, new CustomCollectableInfo() {
				Name = entityTypeID,
				Condition = condition,
				DisplayName = "Cassette", // Can't use Dialog.Clean - languages not initialized yet
			});
		}

		public static string GetDisplayName(string key) {
			if (CustomOtherCollectableTypes.ContainsKey(key)) {
				string nameOverride = CustomOtherCollectableTypes[key].DisplayName;
				return string.IsNullOrEmpty(nameOverride) ? Dialog.Get("Head2Head_CollectableNames_UnnamedCustom") : nameOverride;
			}
			if (CustomHeartTypes.ContainsKey(key)) {
				string nameOverride = CustomHeartTypes[key].DisplayName;
				return string.IsNullOrEmpty(nameOverride) ? Dialog.Get("Head2Head_CollectableNames_Heart") : nameOverride;
			}
			if (CustomCassetteTypes.ContainsKey(key)) {
				string nameOverride = CustomCassetteTypes[key].DisplayName;
				return string.IsNullOrEmpty(nameOverride) ? Dialog.Get("Head2Head_CollectableNames_Cassette") : nameOverride;
			}
			return Dialog.Get("Head2Head_CollectableNames_UnnamedCustom");
		}
	}

	internal class CustomCollectableInfo {
		public string Name;
		public Func<BinaryPacker.Element, bool> Condition;
		public string DisplayName;
	}
}
