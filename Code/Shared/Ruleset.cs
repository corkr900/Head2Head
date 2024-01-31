using Celeste.Mod.Head2Head.Entities;
using Celeste.Mod.Head2Head.Integration;
using Celeste.Mod.Head2Head.UI;
using Monocle;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Celeste.Mod.Head2Head.Shared {
	public class Ruleset {

		#region Static handling of default and custom rulesets

		private static Ruleset _dflt = null;
		public static Ruleset Default {
			get {
				if (_dflt == null) _dflt = new DefaultRuleset();
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
				Logger.Log(LogLevel.Warn, "Head2Head", $"Already found ruleset with ID '{ruleset.ID}'. Overwriting.");
				customRulesets.Remove(ruleset.ID);
			}
			if (string.IsNullOrEmpty(ruleset.Name)) ruleset.Name = ruleset.ID;
			RunOptionsLevelSet set = new RunOptionsLevelSet();
			foreach (ILMeta il in ruleset.Levels) {
				GlobalAreaKey area = new GlobalAreaKey(il.Map, MatchTemplate.GetAreaMode(il.Side));
				if (!area.ExistsLocal || area.IsOverworld) continue;
				AreaData data = area.Data;
				RunOptionsILChapter chap = set.Chapters.FirstOrDefault((RunOptionsILChapter ch) => ch.Data?.SID == il.Map);
				if (chap == null) {
					chap = new RunOptionsILChapter();
					set.Chapters.Add(chap);
				}
				AreaMode mode = MatchTemplate.GetAreaMode(il.Side);
				RunOptionsILSide side = chap.Sides.FirstOrDefault((RunOptionsILSide si) => si.Mode == mode);
				if (side == null) {
					side = new RunOptionsILSide();
					side.Mode = mode;
					chap.Sides.Add(side);
				}
				if (il.AddCategories != null) {
					foreach (CategoryMeta newcat in il.AddCategories) {
						RunOptionsILCategory cat = new RunOptionsILCategory();
						cat.Template = MatchTemplate.AddTemplateFromMeta(newcat, area, false);
						if (cat.Template != null) side.Categories.Add(cat);
					}
				}
			}
			customRulesets.Add(ruleset.ID, new Ruleset(ruleset.Role, ruleset.Name, set));
			Logger.Log(LogLevel.Info, "Head2Head", $"Processed ruleset {ruleset.ID}");
		}

		internal static IEnumerable<KeyValuePair<string, Ruleset>> CustomRulesets() {
			return customRulesets;
		}

		#endregion Handle building custom rulesets

		#region Handle building default ruleset

		#endregion Handle building default ruleset

		#region Instance behavior

		public readonly ImmutableList<RunOptionsLevelSet> LevelSets;
		public readonly string Role;
		public readonly string DisplayName;

		protected Ruleset(string role, string dispName, params RunOptionsLevelSet[] levels) {
			Role = role;
			DisplayName = dispName;
			LevelSets = ImmutableList.Create(levels);
		}

		#endregion Instance behavior
	}

	public class DefaultRuleset : Ruleset {
		// TODO (!!!) Respect "Use SRC ARB Categories" setting

		public DefaultRuleset() : base("", "Default", BuildSets()) {  // TODO (!!!) tokenize

		}

		private static RunOptionsLevelSet[] BuildSets() {
			List<RunOptionsLevelSet> sets = new List<RunOptionsLevelSet>();
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
				if (!setOption.Hidden && (CollabUtils2Integration.IsCollabMap?.Invoke(areaData.SID) ?? false)) {
					setOption.Hidden = true;
				}
				else if (!setOption.Hidden && (CollabUtils2Integration.IsCollabGym?.Invoke(areaData.SID) ?? false)) {
					setOption.Hidden = true;
				}

				// Add stuff about the level
				RunOptionsILChapter chapter = new RunOptionsILChapter();
				chapter.Data = areaData;
				chapter.Icon = areaData.Icon;
				chapter.Title = areaData.Name?.DialogClean();
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
			return sets.ToArray();
		}

		private static bool BuildOptions_NormalChapter(RunOptionsILChapter chapter, AreaData data, AreaKey area) {
			bool addedOptions = false;
			addedOptions |= BuildOptions_Side(chapter, new GlobalAreaKey(area.ID, AreaMode.Normal),
				Dialog.Clean(data.Interlude ? "FILE_BEGIN" : "overworld_normal").ToUpper(),
				GFX.Gui["menu/play"],
				"A");
			addedOptions |= BuildOptions_Side(chapter, new GlobalAreaKey(area.ID, AreaMode.BSide),
				Dialog.Clean("overworld_remix").ToUpper(),
				GFX.Gui["menu/remix"],
				"B");
			addedOptions |= BuildOptions_Side(chapter, new GlobalAreaKey(area.ID, AreaMode.CSide),
				Dialog.Clean("overworld_remix2").ToUpper(),
				GFX.Gui["menu/rmx2"],
				"C");
			return addedOptions;
		}


		private static bool BuildOptions_Side(RunOptionsILChapter chapter, GlobalAreaKey area, string label, MTexture icon, string id) {
			if (!area.IsValidInstalledMap || !area.IsValidMode) return false;
			List<MatchTemplate> cats = StandardMatches.GetCategories(area, true);
			if ((cats?.Count ?? 0) == 0) return false;
			RunOptionsILSide side = new RunOptionsILSide() {
				Label = label,
				Icon = icon,
				ID = id,
				Mode = area.Mode
			};
			foreach(MatchTemplate c in cats) {
				side.Categories.Add(new RunOptionsILCategory() {
					IconPath = c.IconPath,
					Title = c.DisplayName,
					Template = c,
				});
			}
			chapter.Sides.Add(side);
			return true;
		}
	}
}
