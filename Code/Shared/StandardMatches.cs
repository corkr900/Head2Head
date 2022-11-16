using Celeste.Mod.Head2Head.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.Shared {
	public enum StandardCategory {
		// Standard Categories
		Clear,
		HeartCassette,
		ARB,
		ARBHeart,
		CassetteGrab,
		FullClear,
		MoonBerry,
		FullClearMoonBerry,

		// Specialty Categories
		OneFifthBerries,
		OneThirdBerries,
		TimeLimit,

		// Fullgame
		AnyPercent,  // TODO
		AllRedBerries,  // TODO
		TrueEnding,  // TODO
		AllCassettes,  // TODO
		BnyPercent,  // TODO
		AllHearts,  // TODO
		OneHundredPercent,  // TODO
		AllChapters,  // TODO
		AllASides,  // TODO
		AllBSides,  // TODO
		AllCSides,  // TODO

		// Custom
		Custom,
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
						CollectableGoal = Util.CountBerries(area),
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
						CollectableGoal = Util.CountBerries(area),
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
					CollectableGoal = Util.CountBerries(area)
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
						CollectableGoal = Util.CountMoonBerries(area),
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
					CollectableGoal = Util.CountBerries(area)
				});
			}
			int moonberries = Util.CountMoonBerries(area);
			if (moonberries > 0) {
				mp.Objectives.Add(new MatchObjective() {
					ObjectiveType = MatchObjectiveType.MoonBerry,
					CollectableGoal = moonberries,
				});
			}
			return mp;
		}

		// Specialty Categories

		public static MatchPhase ILOneThirdBerries(GlobalAreaKey area) {
			if (string.IsNullOrEmpty(area.Local?.SID)) return null;
			if (!IsCategoryValid(StandardCategory.OneThirdBerries, area)) return null;

			MatchPhase mp = new MatchPhase() {
				category = StandardCategory.OneThirdBerries,
				Area = area,
				Objectives = new List<MatchObjective>() {
					new MatchObjective() {
						ObjectiveType = MatchObjectiveType.Strawberries,
						CollectableGoal = (int)Math.Ceiling(Util.CountBerries(area) / 3.0),
					},
					new MatchObjective() {
						ObjectiveType = MatchObjectiveType.ChapterComplete,
					},
				}
			};
			return mp;
		}

		public static MatchPhase ILOneFifthBerries(GlobalAreaKey area) {
			if (string.IsNullOrEmpty(area.Local?.SID)) return null;
			if (!IsCategoryValid(StandardCategory.OneFifthBerries, area)) return null;

			MatchPhase mp = new MatchPhase() {
				category = StandardCategory.OneFifthBerries,
				Area = area,
				Objectives = new List<MatchObjective>() {
					new MatchObjective() {
						ObjectiveType = MatchObjectiveType.Strawberries,
						CollectableGoal = (int)Math.Ceiling(Util.CountBerries(area) / 5.0),
					},
					new MatchObjective() {
						ObjectiveType = MatchObjectiveType.ChapterComplete,
					},
				}
			};
			return mp;
		}

		public static MatchPhase ILTimeLimit(GlobalAreaKey area, long timeLimit) {
			if (string.IsNullOrEmpty(area.Local?.SID)) return null;
			if (!IsCategoryValid(StandardCategory.TimeLimit, area)) return null;

			MatchPhase mp = new MatchPhase() {
				category = StandardCategory.TimeLimit,
				Area = area,
				Objectives = new List<MatchObjective>() {
					new MatchObjective() {
						ObjectiveType = MatchObjectiveType.TimeLimit,
						TimeLimit = timeLimit,
					},
				}
			};
			return mp;
		}

		// Fullgame Categories

		public static MatchDefinition FullGameAnyPct() {
			MatchDefinition def = new MatchDefinition() {
				Owner = PlayerID.MyID ?? PlayerID.Default,
				CreationInstant = DateTime.Now,
				UseFreshSavefile = true,
				AllowCheatMode = false,
			};
			def.Phases.Add(new MatchPhase() {
				category = StandardCategory.AnyPercent,
				Area = GlobalAreaKey.VanillaPrologue,
				Fullgame = true,
				Objectives = new List<MatchObjective>() {
					new MatchObjective() {
						ObjectiveType = MatchObjectiveType.Fullgame_UnlockChapter,
						CustomTypeKey = "Celeste/8-Epilogue",
					}
				}
			});
			return def;
		}

		//FullGameAllRedBerries,
		//FullGameTrueEnding,
		//FullGameAllCassettes,
		//FullGameBnyPercent,
		//FullGameAllHearts,
		//FullGameOneHundredPercent,
		//FullGameAllChapters,
		//FullGameAllASides,
		//FullGameAllBSides,
		//FullGameAllCSides,

		// More Stuff

		public static bool IsCategoryValid(StandardCategory cat, GlobalAreaKey area, CustomMatchTemplate template = null)
		{
			if (area.IsOverworld) return false;
			if (!area.ExistsLocal) return false;
			if (ILSelector.IsSuppressed(area, cat)) return false;
			if (area.IsVanilla) {
				if (area.Mode != AreaMode.Normal) {
					return cat == StandardCategory.Clear
						|| cat == StandardCategory.Custom;
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
							|| cat == StandardCategory.OneFifthBerries
							|| cat == StandardCategory.OneThirdBerries
							|| cat == StandardCategory.HeartCassette
							|| cat == StandardCategory.FullClear
							|| cat == StandardCategory.CassetteGrab
							|| cat == StandardCategory.Custom;
					case 0:  // Prologue
						return cat == StandardCategory.Clear;
					case 6:  // Reflection
						return cat == StandardCategory.Clear
							|| cat == StandardCategory.FullClear
							|| cat == StandardCategory.CassetteGrab
							|| cat == StandardCategory.Custom;
					case 8:  // epilogue
						return cat == StandardCategory.Clear;
					case 9:  // Core
						return cat == StandardCategory.Clear
							|| cat == StandardCategory.OneFifthBerries
							|| cat == StandardCategory.OneThirdBerries
							|| cat == StandardCategory.FullClear
							|| cat == StandardCategory.CassetteGrab
							|| cat == StandardCategory.Custom;
					case 10:  // Farewell
						return cat == StandardCategory.Clear
							|| cat == StandardCategory.MoonBerry
							|| cat == StandardCategory.TimeLimit
							|| cat == StandardCategory.Custom;
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
					case StandardCategory.OneThirdBerries:
					case StandardCategory.OneFifthBerries:
						return berries && (hasCassette || hasOptionalHeart || !canFC);
					case StandardCategory.ARBHeart:
						return berries && hasOptionalHeart && (hasCassette || !canFC);
					case StandardCategory.HeartCassette:
						return hasOptionalHeart && hasCassette && (berries || !canFC);
					case StandardCategory.CassetteGrab:
						return hasCassette;
					case StandardCategory.FullClear:
						return canFC && (berries || hasCassette || hasOptionalHeart);
					case StandardCategory.MoonBerry:
						return hasMoonBerry;
					case StandardCategory.FullClearMoonBerry:
						return hasMoonBerry && (berries || hasCassette || hasOptionalHeart);
					case StandardCategory.Custom:
						// TODO check custom categories for validity
						return true;
				}
			}
		}

		public static bool HasAnyValidCategoryAnySide(GlobalAreaKey area) {
			if (!area.ExistsLocal) return false;
			if (HasAnyValidCategory(new GlobalAreaKey(area.Local.Value.ID, AreaMode.Normal))) {
				return true;
			}
			if (area.Data.HasMode(AreaMode.BSide) &&
				HasAnyValidCategory(new GlobalAreaKey(area.Local.Value.ID, AreaMode.BSide))) {
				return true;
			}
			if (area.Data.HasMode(AreaMode.CSide) &&
				HasAnyValidCategory(new GlobalAreaKey(area.Local.Value.ID, AreaMode.CSide))) {
				return true;
			}
			return false;
		}

		public static bool HasAnyValidCategory(GlobalAreaKey area) {
			return area.ExistsLocal && area.Data.HasMode(area.Mode) && GetCategories(area).Count > 0;  // TODO reimplement so its more efficient
		}

		public static List<Tuple<StandardCategory, CustomMatchTemplate>> GetCategories(GlobalAreaKey gArea) {
			List<Tuple<StandardCategory, CustomMatchTemplate>> ret = new List<Tuple<StandardCategory, CustomMatchTemplate>>();
			StandardCategory[] cats = Role.GetValidCategories();
			// Standard Categories
			foreach (StandardCategory cat in cats) {
				if (cat == StandardCategory.Custom) continue;
				if (!IsCategoryValid(cat, gArea, null)) continue;
				// enforce SRC Rules setting
				if (Head2HeadModule.Settings.UseSRCRulesForARB && (cat == StandardCategory.ARB || cat == StandardCategory.ARBHeart)) {
					switch (gArea.Local?.ID) {
						default:  // custom
							if (cat == StandardCategory.ARBHeart) continue;
							else break;  
						case 0:  // Prologue
						case 6:  // Reflection
						case 8:  // Epilogue
						case 10: // Farewell
							// disallow both ARB and ARB+Heart
							continue;
						case 1:  // Forsaken City
						case 3:  // Celestial Resort
						case 4:  // Golden Ridge
							// allow only ARB+Heart
							if (cat == StandardCategory.ARB) continue;
							else break;
						case 2:  // Old Site
						case 5:  // Mirror Temple
						case 7:  // Summit
						case 9:  // Core
							// allow only ARB
							if (cat == StandardCategory.ARBHeart) continue;
							else break;
					}
				}
				ret.Add(new Tuple<StandardCategory, CustomMatchTemplate>(cat, null));
			}
			// Custom Categories
			if (CustomMatchTemplate.templates.ContainsKey(gArea)) {
				foreach (CustomMatchTemplate template in CustomMatchTemplate.templates[gArea]) {
					if (!IsCategoryValid(StandardCategory.Custom, gArea, template)) continue;
					ret.Add(new Tuple<StandardCategory, CustomMatchTemplate>(StandardCategory.Custom, template));
				}
			}
			return ret;
		}

		public static string GetCategoryTitle(StandardCategory cat, CustomMatchTemplate tem) {
			return (cat == StandardCategory.Custom && !string.IsNullOrEmpty(tem.DisplayName)) ?
				Util.TranslatedIfAvailable(tem.DisplayName) :
				Dialog.Get(string.Format("Head2Head_CategoryName_{0}", cat.ToString()));
		}
	}
}
