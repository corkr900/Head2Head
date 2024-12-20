﻿using Celeste.Mod.Head2Head.Integration;
using Celeste.Mod.Head2Head.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.Shared {

	public class MatchTemplate {
		internal static Dictionary<GlobalAreaKey, List<MatchTemplate>> ILTemplates = new Dictionary<GlobalAreaKey, List<MatchTemplate>>();
		internal static Dictionary<string, List<MatchTemplate>> FullgameTemplates = new Dictionary<string, List<MatchTemplate>>();

		private static uint _idCounter = 0;
		private static uint NewPhaseOrObjectiveID() {
			return ++_idCounter;
		}

		internal void Register() {
			if (!ILTemplates.ContainsKey(Area)) ILTemplates.Add(Area, new List<MatchTemplate>());
			ILTemplates[Area].Add(this);
		}

		public string Key;
		public GlobalAreaKey Area;
		public string IconPath;
		public string DisplayName;
		public bool AllowCheatMode;
		public bool IncludeInDefaultRuleset;
		public List<MatchPhaseTemplate> Phases = new List<MatchPhaseTemplate>();
		public RandomizerOptionsTemplate RandoOptions;
		public List<MatchRule> Rules = new List<MatchRule>();

		public MatchDefinition BuildIL() {
			if (!Head2HeadModule.Instance.CanBuildMatch()) return null;
			MatchDefinition def = new MatchDefinition() {
				Owner = PlayerID.MyID ?? PlayerID.Default,
				CreationInstant = SyncedClock.Now,
			};
			def.Phases = BuildILPhases();
			def.CategoryDisplayNameOverride = DisplayName;
			def.Rules = new List<MatchRule>(Rules);
			def.CategoryIcon = IconPath;

			if (RandoOptions != null) {  // TODO move this into the integration file
				RandomizerIntegration.SettingsBuilder bld = new RandomizerIntegration.SettingsBuilder();
				bld.LogicType = RandoOptions.LogicType;
				bld.Difficulty = RandoOptions.Difficulty;
				bld.NumDashes = RandoOptions.NumDashes;
				bld.DifficultyEagerness = RandoOptions.DifficultyEagerness;
				bld.MapLength = RandoOptions.MapLength;
				bld.ShineLights = RandoOptions.ShineLights;
				bld.Darkness = RandoOptions.Darkness;
				bld.StrawberryDensity = RandoOptions.StrawberryDensity;
				if (RandoOptions.SeedType == "Random") {  // Random
					bld.RandomizeSeed();
				}
				else if (RandoOptions.SeedType == "Weekly") {
					int numDays = (int)(SyncedClock.Now - DateTime.MinValue).TotalDays;
					bld.RandomizeSeed(numDays / 7);
				}
				else if (RandoOptions.SeedType == "Specific") {
					bld.Seed = RandoOptions.Seed;
				}
				else {
					Logger.Log(LogLevel.Error, "Head2Head", $"Encountered unexpected seed type '{RandoOptions.SeedType}'");
					bld.RandomizeSeed();
				}
				def.RandoSettingsBuilder = bld;
			}

			return def;
		}

		public List<MatchPhase> BuildILPhases() {
			int count = 0;
			List<MatchPhase> list = new List<MatchPhase>();
			foreach (MatchPhaseTemplate tPhase in Phases) {
				MatchPhase ph = new MatchPhase();
				ph.Order = count++;
				ph.Area = tPhase.Area;
				ph.ID = NewPhaseOrObjectiveID();
				ph.Objectives = new List<MatchObjective>();
				foreach (MatchObjectiveTemplate tObj in tPhase.Objectives) {
					ph.Objectives.Add(new MatchObjective() {
						ObjectiveType = tObj.ObjectiveType,
						TimeLimit = tObj.TimeLimit,
						CollectableGoal = tObj.CollectableCount,
						CustomTypeKey = tObj.CustomTypeKey,
						CustomDescription = Util.TranslatedIfAvailable(tObj.Description),
						ID = NewPhaseOrObjectiveID(),
				});
				}
				list.Add(ph);
			}
			return list;
		}

		internal MatchDefinition BuildFullgame() {
			MatchDefinition def = new MatchDefinition() {
				Owner = PlayerID.MyID ?? PlayerID.Default,
				CreationInstant = SyncedClock.Now,
				ChangeSavefile = true,
				AllowCheatMode = AllowCheatMode,
				CategoryDisplayNameOverride = Util.TranslatedIfAvailable(DisplayName),
				Rules = new List<MatchRule>(Rules),
				CategoryIcon = IconPath,
		};
			foreach (MatchPhaseTemplate phtem in Phases) {
				MatchPhase ph = new MatchPhase() {
					category = StandardCategory.Custom,
					Area = Area,
					Fullgame = true,
					LevelSet = phtem.LevelSet,
				};
				foreach (MatchObjectiveTemplate objtem in phtem.Objectives) {
					MatchObjective ob = new MatchObjective() {
						ObjectiveType = objtem.ObjectiveType,
						TimeLimit = objtem.TimeLimit,
						CollectableGoal = objtem.CollectableCount,
						CustomTypeKey = objtem.CustomTypeKey,
						CustomDescription = Util.TranslatedIfAvailable(objtem.Description),
						Side = objtem.Side,
					};
					ph.Objectives.Add(ob);
				}
				def.Phases.Add(ph);
			}
			return def;
		}

		internal static void AddTemplateFromMeta(FullgameCategoryMeta meta, bool addToDefaultRuleset) {
			// Basic data
			MatchTemplate tem = new MatchTemplate();
			tem.Key = meta.ID;
			tem.DisplayName = meta.Name;
			tem.IconPath = meta.Icon;
			tem.Area = new GlobalAreaKey(meta.StartingMap);
			tem.AllowCheatMode = meta.AllowCheatMode ?? false;
			tem.IncludeInDefaultRuleset = addToDefaultRuleset;
			tem.Rules = ParseRules(meta.Rules);

			// Misc other handling
			MatchPhaseTemplate ph = new MatchPhaseTemplate();
			tem.Phases.Add(ph);
			ph.LevelSet = meta.LevelSet ?? "";

			// Process Objective data
			if (meta.Objectives == null || meta.Objectives.Length == 0) {
				Logger.Log(LogLevel.Warn, "Head2Head", "No objectives defined in phase for category: " + meta.ID);
				return;
			}
			foreach (ObjectiveMeta obmeta in meta.Objectives) {
				MatchObjectiveTemplate obtem = ParseObjectiveMeta(obmeta, meta.ID);
				if (obtem == null) return;
				ph.Objectives.Add(obtem);
			}

			// Add it to the library
			if (!FullgameTemplates.ContainsKey(meta.LevelSet)) FullgameTemplates.Add(meta.LevelSet, new List<MatchTemplate>());
			FullgameTemplates[meta.LevelSet].Add(tem);
		}

		public static MatchTemplate MakeTemplateFromMeta(CategoryMeta meta, GlobalAreaKey area, bool includeInDefaultRuleset) {
			// Process Match data
			MatchTemplate tem = new MatchTemplate();
			tem.Key = meta.ID;
			tem.Area = area;
			tem.DisplayName = meta.Name;
			tem.IncludeInDefaultRuleset = includeInDefaultRuleset;
			tem.Rules = ParseRules(meta.Rules);
			if (GFX.Gui.Has(meta.Icon)) {
				tem.IconPath = meta.Icon;
			}
			// Process Phase data
			if (meta.Phases == null || meta.Phases.Length == 0) {
				Logger.Log(LogLevel.Warn, "Head2Head", "No phases defined for category: " + meta.ID);
				return null;
			}
			foreach (PhaseMeta ph in meta.Phases) {
				MatchPhaseTemplate phtem = new MatchPhaseTemplate();
				GlobalAreaKey phArea = new GlobalAreaKey(ph.Map, GetAreaMode(ph.Side));
				if (!phArea.ExistsLocal || phArea.IsOverworld || phArea.Equals(GlobalAreaKey.Head2HeadLobby)) {
					Logger.Log(LogLevel.Warn, "Head2Head", "Phase has invalid area SID (" + ph.Map + ") for category: " + meta.ID);
					return null;
				}
				phtem.Area = phArea;
				// Process Objective data
				if (ph.Objectives == null || ph.Objectives.Length == 0) {
					Logger.Log(LogLevel.Warn, "Head2Head", "No objectives defined in phase for category: " + meta.ID);
					return null;
				}
				foreach (ObjectiveMeta ob in ph.Objectives) {
					MatchObjectiveTemplate obtem = ParseObjectiveMeta(ob, meta.ID);
					if (obtem == null) return null;
					phtem.Objectives.Add(obtem);
				}
				tem.Phases.Add(phtem);
			}

			// Add to the library
			if (!ILTemplates.ContainsKey(area)) ILTemplates.Add(area, new List<MatchTemplate>());
			MatchTemplate existing = ILTemplates[area].Find((MatchTemplate cmt) => { return cmt.Key == tem.Key; });
			if (existing != null) ILTemplates[area].Remove(existing);
			ILTemplates[area].Add(tem);

			return tem;
		}

		private static MatchObjectiveTemplate ParseObjectiveMeta(ObjectiveMeta meta, string catID) {
			MatchObjectiveTemplate obtem = new MatchObjectiveTemplate();
			MatchObjectiveType? t = GetObjectiveType(meta.Type);
			if (t == null) {
				Logger.Log(LogLevel.Warn, "Head2Head", "Invalid objective type: " + meta.Type + " for category: " + catID);
				return null;
			}
			obtem.ObjectiveType = t ?? MatchObjectiveType.ChapterComplete;
			obtem.CollectableCount = meta.Count > 0 ? meta.Count :
				(obtem.ObjectiveType == MatchObjectiveType.Strawberries
				|| obtem.ObjectiveType == MatchObjectiveType.MoonBerry
				|| obtem.ObjectiveType == MatchObjectiveType.CustomCollectable) ? 1 : -1;
			if (!string.IsNullOrEmpty(meta.TimeLimit)) {
				string[] split = meta.TimeLimit.Split(':');
				if (split.Length > 1) {
					int minutes, seconds;
					if (!int.TryParse(split[0], out minutes) || !int.TryParse(split[1], out seconds)) {
						Logger.Log(LogLevel.Warn, "Head2Head", "Malformed time limit (" + meta.TimeLimit + ") in category: " + catID);
						return null;
					}
					obtem.TimeLimit = Util.TimeValueInternal(minutes, seconds);
				}
				else {
					int seconds;
					if (!int.TryParse(split[0], out seconds)) {
						Logger.Log(LogLevel.Warn, "Head2Head", "Malformed time limit (" + meta.TimeLimit + ") in category: " + catID);
						return null;
					}
					obtem.TimeLimit = Util.TimeValueInternal(0, seconds);
				}

			}
			else obtem.TimeLimit = 0;
			obtem.CustomTypeKey = meta.ID;
			obtem.Description = meta.Description;
			obtem.Side = GetAreaMode(meta.Side);
			return obtem;
		}

		private static List<MatchRule> ParseRules(string[] meta) {
			if (meta == null) return new List<MatchRule>();
			List<MatchRule> rules = new List<MatchRule>();
			foreach (string rule in meta) {
				if (Enum.TryParse(rule, out MatchRule parsedRule)) {
					rules.Add(parsedRule);
				}
				else {
					Logger.Log(LogLevel.Warn, "Head2Head", $"Found invalid match rule: {rule ?? "null"}");
				}
			}
			return rules;
		}

		internal static AreaMode GetAreaMode(string side) {
			switch (side?.ToLower()) {
				default:
				case null:
				case "a":
				case "normal":
				case "aside":
					return AreaMode.Normal;
				case "b":
				case "bside":
					return AreaMode.BSide;
				case "c":
				case "cside":
					return AreaMode.CSide;
			}
		}

		internal static MatchObjectiveType? GetObjectiveType(string s) {
			MatchObjectiveType t;
			if (Enum.TryParse(s, out t)) return t;
			return null;
		}

		internal static void ClearCustomTemplates() {
			ILTemplates.Clear();
			FullgameTemplates.Clear();
		}
	}

	public class MatchPhaseTemplate {
		public GlobalAreaKey Area;
		public string LevelSet = "";
		public List<MatchObjectiveTemplate> Objectives = new List<MatchObjectiveTemplate>();
	}

	public class MatchObjectiveTemplate {
		public MatchObjectiveType ObjectiveType;
		public int CollectableCount = -1;
		public long TimeLimit = 0;
		public string CustomTypeKey;
		public string Description;
		public AreaMode Side;
	}

	[Serializable]
	public class RandomizerOptionsTemplate {
		public string Difficulty;
		public string SeedType;  // "Random", "Specific", or "Weekly"
		public string LogicType;
		public string NumDashes;
		public string DifficultyEagerness;
		public string MapLength;
		public string ShineLights;
		public string Darkness;
		public string StrawberryDensity;
		public string Seed;  // Only used if SeedType == "Specific"
	}
}
