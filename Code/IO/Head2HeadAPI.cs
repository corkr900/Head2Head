using Celeste.Mod.Head2Head.Shared;
using MonoMod.ModInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.IO {
	[ModExportName("Head2Head.API")]
	public static class Head2HeadAPI {

		public static bool HasMatch()
			=> !string.IsNullOrEmpty(PlayerStatus.Current.CurrentMatchID);

		public static bool CurrentlyInMatch(bool includeUnstarted)
			=> PlayerStatus.Current.IsInMatch(includeUnstarted);

		public static void RegisterHeartType(string entityTypeID)
			=> CustomCollectables.AddHeart(entityTypeID, null);

		public static void RegisterConditionalHeartType(string entityTypeID, Func<BinaryPacker.Element, bool> condition)
			=> CustomCollectables.AddHeart(entityTypeID, condition);

		public static void RegisterCassetteType(string entityTypeID)
			=> CustomCollectables.AddCassette(entityTypeID, null);

		public static void RegisterConditionalCassette(string entityTypeID, Func<BinaryPacker.Element, bool> condition)
			=> CustomCollectables.AddCassette(entityTypeID, condition);

		public static void RegisterGenericCollectableType(string entityTypeID, string displayName)
			=> CustomCollectables.AddOtherCollectable(entityTypeID, displayName, null);

		public static void RegisterConditionalGenericCollectableType(string entityTypeID, string displayName, Func<BinaryPacker.Element, bool> condition)
			=> CustomCollectables.AddOtherCollectable(entityTypeID, displayName, condition);

		// TODO (!!!) Add API to remove standard categories from modded maps
	}
}
