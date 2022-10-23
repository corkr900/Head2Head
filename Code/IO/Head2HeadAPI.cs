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

		// Custom collectable types

		public static void RegisterHeartType(string entityTypeID)
			=> CustomCollectables.AddHeart(entityTypeID, null);

		public static void RegisterConditionalHeartType(string entityTypeID, Func<BinaryPacker.Element, bool> condition)
			=> CustomCollectables.AddHeart(entityTypeID, condition);

		public static void RegisterCassetteType(string entityTypeID)
			=> CustomCollectables.AddCassette(entityTypeID, null);

		public static void RegisterConditionalCassette(string entityTypeID, Func<BinaryPacker.Element, bool> condition)
			=> CustomCollectables.AddCassette(entityTypeID, condition);

		// Custom events / collectables

		public static void CustomCollectableCollected(string entityTypeID, AreaKey area, EntityID id)
			=> PlayerStatus.Current.CustomCollectableCollected(entityTypeID, new GlobalAreaKey(area), id);

		public static void CustomObjectiveCompleted(string objectiveTypeID, AreaKey area)
			=> PlayerStatus.Current.CustomObjectiveCompleted(objectiveTypeID, new GlobalAreaKey(area));
	}
}
