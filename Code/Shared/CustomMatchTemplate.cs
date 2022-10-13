using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.Shared {

	// TODO (!!!) define custom categories in a formatted file somewhere
	// TODO (!!!) consider maybe making a UI for custom categories (then again, maybe not)

	public class CustomMatchTemplate {
		internal static Dictionary<string, List<CustomMatchTemplate>> templates = new Dictionary<string, List<CustomMatchTemplate>>();

		internal void Register() {
			if (!templates.ContainsKey(MapSID)) templates.Add(MapSID, new List<CustomMatchTemplate>());
			templates[MapSID].Add(this);
		}

		public string MapSID;
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
				ph.Area = new GlobalAreaKey(tPhase.PhaseSID, tPhase.Mode);
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
	}

	public class CustomMatchPhaseTemplate {
		public string DisplayName;
		public string PhaseSID;
		public AreaMode Mode;
		public List<CustomMatchObjectiveTemplate> Objectives = new List<CustomMatchObjectiveTemplate>();
	}

	public class CustomMatchObjectiveTemplate {
		public MatchObjectiveType ObjectiveType;
		public int CollectableCount = -1;
		public long TimeLimit = 0;
	}
}
