using Celeste.Mod.Head2Head.Integration;
using Celeste.Mod.Head2Head.Shared;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.UI {
	internal class DeathraceSelectorUI : Entity {

		private List<DeathraceOption> options = new();

		public DeathraceSelectorUI() {

		}

		private void LoadOptions() {
			int count = AreaData.Areas.Count;

			// Loop through normal vanilla + modded maps & add valid options
			for (int id = 0; id < count; id++) {
				AreaData areaData = AreaData.Get(id);
				string set = areaData?.LevelSet;
				if (string.IsNullOrEmpty(set)) continue;
				if (set != "Head2Head") continue;  // TODO (future) custom deathraces
				if (areaData.SID == GlobalAreaKey.Head2HeadLobbySID) continue;

				GlobalAreaKey area = new(areaData.ToKey( /* TODO (future) deathrace alt side support? */));
				MatchTemplate template = BuildMatchTemplate(area, set);
				if (template == null) continue;

				options.Add(new DeathraceOption() {
					Area = area,
					Template = template,
				});
			}
		}

		private MatchTemplate BuildMatchTemplate(GlobalAreaKey area, string levelSet = "") {
			return new MatchTemplate() {
				Area = area,
				DisplayName = area.DisplayName,
				IncludeInDefaultRuleset = false,
				Key = string.Format("deathrace:{0}", area.SID),
				Rules = new List<MatchRule> { MatchRule.DnfOnDeath },
				Phases = new List<MatchPhaseTemplate>() {
					new MatchPhaseTemplate() {
						Area = area,
						LevelSet = levelSet,
						Objectives = new List<MatchObjectiveTemplate>() {
							new MatchObjectiveTemplate() {
								ObjectiveType = MatchObjectiveType.DeathraceComplete,
							}
						}
					}
				}
			};
		}
	}

	internal class DeathraceOption {
		public GlobalAreaKey Area { get; set; }
		//public int Laps { get; set; }

		public MatchTemplate Template { get; set; }
	}
}
