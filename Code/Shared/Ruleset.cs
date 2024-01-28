using Celeste.Mod.Head2Head.Entities;
using Celeste.Mod.Head2Head.Integration;
using Celeste.Mod.Head2Head.UI;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.Shared {
	public class Ruleset {

		#region Static handling of default and custom rulesets

		private static Ruleset _dflt = null;
		public static Ruleset Default {
			get {
				if (_dflt == null) _dflt = BuildDefault();
				return _dflt;
			}
		}

		private static Dictionary<string, Ruleset> customRulesets = new Dictionary<string, Ruleset>();

		public static Ruleset Get(string name) {
			if (string.IsNullOrEmpty(name) || name.ToLower() == "default" || !customRulesets.ContainsKey(name)) {
				return Default;
			}
			return customRulesets[name];
		}

		public static Ruleset Current {
			get {
				return Get(Head2HeadModule.Settings?.Ruleset);
			}
		}

		#endregion Static handling of default and custom rulesets

		#region Handle building custom rulesets

		internal static void ProcessMeta(RulesetMeta ruleset) {
			if (string.IsNullOrEmpty(ruleset.ID)) {
				Logger.Log(LogLevel.Error, "Head2Head", "Encountered custom ruleset with null ID :(");
				return;
			}
			if (ruleset.Levels == null) {
				Logger.Log(LogLevel.Error, "Head2Head", $"Custom ruleset '{ruleset.ID}' needs levels!");
				return;
			}
			if (customRulesets.ContainsKey(ruleset.ID)) {
				// TODO this warning triggers when re-loading the same ruleset on a subsequent entry to the lobby...
				Logger.Log(LogLevel.Warn, "Head2Head", $"Encountered duplicate ruleset ID '{ruleset.ID}'. Skipping.");
				return;
			}
			if (string.IsNullOrEmpty(ruleset.Name)) ruleset.Name = ruleset.ID;
			RunOptionsLevelSet set = new RunOptionsLevelSet();
			foreach (ILMeta il in ruleset.Levels) {
				GlobalAreaKey area = new GlobalAreaKey(il.Map, CustomMatchTemplate.GetAreaMode(il.Side));
				if (!area.ExistsLocal || area.IsOverworld) continue;
				AreaData data = area.Data;
				RunOptionsILChapter chap = set.Chapters.FirstOrDefault((RunOptionsILChapter ch) => ch.Data?.SID == il.Map);
				if (chap == null) {
					chap = new RunOptionsILChapter();
					set.Chapters.Add(chap);
				}
				AreaMode mode = CustomMatchTemplate.GetAreaMode(il.Side);
				RunOptionsILSide side = chap.Sides.FirstOrDefault((RunOptionsILSide si) => si.Mode == mode);
				if (side == null) {
					side = new RunOptionsILSide();
					side.Mode = mode;
					chap.Sides.Add(side);
				}
				if (il.AddCategories != null) {
					foreach (CategoryMeta newcat in il.AddCategories) {
						RunOptionsILCategory cat = new RunOptionsILCategory();
						cat.CustomTemplate = CustomMatchTemplate.AddTemplateFromMeta(newcat, area, false);
						if (cat.CustomTemplate != null) side.Categories.Add(cat);
					}
				}
			}
			customRulesets.Add(ruleset.ID, new Ruleset(ruleset.Role, set));
			Logger.Log(LogLevel.Info, "Head2Head", $"Processed ruleset {ruleset.ID}");
		}

		#endregion Handle building custom rulesets

		#region Handle building default ruleset

		private static int _specialIDCounter = 0;
		internal static int NewSpecialID() {
			return ++_specialIDCounter;
		}

		private static Ruleset BuildDefault() {
			List<RunOptionsLevelSet> sets = new List<RunOptionsLevelSet>();
			_specialIDCounter = 0;
			int count = AreaData.Areas.Count;
			string levelSet = "";
			RunOptionsLevelSet setOption = null;
			bool setAdded = false;

			// Loop through normal vanilla + modded maps & add valid options
			for (int id = 0; id < count; id++) {
				AreaData areaData = AreaData.Get(id);
				if (areaData == null) continue;
				if (CollabUtils2Integration.IsCollabGym?.Invoke(areaData.SID) ?? false) continue;
				string set = areaData.LevelSet;
				if (string.IsNullOrEmpty(set)) continue;
				if (set == "Head2Head") continue;

				// Add a new level set option if we need to
				if (set != levelSet || setOption == null) {
					levelSet = set;
					setOption = new RunOptionsLevelSet();
					setOption.LevelSet = levelSet;
					setAdded = false;
				}

				// Hide non-lobby collab maps
				if (CollabUtils2Integration.IsCollabMap?.Invoke(areaData.SID) ?? false) {
					setOption.Hidden = true;
				}
				else if (CollabUtils2Integration.IsCollabGym?.Invoke(areaData.SID) ?? false) {
					setOption.Hidden = true;
				}

				// Add stuff about the level
				RunOptionsILChapter chapter = new RunOptionsILChapter();
				chapter.Data = areaData;
				if (BuildOptions_NormalChapter(chapter, areaData, areaData.ToKey())) {
					setOption.Chapters.Add(chapter);
					if (!setAdded) {
						sets.Add(setOption);
						setAdded = true;
					}
				}
			}

			// Add special options
			RandomizerCategories.AddRandomizerCategories(sets);
			return new Ruleset("", sets.ToArray());
		}

		private static bool BuildOptions_NormalChapter(RunOptionsILChapter chapter, AreaData data, AreaKey area) {
			bool addedOptions = false;
			if (StandardMatches.HasAnyValidCategory(new GlobalAreaKey(area.ID, AreaMode.Normal), true)) {
				chapter.Sides.Add(new RunOptionsILSide {
					Label = Dialog.Clean(data.Interlude ? "FILE_BEGIN" : "overworld_normal").ToUpper(),
					Icon = GFX.Gui["menu/play"],
					ID = "A",
					Mode = AreaMode.Normal,
				});
				addedOptions = true;
			}
			if (StandardMatches.HasAnyValidCategory(new GlobalAreaKey(area.ID, AreaMode.BSide), true)) {
				chapter.Sides.Add(new RunOptionsILSide {
					Label = Dialog.Clean("overworld_remix"),
					Icon = GFX.Gui["menu/remix"],
					ID = "B",
					Mode = AreaMode.BSide,
				});
				addedOptions = true;
			}
			if (StandardMatches.HasAnyValidCategory(new GlobalAreaKey(area.ID, AreaMode.CSide), true)) {
				chapter.Sides.Add(new RunOptionsILSide {
					Label = Dialog.Clean("overworld_remix2"),
					Icon = GFX.Gui["menu/rmx2"],
					ID = "C",
					Mode = AreaMode.CSide,
				});
				addedOptions = true;
			}
			return addedOptions;
		}

		#endregion Handle building default ruleset

		#region Instance behavior

		public readonly ImmutableList<RunOptionsLevelSet> LevelSets;
		public readonly string Role;

		private Ruleset(string role, params RunOptionsLevelSet[] levels) {
			Role = role;
			LevelSets = ImmutableList.Create(levels);
		}

		#endregion Instance behavior
	}
}
