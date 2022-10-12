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
				Condition = null,
				DisplayName = Dialog.Clean("Head2Head_CrystalHeart"),
			});
		}

		public static void AddCassette(string entityTypeID, Func<BinaryPacker.Element, bool> condition) {
			CustomCassetteTypes.Add(entityTypeID, new CustomCollectableInfo() {
				Name = entityTypeID,
				Condition = condition,
				DisplayName = Dialog.Clean("Head2Head_Cassette"),
			});
		}

		public static void AddOtherCollectable(string entityTypeID, string displayName, Func<BinaryPacker.Element, bool> condition) {
			CustomCassetteTypes.Add(entityTypeID, new CustomCollectableInfo() {
				Name = entityTypeID,
				Condition = condition,
				DisplayName = displayName,
			});
		}
	}

	internal class CustomCollectableInfo {
		public string Name;
		public Func<BinaryPacker.Element, bool> Condition;
		public string DisplayName;
	}
}
