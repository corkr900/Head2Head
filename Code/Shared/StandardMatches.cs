using Celeste.Mod.Head2Head.Entities;
using Celeste.Mod.Head2Head.IO;
using Microsoft.Xna.Framework;
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
		/// <summary>
		/// IL all red berries (NOT full game)
		/// </summary>
		ARB,
		ARBHeart,
		CassetteGrab,
		FullClear,
		MoonBerry,
		FullClearMoonBerry,

		// Fullgame
		AnyPercent,
		/// <summary>
		/// Fullgame all red berries (NOT IL)
		/// </summary>
		AllRedBerries,
		TrueEnding,
		AllCassettes,
		BnyPercent,
		AllHearts,
		OneHundredPercent,
		AllChapters,
		AllASides,
		AllBSides,
		AllCSides,

		// Custom
		Custom,
	}

	public static class StandardMatches {

		// TODO (!) cut over full-game categories to use the generic template framework

		public static MatchDefinition GetFullgameCategoryDefinition(StandardCategory cat) {
			switch (cat) {
				default:
					return null;
				case StandardCategory.AnyPercent:
					return FullGameAnyPct();
				case StandardCategory.AllRedBerries:
					return FullGameAllRedBerries();
				case StandardCategory.TrueEnding:
					return FullGameTrueEnding();
				case StandardCategory.AllCassettes:
					return FullGameAllCassettes();
				case StandardCategory.BnyPercent:
					return FullGameBnyPercent();
				case StandardCategory.AllHearts:
					return FullGameAllHearts();
				case StandardCategory.OneHundredPercent:
					return FullGameOneHundredPercent();
				case StandardCategory.AllChapters:
					return FullGameAllChapters();
				case StandardCategory.AllASides:
					return FullGameAllASides();
				case StandardCategory.AllBSides:
					return FullGameAllBSides();
				case StandardCategory.AllCSides:
					return FullGameAllCSides();
			}
		}

		public static MatchDefinition FullGameAnyPct() {
			MatchDefinition def = new MatchDefinition() {
				Owner = PlayerID.MyID ?? PlayerID.Default,
				CreationInstant = SyncedClock.Now,
				ChangeSavefile = true,
				AllowCheatMode = false,
			};
			def.Phases.Add(new MatchPhase() {
				category = StandardCategory.AnyPercent,
				Area = GlobalAreaKey.VanillaPrologue,
				Fullgame = true,
				LevelSet = "Celeste",
				Objectives = new List<MatchObjective>() {
					new MatchObjective() {
						ObjectiveType = MatchObjectiveType.UnlockChapter,
						CustomTypeKey = "Celeste/8-Epilogue",
					}
				}
			});
			return def;
		}
		public static MatchDefinition FullGameAllRedBerries() {
			MatchDefinition def = new MatchDefinition() {
				Owner = PlayerID.MyID ?? PlayerID.Default,
				CreationInstant = SyncedClock.Now,
				ChangeSavefile = true,
				AllowCheatMode = false,
			};
			def.Phases.Add(new MatchPhase() {
				category = StandardCategory.AllRedBerries,
				Area = GlobalAreaKey.VanillaPrologue,
				Fullgame = true,
				LevelSet = "Celeste",
				Objectives = new List<MatchObjective>() {
					new MatchObjective() {
						ObjectiveType = MatchObjectiveType.Strawberries,
						CollectableGoal = 175,
					},
					new MatchObjective() {
						ObjectiveType = MatchObjectiveType.ChapterComplete,
						CustomTypeKey = "Celeste/9-Core",
					},
				}
			});
			return def;
		}
		public static MatchDefinition FullGameTrueEnding() {
			MatchDefinition def = new MatchDefinition() {
				Owner = PlayerID.MyID ?? PlayerID.Default,
				CreationInstant = SyncedClock.Now,
				ChangeSavefile = true,
				AllowCheatMode = false,
			};
			def.Phases.Add(new MatchPhase() {
				category = StandardCategory.TrueEnding,
				Area = GlobalAreaKey.VanillaPrologue,
				Fullgame = true,
				LevelSet = "Celeste",
				Objectives = new List<MatchObjective>() {
					new MatchObjective() {
						ObjectiveType = MatchObjectiveType.ChapterComplete,
						CustomTypeKey = "Celeste/LostLevels",
					},
				}
			});
			return def;
		}
		public static MatchDefinition FullGameAllCassettes() {
			MatchDefinition def = new MatchDefinition() {
				Owner = PlayerID.MyID ?? PlayerID.Default,
				CreationInstant = SyncedClock.Now,
				ChangeSavefile = true,
				AllowCheatMode = false,
			};
			def.Phases.Add(new MatchPhase() {
				category = StandardCategory.AllCassettes,
				Area = GlobalAreaKey.VanillaPrologue,
				Fullgame = true,
				LevelSet = "Celeste",
				Objectives = new List<MatchObjective>() {
					new MatchObjective() {
						ObjectiveType = MatchObjectiveType.CassetteCollect,
						CollectableGoal = 8,
					},
					new MatchObjective() {
						ObjectiveType = MatchObjectiveType.ChapterComplete,
						CustomTypeKey = "Celeste/9-Core",
					},
				}
			});
			return def;
		}
		public static MatchDefinition FullGameBnyPercent() {
			// TODO (!) Bny% CANNOT finish any A sides
			MatchDefinition def = new MatchDefinition() {
				Owner = PlayerID.MyID ?? PlayerID.Default,
				CreationInstant = SyncedClock.Now,
				ChangeSavefile = true,
				AllowCheatMode = false,
			};
			def.Phases.Add(new MatchPhase() {
				category = StandardCategory.BnyPercent,
				Area = GlobalAreaKey.VanillaPrologue,
				Fullgame = true,
				LevelSet = "Celeste",
				Objectives = new List<MatchObjective>() {
					new MatchObjective() {
						ObjectiveType = MatchObjectiveType.UnlockChapter,
						CustomTypeKey = "Celeste/8-Epilogue",
					}
				}
			});
			return def;
		}
		public static MatchDefinition FullGameAllHearts() {
			MatchDefinition def = new MatchDefinition() {
				Owner = PlayerID.MyID ?? PlayerID.Default,
				CreationInstant = SyncedClock.Now,
				ChangeSavefile = true,
				AllowCheatMode = false,
			};
			def.Phases.Add(new MatchPhase() {
				category = StandardCategory.AllHearts,
				Area = GlobalAreaKey.VanillaPrologue,
				Fullgame = true,
				LevelSet = "Celeste",
				Objectives = new List<MatchObjective>() {
					new MatchObjective() {
						ObjectiveType = MatchObjectiveType.HeartCollect,
						CollectableGoal = 24,
					}
				}
			});
			return def;
		}
		public static MatchDefinition FullGameOneHundredPercent() {
			MatchDefinition def = new MatchDefinition() {
				Owner = PlayerID.MyID ?? PlayerID.Default,
				CreationInstant = SyncedClock.Now,
				ChangeSavefile = true,
				AllowCheatMode = false,
			};
			def.Phases.Add(new MatchPhase() {
				category = StandardCategory.OneHundredPercent,
				Area = GlobalAreaKey.VanillaPrologue,
				Fullgame = true,
				LevelSet = "Celeste",
				Objectives = new List<MatchObjective>() {
					new MatchObjective() {
						ObjectiveType = MatchObjectiveType.ChapterComplete,
						CollectableGoal = 27,
					},
					new MatchObjective() {
						ObjectiveType = MatchObjectiveType.Strawberries,
						CollectableGoal = 175,
					},
					new MatchObjective() {
						ObjectiveType = MatchObjectiveType.MoonBerry,
						CollectableGoal = 1,
					},
				}
			});
			return def;
		}
		public static MatchDefinition FullGameAllChapters() {
			MatchDefinition def = new MatchDefinition() {
				Owner = PlayerID.MyID ?? PlayerID.Default,
				CreationInstant = SyncedClock.Now,
				ChangeSavefile = true,
				AllowCheatMode = false,
			};
			def.Phases.Add(new MatchPhase() {
				category = StandardCategory.AllChapters,
				Area = GlobalAreaKey.VanillaPrologue,
				Fullgame = true,
				LevelSet = "Celeste",
				Objectives = new List<MatchObjective>() {
					new MatchObjective() {
						ObjectiveType = MatchObjectiveType.ChapterComplete,
						CollectableGoal = 27,
					},
				}
			});
			return def;
		}
		public static MatchDefinition FullGameAllASides() {
			MatchDefinition def = new MatchDefinition() {
				Owner = PlayerID.MyID ?? PlayerID.Default,
				CreationInstant = SyncedClock.Now,
				ChangeSavefile = true,
				AllowCheatMode = true,
			};
			def.Phases.Add(new MatchPhase() {
				category = StandardCategory.AllASides,
				Area = GlobalAreaKey.VanillaPrologue,
				Fullgame = true,
				LevelSet = "Celeste",
				Objectives = new List<MatchObjective>() {
					new MatchObjective() {
						ObjectiveType = MatchObjectiveType.ChapterComplete,
						CustomTypeKey = "Celeste/1-ForsakenCity",
						Side = AreaMode.Normal,
					},
					new MatchObjective() {
						ObjectiveType = MatchObjectiveType.ChapterComplete,
						CustomTypeKey = "Celeste/2-OldSite",
						Side = AreaMode.Normal,
					},
					new MatchObjective() {
						ObjectiveType = MatchObjectiveType.ChapterComplete,
						CustomTypeKey = "Celeste/3-CelestialResort",
						Side = AreaMode.Normal,
					},
					new MatchObjective() {
						ObjectiveType = MatchObjectiveType.ChapterComplete,
						CustomTypeKey = "Celeste/4-GoldenRidge",
						Side = AreaMode.Normal,
					},
					new MatchObjective() {
						ObjectiveType = MatchObjectiveType.ChapterComplete,
						CustomTypeKey = "Celeste/5-MirrorTemple",
						Side = AreaMode.Normal,
					},
					new MatchObjective() {
						ObjectiveType = MatchObjectiveType.ChapterComplete,
						CustomTypeKey = "Celeste/6-Reflection",
						Side = AreaMode.Normal,
					},
					new MatchObjective() {
						ObjectiveType = MatchObjectiveType.ChapterComplete,
						CustomTypeKey = "Celeste/7-Summit",
						Side = AreaMode.Normal,
					},
					new MatchObjective() {
						ObjectiveType = MatchObjectiveType.ChapterComplete,
						CustomTypeKey = "Celeste/9-Core",
						Side = AreaMode.Normal,
					},
				}
			});
			return def;
		}
		public static MatchDefinition FullGameAllBSides() {
			MatchDefinition def = new MatchDefinition() {
				Owner = PlayerID.MyID ?? PlayerID.Default,
				CreationInstant = SyncedClock.Now,
				ChangeSavefile = true,
				AllowCheatMode = true,
			};
			def.Phases.Add(new MatchPhase() {
				category = StandardCategory.AllBSides,
				Area = GlobalAreaKey.VanillaPrologue,
				Fullgame = true,
				LevelSet = "Celeste",
				Objectives = new List<MatchObjective>() {
					new MatchObjective() {
						ObjectiveType = MatchObjectiveType.ChapterComplete,
						CustomTypeKey = "Celeste/1-ForsakenCity",
						Side = AreaMode.BSide,
					},
					new MatchObjective() {
						ObjectiveType = MatchObjectiveType.ChapterComplete,
						CustomTypeKey = "Celeste/2-OldSite",
						Side = AreaMode.BSide,
					},
					new MatchObjective() {
						ObjectiveType = MatchObjectiveType.ChapterComplete,
						CustomTypeKey = "Celeste/3-CelestialResort",
						Side = AreaMode.BSide,
					},
					new MatchObjective() {
						ObjectiveType = MatchObjectiveType.ChapterComplete,
						CustomTypeKey = "Celeste/4-GoldenRidge",
						Side = AreaMode.BSide,
					},
					new MatchObjective() {
						ObjectiveType = MatchObjectiveType.ChapterComplete,
						CustomTypeKey = "Celeste/5-MirrorTemple",
						Side = AreaMode.BSide,
					},
					new MatchObjective() {
						ObjectiveType = MatchObjectiveType.ChapterComplete,
						CustomTypeKey = "Celeste/6-Reflection",
						Side = AreaMode.BSide,
					},
					new MatchObjective() {
						ObjectiveType = MatchObjectiveType.ChapterComplete,
						CustomTypeKey = "Celeste/7-Summit",
						Side = AreaMode.BSide,
					},
					new MatchObjective() {
						ObjectiveType = MatchObjectiveType.ChapterComplete,
						CustomTypeKey = "Celeste/9-Core",
						Side = AreaMode.BSide,
					},
				}
			});
			return def;
		}
		public static MatchDefinition FullGameAllCSides() {
			MatchDefinition def = new MatchDefinition() {
				Owner = PlayerID.MyID ?? PlayerID.Default,
				CreationInstant = SyncedClock.Now,
				ChangeSavefile = true,
				AllowCheatMode = true,
			};
			def.Phases.Add(new MatchPhase() {
				category = StandardCategory.AllCSides,
				Area = GlobalAreaKey.VanillaPrologue,
				Fullgame = true,
				LevelSet = "Celeste",
				Objectives = new List<MatchObjective>() {
					new MatchObjective() {
						ObjectiveType = MatchObjectiveType.ChapterComplete,
						CustomTypeKey = "Celeste/1-ForsakenCity",
						Side = AreaMode.CSide,
					},
					new MatchObjective() {
						ObjectiveType = MatchObjectiveType.ChapterComplete,
						CustomTypeKey = "Celeste/2-OldSite",
						Side = AreaMode.CSide,
					},
					new MatchObjective() {
						ObjectiveType = MatchObjectiveType.ChapterComplete,
						CustomTypeKey = "Celeste/3-CelestialResort",
						Side = AreaMode.CSide,
					},
					new MatchObjective() {
						ObjectiveType = MatchObjectiveType.ChapterComplete,
						CustomTypeKey = "Celeste/4-GoldenRidge",
						Side = AreaMode.CSide,
					},
					new MatchObjective() {
						ObjectiveType = MatchObjectiveType.ChapterComplete,
						CustomTypeKey = "Celeste/5-MirrorTemple",
						Side = AreaMode.CSide,
					},
					new MatchObjective() {
						ObjectiveType = MatchObjectiveType.ChapterComplete,
						CustomTypeKey = "Celeste/6-Reflection",
						Side = AreaMode.CSide,
					},
					new MatchObjective() {
						ObjectiveType = MatchObjectiveType.ChapterComplete,
						CustomTypeKey = "Celeste/7-Summit",
						Side = AreaMode.CSide,
					},
					new MatchObjective() {
						ObjectiveType = MatchObjectiveType.ChapterComplete,
						CustomTypeKey = "Celeste/9-Core",
						Side = AreaMode.CSide,
					},
				}
			});
			return def;
		}

		// More Stuff

		public static bool IsCategoryValid(StandardCategory cat, GlobalAreaKey area, MatchTemplate template = null, bool defaultOnly = true)
		{
			if (area.IsOverworld) return false;
			if (!area.ExistsLocal) return false;
			if (ILSelector.IsSuppressed(area, cat)) return false;
			if (template?.IncludeInDefaultRuleset == false && defaultOnly) return false;
            if (area.IsVanilla) {
				if (area.Mode != AreaMode.Normal) {
					return cat == StandardCategory.Clear
						|| cat == StandardCategory.Custom;
				}
				switch (area.Local?.ID) {
					case null:
					default:
						return false;
					case 1:  // City
					case 2:  // Site
					case 3:  // Resort
					case 4:  // Ridge
					case 5:  // Temple
					case 7:  // Summit
						return cat == StandardCategory.Clear
							|| cat == StandardCategory.ARB
							|| cat == StandardCategory.ARBHeart
							|| cat == StandardCategory.HeartCassette
							|| cat == StandardCategory.FullClear
							|| cat == StandardCategory.CassetteGrab
							|| cat == StandardCategory.Custom;
					case 6:  // Reflection
						return cat == StandardCategory.Clear
							|| cat == StandardCategory.FullClear
							|| cat == StandardCategory.CassetteGrab
							|| cat == StandardCategory.Custom;
					case 9:  // Core
						return cat == StandardCategory.Clear
							|| cat == StandardCategory.ARB
							|| cat == StandardCategory.CassetteGrab
							|| cat == StandardCategory.Custom;
					case 10:  // Farewell
						return cat == StandardCategory.Clear
							|| cat == StandardCategory.MoonBerry
							|| cat == StandardCategory.Custom;
					case 0:  // Prologue
					case 8:  // Epilogue
						return cat == StandardCategory.Clear;
				}
			}
			else {
				bool berries = Util.HasTrackedBerries(area);
				bool hasCassette = Util.HasCassette(area);
				bool hasOptionalHeart = Util.HasOptionalRealHeart(area);
				bool canFC = area.Data?.CanFullClear ?? false;
				bool hasMoonBerry = Util.CountMoonBerries(area) > 0;
				switch (cat) {
					default:
						return false;
					case StandardCategory.Clear:
						return true;
					case StandardCategory.ARB:
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
						return true;
				}
			}
		}

		public static bool HasAnyValidCategoryAnySide(GlobalAreaKey area) {
			if (!area.ExistsLocal) return false;
			if (HasAnyValidCategory(new GlobalAreaKey(area.Local.Value.ID, AreaMode.Normal), true)) {
				return true;
			}
			if (area.Data.HasMode(AreaMode.BSide) &&
				HasAnyValidCategory(new GlobalAreaKey(area.Local.Value.ID, AreaMode.BSide), true)) {
				return true;
			}
			if (area.Data.HasMode(AreaMode.CSide) &&
				HasAnyValidCategory(new GlobalAreaKey(area.Local.Value.ID, AreaMode.CSide), true)) {
				return true;
			}
			return false;
		}

		public static bool HasAnyValidCategory(GlobalAreaKey area, bool defaultOnly) {
			return area.ExistsLocal && area.Data.HasMode(area.Mode) && GetCategories(area, defaultOnly).Count > 0;  // TODO (!) reimplement so its more efficient
		}

		private static bool ShowCategory(int? id, AreaMode areaMode, StandardCategory cat) {
			bool? roleOverride = Role.ShowCategoryOverride(id, areaMode, cat);
			if (roleOverride != null) return roleOverride.Value;
			if (!Head2HeadModule.Settings.UseSRCRulesForARB) return true;
			if (cat != StandardCategory.ARB && cat != StandardCategory.ARBHeart) return true;
			switch (id) {
				default:  // custom
					if (cat == StandardCategory.ARBHeart) return false;
					else return true;
				case 0:  // Prologue
				case 6:  // Reflection
				case 8:  // Epilogue
				case 10: // Farewell
					// disallow both ARB and ARB+Heart
					return false;
				case 1:  // Forsaken City
				case 3:  // Celestial Resort
				case 4:  // Golden Ridge
					// allow only ARB+Heart
					if (cat == StandardCategory.ARB) return false;
					else return true;
				case 2:  // Old Site
				case 5:  // Mirror Temple
				case 7:  // Summit
				case 9:  // Core
					// allow only ARB
					if (cat == StandardCategory.ARBHeart) return false;
					else return true;
			}
		}

		public static List<MatchTemplate> GetCategories(GlobalAreaKey gArea, bool defaultOnly) {
			List<MatchTemplate> ret = new List<MatchTemplate>();
			StandardCategory[] cats = Role.GetValidCategories();
			AreaMetaInfo areaMetaInfo = null;
			// Standard Categories
			foreach (StandardCategory cat in cats) {
				if (cat == StandardCategory.Custom) continue;
				if (!IsCategoryValid(cat, gArea, null)) continue;
				// enforce SRC Rules setting
				if (!ShowCategory(gArea.Local?.ID, gArea.Local?.Mode ?? AreaMode.Normal, cat)) continue;
				ret.Add(GetStandardMatchTemplate(cat, gArea, ref areaMetaInfo));
			}
			// Custom Categories
			if (Role.AllowCustomCategories() && MatchTemplate.ILTemplates.ContainsKey(gArea)) {
				foreach (MatchTemplate template in MatchTemplate.ILTemplates[gArea]) {
					if (!IsCategoryValid(StandardCategory.Custom, gArea, template, defaultOnly)) continue;
					ret.Add(template);
				}
			}
			return ret;
		}

		public static string GetCategoryTitle(StandardCategory cat, MatchTemplate tem) {
			return (cat == StandardCategory.Custom && !string.IsNullOrEmpty(tem?.DisplayName)) ?
				Util.TranslatedIfAvailable(tem.DisplayName) :
				Dialog.Get(string.Format("Head2Head_CategoryName_{0}", cat.ToString()));
		}

		private static MatchTemplate GetStandardMatchTemplate(StandardCategory cat, GlobalAreaKey area, ref AreaMetaInfo info) {
			if (info == null) {
				info = new AreaMetaInfo() {
					BerryCount = Util.TrackedBerryCount(area),
					HasCassette = Util.HasCassette(area),
					HasOptionalHeart = Util.HasOptionalRealHeart(area),
					CanFC = area.Data?.CanFullClear ?? false,
					MoonBerryCount = Util.CountMoonBerries(area),
				};
			}
			switch (cat) {
				default:
				case StandardCategory.Custom:
					return null;
				case StandardCategory.Clear:
					return StandardTemplate_Clear(area, info);
				case StandardCategory.ARB:
					return StandardTemplate_ARB(area, info);
				case StandardCategory.ARBHeart:
					return StandardTemplate_ARBHeart(area, info);
				case StandardCategory.HeartCassette:
					return StandardTemplate_HeartCassette(area, info);
				case StandardCategory.CassetteGrab:
					return StandardTemplate_CassetteGrab(area, info);
				case StandardCategory.FullClear:
					return StandardTemplate_FullClear(area, info);
				case StandardCategory.MoonBerry:
					return StandardTemplate_MoonBerry(area, info);
				case StandardCategory.FullClearMoonBerry:
					return StandardTemplate_FullClearMoonBerry(area, info);
			}
		}

		private static MatchTemplate StandardTemplate_Clear(GlobalAreaKey area, AreaMetaInfo info) {
			return new MatchTemplate() {
				AllowCheatMode = false,
				Area = area,
				DisplayName = GetCategoryTitle(StandardCategory.Clear, null),
				IconPath = Util.CategoryToIcon(StandardCategory.Clear),
				IncludeInDefaultRuleset = true,
				Phases = {
					new MatchPhaseTemplate() {
						Area = area,
						Objectives = {
							new MatchObjectiveTemplate() {
								ObjectiveType = MatchObjectiveType.ChapterComplete
							}
						}
					}
				}
			};
		}

		private static MatchTemplate StandardTemplate_ARB(GlobalAreaKey area, AreaMetaInfo info) {
			return new MatchTemplate() {
				AllowCheatMode = false,
				Area = area,
				DisplayName = GetCategoryTitle(StandardCategory.ARB, null),
				IconPath = Util.CategoryToIcon(StandardCategory.ARB),
				IncludeInDefaultRuleset = true,
				Phases = {
					new MatchPhaseTemplate() {
						Area = area,
						Objectives = {
							new MatchObjectiveTemplate() {
								ObjectiveType = MatchObjectiveType.ChapterComplete
							},
							new MatchObjectiveTemplate() {
								ObjectiveType = MatchObjectiveType.Strawberries,
								CollectableCount = info.BerryCount,
							}
						}
					}
				}
			};
		}

		private static MatchTemplate StandardTemplate_ARBHeart(GlobalAreaKey area, AreaMetaInfo info) {
			return new MatchTemplate() {
				AllowCheatMode = false,
				Area = area,
				DisplayName = GetCategoryTitle(StandardCategory.ARBHeart, null),
				IconPath = Util.CategoryToIcon(StandardCategory.ARBHeart),
				IncludeInDefaultRuleset = true,
				Phases = {
					new MatchPhaseTemplate() {
						Area = area,
						Objectives = {
							new MatchObjectiveTemplate() {
								ObjectiveType = MatchObjectiveType.ChapterComplete
							},
							new MatchObjectiveTemplate() {
								ObjectiveType = MatchObjectiveType.Strawberries,
								CollectableCount = info.BerryCount
							},
							new MatchObjectiveTemplate() {
								ObjectiveType = MatchObjectiveType.HeartCollect
							}
						}
					}
				}
			};
		}

		private static MatchTemplate StandardTemplate_HeartCassette(GlobalAreaKey area, AreaMetaInfo info) {
			return new MatchTemplate() {
				AllowCheatMode = false,
				Area = area,
				DisplayName = GetCategoryTitle(StandardCategory.HeartCassette, null),
				IconPath = Util.CategoryToIcon(StandardCategory.HeartCassette),
				IncludeInDefaultRuleset = true,
				Phases = {
					new MatchPhaseTemplate() {
						Area = area,
						Objectives = {
							new MatchObjectiveTemplate() {
								ObjectiveType = MatchObjectiveType.ChapterComplete
							},
							new MatchObjectiveTemplate() {
								ObjectiveType = MatchObjectiveType.HeartCollect
							},
							new MatchObjectiveTemplate() {
								ObjectiveType = MatchObjectiveType.CassetteCollect
							}
						}
					}
				}
			};
		}

		private static MatchTemplate StandardTemplate_CassetteGrab(GlobalAreaKey area, AreaMetaInfo info) {
			return new MatchTemplate() {
				AllowCheatMode = false,
				Area = area,
				DisplayName = GetCategoryTitle(StandardCategory.CassetteGrab, null),
				IconPath = Util.CategoryToIcon(StandardCategory.CassetteGrab),
				IncludeInDefaultRuleset = true,
				Phases = {
					new MatchPhaseTemplate() {
						Area = area,
						Objectives = {
							new MatchObjectiveTemplate() {
								ObjectiveType = MatchObjectiveType.CassetteCollect
							}
						}
					}
				}
			};
		}

		private static MatchTemplate StandardTemplate_FullClear(GlobalAreaKey area, AreaMetaInfo info) {
			MatchTemplate ret = new MatchTemplate() {
				AllowCheatMode = false,
				Area = area,
				DisplayName = GetCategoryTitle(StandardCategory.FullClear, null),
				IconPath = Util.CategoryToIcon(StandardCategory.FullClear),
				IncludeInDefaultRuleset = true,
				Phases = {
					new MatchPhaseTemplate() {
						Area = area,
						Objectives = {
							new MatchObjectiveTemplate() {
								ObjectiveType = MatchObjectiveType.ChapterComplete
							}
						}
					}
				}
			};
			if (info.HasCassette) {
				ret.Phases[0].Objectives.Add(new MatchObjectiveTemplate() {
					ObjectiveType = MatchObjectiveType.CassetteCollect
				});
			}
			if (info.HasOptionalHeart) {
				ret.Phases[0].Objectives.Add(new MatchObjectiveTemplate() {
					ObjectiveType = MatchObjectiveType.HeartCollect
				});
			}
			if (info.BerryCount > 0) {
				ret.Phases[0].Objectives.Add(new MatchObjectiveTemplate() {
					ObjectiveType = MatchObjectiveType.Strawberries,
					CollectableCount = info.BerryCount
				});
			}
			return ret;
		}

		private static MatchTemplate StandardTemplate_MoonBerry(GlobalAreaKey area, AreaMetaInfo info) {
			return new MatchTemplate() {
				AllowCheatMode = false,
				Area = area,
				DisplayName = GetCategoryTitle(StandardCategory.MoonBerry, null),
				IconPath = Util.CategoryToIcon(StandardCategory.MoonBerry),
				IncludeInDefaultRuleset = true,
				Phases = {
					new MatchPhaseTemplate() {
						Area = area,
						Objectives = {
							new MatchObjectiveTemplate() {
								ObjectiveType = MatchObjectiveType.ChapterComplete
							},
							new MatchObjectiveTemplate() {
								ObjectiveType = MatchObjectiveType.MoonBerry,
								CollectableCount = info.MoonBerryCount
							},
						}
					}
				}
			};
		}

		private static MatchTemplate StandardTemplate_FullClearMoonBerry(GlobalAreaKey area, AreaMetaInfo info) {
			MatchTemplate ret = new MatchTemplate() {
				AllowCheatMode = false,
				Area = area,
				DisplayName = GetCategoryTitle(StandardCategory.FullClearMoonBerry, null),
				IconPath = Util.CategoryToIcon(StandardCategory.FullClearMoonBerry),
				IncludeInDefaultRuleset = true,
				Phases = {
					new MatchPhaseTemplate() {
						Area = area,
						Objectives = {
							new MatchObjectiveTemplate() {
								ObjectiveType = MatchObjectiveType.ChapterComplete
							},
							new MatchObjectiveTemplate() {
								ObjectiveType = MatchObjectiveType.MoonBerry,
								CollectableCount = info.MoonBerryCount
							}
						}
					}
				}
			};
			if (info.HasCassette) {
				ret.Phases[0].Objectives.Add(new MatchObjectiveTemplate() {
					ObjectiveType = MatchObjectiveType.CassetteCollect
				});
			}
			if (info.HasOptionalHeart) {
				ret.Phases[0].Objectives.Add(new MatchObjectiveTemplate() {
					ObjectiveType = MatchObjectiveType.HeartCollect
				});
			}
			if (info.BerryCount > 0) {
				ret.Phases[0].Objectives.Add(new MatchObjectiveTemplate() {
					ObjectiveType = MatchObjectiveType.Strawberries,
					CollectableCount = info.BerryCount
				});
			}
			return ret;
		}

		private class AreaMetaInfo {
			public bool HasCassette;
			public bool HasOptionalHeart;
			public int MoonBerryCount;
			public bool CanFC;
			public int BerryCount;
		}
	}
}
