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
			if (ruleset.Chapters == null) {
				Logger.Log(LogLevel.Error, "Head2Head", $"Custom ruleset '{ruleset.ID}' needs chapters!");
				return;
			}
			if (customRulesets.ContainsKey(ruleset.ID)) {
				Logger.Log(LogLevel.Warn, "Head2Head", $"Already found ruleset with ID '{ruleset.ID}'. Overwriting.");
				customRulesets.Remove(ruleset.ID);
			}
			if (string.IsNullOrEmpty(ruleset.Name)) ruleset.Name = ruleset.ID;
			RunOptionsLevelSet set = new RunOptionsLevelSet();
			foreach (RulesetChapterMeta chapterMeta in ruleset.Chapters) {
				if (chapterMeta.Sides == null) {
					Logger.Log(LogLevel.Warn, "Head2Head", $"Custom ruleset has chapter with null sides: {chapterMeta.Name}");
					continue;
				}
				RunOptionsILChapter chap = new RunOptionsILChapter();
				chap.Data = new GlobalAreaKey(chapterMeta.MapSID).Data;
				chap.Title = chapterMeta.Name;
				chap.Icon = chapterMeta.Icon;
				foreach (var sideMeta in chapterMeta.Sides) {
					if (sideMeta.Categories == null) {
						Logger.Log(LogLevel.Warn, "Head2Head", $"Custom ruleset has side with null categories: {chapterMeta.Name} -> {sideMeta.Name}");
						continue;
					}
					RunOptionsILSide side = new RunOptionsILSide();
					side.Mode = AreaMode.Normal;
					side.Label = sideMeta.Name;
					side.Icon = GFX.Gui.GetOrDefault(sideMeta.Icon, GFX.Gui["areaselect/startpoint"]);
					foreach(var catMeta in sideMeta.Categories) {
						RunOptionsILCategory cat = new RunOptionsILCategory();
						cat.Title = catMeta.Name;
						cat.IconPath = catMeta.Icon;
						cat.Template = MatchTemplate.AddTemplateFromMeta(catMeta, GlobalAreaKey.Overworld, false);
						if (cat.Template != null) side.Categories.Add(cat);
					}
					chap.Sides.Add(side);
				}
				set.Chapters.Add(chap);
			}
			Func<string,Role> EnumParser = (string rolestr) => {
				if (Enum.TryParse(rolestr, out Role role)) return role;
				Logger.Log(LogLevel.Warn, "Head2Head", $"Encountered invalid role '{rolestr}' in custom ruleset '{ruleset.Name}'");
				return Role.None;
			};
			customRulesets.Add(ruleset.ID, new Ruleset(ruleset.Roles?.Select(EnumParser)?.ToArray(), ruleset.Name, set));
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
		public readonly ImmutableList<Role> Roles;
		public readonly string DisplayName;

		protected Ruleset(Role[] roles, string dispName, params RunOptionsLevelSet[] levels) {
			DisplayName = dispName;
			Roles = ImmutableList.Create(roles ?? Array.Empty<Role>());
			LevelSets = ImmutableList.Create(levels ?? Array.Empty<RunOptionsLevelSet>());
		}

		#endregion Instance behavior
	}

	public class DefaultRuleset : Ruleset {
		// TODO (!!!) Respect "Use SRC ARB Categories" setting
		// TODO (!!!) Custom categories aren't getting added to the default ruleset

		public DefaultRuleset() : base(null, "Default", BuildSets()) {  // TODO (!!!) tokenize

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
