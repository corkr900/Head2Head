using Celeste.Mod.Head2Head.Integration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.Shared {

	// TODO (!!!) consider maybe making a UI for custom categories (then again, maybe not)

	public class CustomMatchTemplate {
		internal static Dictionary<GlobalAreaKey, List<CustomMatchTemplate>> templates = new Dictionary<GlobalAreaKey, List<CustomMatchTemplate>>();

		internal void Register() {
			if (!templates.ContainsKey(Area)) templates.Add(Area, new List<CustomMatchTemplate>());
			templates[Area].Add(this);
		}

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
						BerryGoal = tObj.CollectableCount,
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
			tem.DisplayName = TranslatedIfAvailable(meta.Name);
			if (GFX.Gui.Has(meta.Icon)) {
				tem.IconPath = meta.Icon;
			}
			// Process Phase data
			if (meta.Phases == null || meta.Phases.Length == 0) return;  // TODO write an error (phases not defined)
			foreach (PhaseMeta ph in meta.Phases) {
				CustomMatchPhaseTemplate phtem = new CustomMatchPhaseTemplate();
				GlobalAreaKey phArea = new GlobalAreaKey(ph.Map, GetAreaMode(ph.Side));
				if (!phArea.ExistsLocal || phArea.IsOverworld || phArea.Equals(GlobalAreaKey.Head2HeadLobby)) return;  // TODO write an error (phases has incorrect area)
				phtem.Area = phArea;
				// Process Objective data
				if (meta.Phases == null || meta.Phases.Length == 0) return;  // TODO write an error (objectives not defined)
				foreach (ObjectiveMeta ob in ph.Objectives) {
					CustomMatchObjectiveTemplate obtem = new CustomMatchObjectiveTemplate();
					MatchObjectiveType? t = GetObjectiveType(ob.Type);
					if (t == null) return;  // TODO write an error (invalid objective type)
					obtem.ObjectiveType = t ?? MatchObjectiveType.ChapterComplete;
					obtem.CollectableCount = ob.Count > 0 ? ob.Count : -1;
					obtem.TimeLimit = ob.TimeLimit;
					phtem.Objectives.Add(obtem);
				}
				tem.Phases.Add(phtem);
			}

			// Add to the library
			if (!templates.ContainsKey(area)) templates.Add(area, new List<CustomMatchTemplate>());
			templates[area].Add(tem);
		}

		private static string TranslatedIfAvailable(string name) {
			return Dialog.Has(name) ? Dialog.Clean(name) : name;
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
	}
}
