using Celeste.Mod.Head2Head.Integration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.Shared {

	public class CustomMatchTemplate {
		internal static Dictionary<GlobalAreaKey, List<CustomMatchTemplate>> templates = new Dictionary<GlobalAreaKey, List<CustomMatchTemplate>>();

		internal void Register() {
			if (!templates.ContainsKey(Area)) templates.Add(Area, new List<CustomMatchTemplate>());
			templates[Area].Add(this);
		}

		public string Key;
		public GlobalAreaKey Area;
		public string IconPath;
		public string DisplayName;
		public List<CustomMatchPhaseTemplate> Phases = new List<CustomMatchPhaseTemplate>();

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

		public static void AddTemplateFromMeta(CategoryMeta meta, GlobalAreaKey area) {
			// Process Match data
			CustomMatchTemplate tem = new CustomMatchTemplate();
			tem.Area = area;
			tem.Key = meta.ID;
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
					CustomMatchObjectiveTemplate obtem = new CustomMatchObjectiveTemplate();
					MatchObjectiveType? t = GetObjectiveType(ob.Type);
					if (t == null) {
						Logger.Log(LogLevel.Warn, "Head2Head", "Invalid objective type: " + ob.Type + " for category: " + meta.ID);
						return;
					}
					obtem.ObjectiveType = t ?? MatchObjectiveType.ChapterComplete;
					obtem.CollectableCount = ob.Count > 0 ? ob.Count :
						(obtem.ObjectiveType == MatchObjectiveType.Strawberries
						|| obtem.ObjectiveType == MatchObjectiveType.MoonBerry
						|| obtem.ObjectiveType == MatchObjectiveType.CustomCollectable) ? 1 : -1;
					if (!string.IsNullOrEmpty(ob.TimeLimit)) {
						string[] split = ob.TimeLimit.Split(':');
						if (split.Length > 1) {
							int minutes, seconds;
							if (!int.TryParse(split[0], out minutes) || !int.TryParse(split[1], out seconds)) {
								Logger.Log(LogLevel.Warn, "Head2Head", "Malformed time limit (" + ob.TimeLimit + ") in category: " + meta.ID);
								return;
							}
							obtem.TimeLimit = Util.TimeValueInternal(minutes, seconds);
						}
						else {
							int seconds;
							if (!int.TryParse(split[0], out seconds)) {
								Logger.Log(LogLevel.Warn, "Head2Head", "Malformed time limit (" + ob.TimeLimit + ") in category: " + meta.ID);
								return;
							}
							obtem.TimeLimit = Util.TimeValueInternal(0, seconds);
						}
						
					}
					else obtem.TimeLimit = 0;
					obtem.CustomTypeKey = ob.ID;
					obtem.Description = ob.Description;
					phtem.Objectives.Add(obtem);
				}
				tem.Phases.Add(phtem);
			}

			// Add to the library
			if (!templates.ContainsKey(area)) templates.Add(area, new List<CustomMatchTemplate>());
			CustomMatchTemplate existing = templates[area].Find((CustomMatchTemplate cmt) => { return cmt.Key == tem.Key; });
			if (existing != null) templates[area].Remove(existing);  // TODO reload stuff more smartly (it won't change very often)
			templates[area].Add(tem);
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
	}

	public class CustomMatchPhaseTemplate {
		public GlobalAreaKey Area;
		public List<CustomMatchObjectiveTemplate> Objectives = new List<CustomMatchObjectiveTemplate>();
	}

	public class CustomMatchObjectiveTemplate {
		public MatchObjectiveType ObjectiveType;
		public int CollectableCount = -1;
		public long TimeLimit = 0;
		public string CustomTypeKey;
		public string Description;
	}
}
