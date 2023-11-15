using Celeste.Mod.Head2Head.Integration;
using Celeste.Mod.Head2Head.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.Shared {

	public class CustomMatchTemplate {
		internal static Dictionary<GlobalAreaKey, List<CustomMatchTemplate>> ILTemplates = new Dictionary<GlobalAreaKey, List<CustomMatchTemplate>>();
		internal static Dictionary<string, List<CustomMatchTemplate>> FullgameTemplates = new Dictionary<string, List<CustomMatchTemplate>>();

		internal void Register() {
			if (!ILTemplates.ContainsKey(Area)) ILTemplates.Add(Area, new List<CustomMatchTemplate>());
			ILTemplates[Area].Add(this);
		}

		public string Key;
		public GlobalAreaKey Area;
		public string IconPath;
		public string DisplayName;
		public bool AllowCheatMode;
		public List<CustomMatchPhaseTemplate> Phases = new List<CustomMatchPhaseTemplate>();
		public RandomizerOptionsTemplate RandoOptions;

		public List<MatchPhase> Build() {
			int count = 0;
			List<MatchPhase> list = new List<MatchPhase>();
			foreach (CustomMatchPhaseTemplate tPhase in Phases) {
				MatchPhase ph = new MatchPhase();
				ph.category = StandardCategory.Custom;
				ph.Order = count++;
				ph.Area = tPhase.Area;
				ph.Objectives = new List<MatchObjective>();
				foreach (CustomMatchObjectiveTemplate tObj in tPhase.Objectives) {
					ph.Objectives.Add(new MatchObjective() {
						ObjectiveType = tObj.ObjectiveType,
						TimeLimit = tObj.TimeLimit,
						CollectableGoal = tObj.CollectableCount,
						CustomTypeKey = tObj.CustomTypeKey,
						CustomDescription = Util.TranslatedIfAvailable(tObj.Description),
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
			};
			foreach (CustomMatchPhaseTemplate phtem in Phases) {
				MatchPhase ph = new MatchPhase() {
					category = StandardCategory.Custom,
					Area = Area,
					Fullgame = true,
					LevelSet = phtem.LevelSet,
				};
				foreach (CustomMatchObjectiveTemplate objtem in phtem.Objectives) {
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

		internal static void AddTemplateFromMeta(FullgameMeta meta) {
			// Basic data
			CustomMatchTemplate tem = new CustomMatchTemplate();
			tem.Key = meta.ID;
			tem.DisplayName = meta.Name;
			tem.IconPath = meta.Icon;
			tem.Area = new GlobalAreaKey(meta.StartingMap);
			tem.AllowCheatMode = meta.AllowCheatMode ?? false;

			// Misc other handling
			CustomMatchPhaseTemplate ph = new CustomMatchPhaseTemplate();
			tem.Phases.Add(ph);
			ph.LevelSet = meta.LevelSet ?? "";

			// Process Objective data
			if (meta.Objectives == null || meta.Objectives.Length == 0) {
				Logger.Log(LogLevel.Warn, "Head2Head", "No objectives defined in phase for category: " + meta.ID);
				return;
			}
			foreach (ObjectiveMeta obmeta in meta.Objectives) {
				CustomMatchObjectiveTemplate obtem = ParseObjectiveMeta(obmeta, meta.ID);
				if (obtem == null) return;
				ph.Objectives.Add(obtem);
			}

			// Add it to the library
			if (!FullgameTemplates.ContainsKey(meta.LevelSet)) FullgameTemplates.Add(meta.LevelSet, new List<CustomMatchTemplate>());
			FullgameTemplates[meta.LevelSet].Add(tem);
		}

		public static void AddTemplateFromMeta(CategoryMeta meta, GlobalAreaKey area) {
			// Process Match data
			CustomMatchTemplate tem = new CustomMatchTemplate();
			tem.Key = meta.ID;
			tem.Area = area;
			tem.DisplayName = meta.Name;
			if (GFX.Gui.Has(meta.Icon)) {
				tem.IconPath = meta.Icon;
			}
			// Process Phase data
			if (meta.Phases == null || meta.Phases.Length == 0) {
				Logger.Log(LogLevel.Warn, "Head2Head", "No phases defined for category: " + meta.ID);
				return;
			}
			foreach (PhaseMeta ph in meta.Phases) {
				CustomMatchPhaseTemplate phtem = new CustomMatchPhaseTemplate();
				GlobalAreaKey phArea = new GlobalAreaKey(ph.Map, GetAreaMode(ph.Side));
				if (!phArea.ExistsLocal || phArea.IsOverworld || phArea.Equals(GlobalAreaKey.Head2HeadLobby)) {
					Logger.Log(LogLevel.Warn, "Head2Head", "Phase has invalid area SID (" + ph.Map + ") for category: " + meta.ID);
					return;
				}
				phtem.Area = phArea;
				// Process Objective data
				if (ph.Objectives == null || ph.Objectives.Length == 0) {
					Logger.Log(LogLevel.Warn, "Head2Head", "No objectives defined in phase for category: " + meta.ID);
					return;
				}
				foreach (ObjectiveMeta ob in ph.Objectives) {
					CustomMatchObjectiveTemplate obtem = ParseObjectiveMeta(ob, meta.ID);
					if (obtem == null) return;
					phtem.Objectives.Add(obtem);
				}
				tem.Phases.Add(phtem);
			}

			// Add to the library
			if (!ILTemplates.ContainsKey(area)) ILTemplates.Add(area, new List<CustomMatchTemplate>());
			CustomMatchTemplate existing = ILTemplates[area].Find((CustomMatchTemplate cmt) => { return cmt.Key == tem.Key; });
			if (existing != null) ILTemplates[area].Remove(existing);  // TODO reload stuff more smartly (it won't change very often)
			ILTemplates[area].Add(tem);
		}

		private static CustomMatchObjectiveTemplate ParseObjectiveMeta(ObjectiveMeta meta, string catID) {
			CustomMatchObjectiveTemplate obtem = new CustomMatchObjectiveTemplate();
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

	public class CustomMatchPhaseTemplate {
		public GlobalAreaKey Area;
		public string LevelSet = "";
		public List<CustomMatchObjectiveTemplate> Objectives = new List<CustomMatchObjectiveTemplate>();
	}

	public class CustomMatchObjectiveTemplate {
		public MatchObjectiveType ObjectiveType;
		public int CollectableCount = -1;
		public long TimeLimit = 0;
		public string CustomTypeKey;
		public string Description;
		public AreaMode Side;
	}

	public class RandomizerOptionsTemplate {
		public string Difficulty;
		public string SeedType;
		public string LogicType;
		public string NumDashes;
		public string DifficultyEagerness;
		public string MapLength;
		public string ShineLights;
		public string Darkness;
		public string StrawberryDensity;
	}
}
