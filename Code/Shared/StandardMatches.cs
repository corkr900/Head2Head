using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.Shared {
	public enum StandardCategory {
		Clear,
		HeartCassette,
		ARB,
		ARBHeart,
		CassetteGrab,
		FullClear,
		MoonBerry,
		FullClearMoonBerry,
	}

	public static class StandardMatches {
		public static MatchPhase ILClear(GlobalAreaKey area) {
			if (string.IsNullOrEmpty(area.Local?.SID)) return null;
			if (!IsCategoryValid(StandardCategory.Clear, area)) return null;

			MatchPhase mp = new MatchPhase() {
				category = StandardCategory.Clear,
				Area = area,
				Objectives = new List<MatchObjective>() {
					new MatchObjective() {
						ObjectiveType = MatchObjectiveType.ChapterComplete,
					}
				}
			};
			return mp;
		}

		public static MatchPhase ILHeartCassette(GlobalAreaKey area) {
			if (string.IsNullOrEmpty(area.Local?.SID)) return null;
			if (!IsCategoryValid(StandardCategory.HeartCassette, area)) return null;

			MatchPhase mp = new MatchPhase() {
				category = StandardCategory.HeartCassette,
				Area = area,
				Objectives = new List<MatchObjective>() {
					new MatchObjective() {
						ObjectiveType = MatchObjectiveType.HeartCollect,
					},
					new MatchObjective() {
						ObjectiveType = MatchObjectiveType.CassetteCollect,
					},
					new MatchObjective() {
						ObjectiveType = MatchObjectiveType.ChapterComplete,
					}
				}
			};
			return mp;
		}

		public static MatchPhase ILAllRedBerries(GlobalAreaKey area) {
			if (string.IsNullOrEmpty(area.Local?.SID)) return null;
			if (!IsCategoryValid(StandardCategory.ARB, area)) return null;

			MatchPhase mp = new MatchPhase() {
				category = StandardCategory.ARB,
				Area = area,
				Objectives = new List<MatchObjective>() {
					new MatchObjective() {
						ObjectiveType = MatchObjectiveType.Strawberries,
						BerryGoal = Util.CountBerries(area),
					},
					new MatchObjective() {
						ObjectiveType = MatchObjectiveType.ChapterComplete,
					},
				}
			};
			return mp;
		}

		public static MatchPhase ILAllRedBerriesHeart(GlobalAreaKey area) {
			if (string.IsNullOrEmpty(area.Local?.SID)) return null;
			if (!IsCategoryValid(StandardCategory.ARBHeart, area)) return null;

			MatchPhase mp = new MatchPhase() {
				category = StandardCategory.ARBHeart,
				Area = area,
				Objectives = new List<MatchObjective>() {
					new MatchObjective() {
						ObjectiveType = MatchObjectiveType.Strawberries,
						BerryGoal = Util.CountBerries(area),
					},
					new MatchObjective() {
						ObjectiveType = MatchObjectiveType.HeartCollect,
					},
					new MatchObjective() {
						ObjectiveType = MatchObjectiveType.ChapterComplete,
					},
				}
			};
			return mp;
		}

		public static MatchPhase ILCassetteGrab(GlobalAreaKey area) {
			if (string.IsNullOrEmpty(area.Local?.SID)) return null;
			if (!IsCategoryValid(StandardCategory.CassetteGrab, area)) return null;

			MatchPhase mp = new MatchPhase() {
				category = StandardCategory.CassetteGrab,
				Area = area,
				Objectives = new List<MatchObjective>() {
					new MatchObjective() {
						ObjectiveType = MatchObjectiveType.CassetteCollect,
					},
				}
			};
			return mp;
		}

		public static MatchPhase ILFullClear(GlobalAreaKey area) {
			if (string.IsNullOrEmpty(area.Local?.SID)) return null;
			if (!IsCategoryValid(StandardCategory.FullClear, area)) return null;

			MatchPhase mp = new MatchPhase() {
				category = StandardCategory.FullClear,
				Area = area,
				Objectives = new List<MatchObjective>() {
					new MatchObjective() {
						ObjectiveType = MatchObjectiveType.ChapterComplete,
					},
				}
			};
			if (Util.HasOptionalRealHeart(area)) {
				mp.Objectives.Add(new MatchObjective() {
					ObjectiveType = MatchObjectiveType.HeartCollect,
				});
			}
			if (Util.HasCassette(area)) {
				mp.Objectives.Add(new MatchObjective() {
					ObjectiveType = MatchObjectiveType.CassetteCollect,
				});
			}
			if (Util.HasTrackedBerries(area)) {
				mp.Objectives.Add(new MatchObjective() {
					ObjectiveType = MatchObjectiveType.Strawberries,
					BerryGoal = Util.CountBerries(area)
				});
			}
			return mp;
		}

		public static MatchPhase ILMoonBerry(GlobalAreaKey area) {
			if (string.IsNullOrEmpty(area.Local?.SID)) return null;
			if (!IsCategoryValid(StandardCategory.MoonBerry, area)) return null;

			MatchPhase mp = new MatchPhase() {
				category = StandardCategory.MoonBerry,
				Area = area,
				Objectives = new List<MatchObjective>() {
					new MatchObjective() {
						ObjectiveType = MatchObjectiveType.ChapterComplete,
					},
					new MatchObjective() {
						ObjectiveType = MatchObjectiveType.MoonBerry,
						BerryGoal = Util.CountMoonBerries(area),
					},
				}
			};
			return mp;
		}

		public static MatchPhase ILFCMoonBerry(GlobalAreaKey area) {
			if (string.IsNullOrEmpty(area.Local?.SID)) return null;
			if (!IsCategoryValid(StandardCategory.FullClearMoonBerry, area)) return null;

			MatchPhase mp = new MatchPhase() {
				category = StandardCategory.FullClearMoonBerry,
				Area = area,
				Objectives = new List<MatchObjective>() {
					new MatchObjective() {
						ObjectiveType = MatchObjectiveType.ChapterComplete,
					},
				}
			};
			if (Util.HasOptionalRealHeart(area)) {
				mp.Objectives.Add(new MatchObjective() {
					ObjectiveType = MatchObjectiveType.HeartCollect,
				});
			}
			if (Util.HasCassette(area)) {
				mp.Objectives.Add(new MatchObjective() {
					ObjectiveType = MatchObjectiveType.CassetteCollect,
				});
			}
			if (Util.HasTrackedBerries(area)) {
				mp.Objectives.Add(new MatchObjective() {
					ObjectiveType = MatchObjectiveType.Strawberries,
					BerryGoal = Util.CountBerries(area)
				});
			}
			int moonberries = Util.CountMoonBerries(area);
			if (moonberries > 0) {
				mp.Objectives.Add(new MatchObjective() {
					ObjectiveType = MatchObjectiveType.MoonBerry,
					BerryGoal = moonberries,
				});
			}
			return mp;
		}

		public static bool IsCategoryValid(StandardCategory cat, GlobalAreaKey area)
		{
			// TODO the game crashes if there are zero valid categories for a map...
			if (area.IsOverworld) return false;
			if (!area.ExistsLocal) return false;
			if (area.IsVanilla) {
				if (area.Mode != AreaMode.Normal) {
					return cat == StandardCategory.Clear;
				}
				switch (area.Local?.ID) {
					case null:
					default:
						return false;
					case 1:
					case 2:
					case 3:
					case 4:
					case 5:
					case 7:
						return cat == StandardCategory.Clear
							|| cat == StandardCategory.ARB
							|| cat == StandardCategory.ARBHeart
							|| cat == StandardCategory.HeartCassette
							|| cat == StandardCategory.FullClear
							|| cat == StandardCategory.CassetteGrab;
					case 0:  // Prologue
						return cat == StandardCategory.Clear;
					case 6:  // Reflection
						return cat == StandardCategory.Clear
							|| cat == StandardCategory.FullClear
							|| cat == StandardCategory.CassetteGrab;
					case 8:  // epilogue
						return cat == StandardCategory.Clear;
					case 9:  // Core
						return cat == StandardCategory.Clear
							|| cat == StandardCategory.FullClear
							|| cat == StandardCategory.CassetteGrab;
					case 10:  // Farewell
						return cat == StandardCategory.Clear
							|| cat == StandardCategory.MoonBerry;
				}
			}
			else {
				bool berries = Util.HasTrackedBerries(area);
				bool hasCassette = Util.HasCassette(area);
				bool hasOptionalHeart = Util.HasOptionalRealHeart(area);
				bool canFC = area.Data?.CanFullClear ?? false;
				bool hasMoonBerry = Util.CountMoonBerries(area) > 0;
				switch (cat) {
					case StandardCategory.Clear:
					default:
						return true;
					case StandardCategory.ARB:
						return berries && (hasCassette || hasOptionalHeart || !canFC);
					case StandardCategory.ARBHeart:
					case StandardCategory.HeartCassette:
						return berries && hasCassette && (hasOptionalHeart || !canFC);
					case StandardCategory.CassetteGrab:
						return hasCassette;
					case StandardCategory.FullClear:
						return canFC && (berries || hasCassette || hasOptionalHeart);
					case StandardCategory.MoonBerry:
						return hasMoonBerry;
					case StandardCategory.FullClearMoonBerry:
						return hasMoonBerry && (berries || hasCassette || hasOptionalHeart);
				}
			}
		}
	}
}
