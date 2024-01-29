using Celeste.Mod.Head2Head.Entities;
using Celeste.Mod.Head2Head.Integration;
using Celeste.Mod.Head2Head.Shared;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.UI {
	public class OuiRunSelectILLevelSet : Oui {
		public int Direction = 0;
		private static int setIndex = 0;

		public override IEnumerator Enter(Oui from) {
			if (Direction > 0) {
				yield return 0.25f;
				if (++setIndex >= OuiRunSelectIL.UsingRuleset.LevelSets.Count - 1) setIndex = 0;
			}
			else if (Direction < 0) {
				yield return 0.25f;
				if (--setIndex < 0) setIndex = OuiRunSelectIL.UsingRuleset.LevelSets.Count - 1;
			}
			else {
				GotoChapterSelect();
				yield break;
			}
			if (Direction > 0) {
				Audio.Play("event:/ui/world_map/chapter/pane_expand");
			}
			else {
				Audio.Play("event:/ui/world_map/chapter/pane_contract");
			}
			GotoChapterSelect();
		}

		private void GotoChapterSelect() {
			OuiRunSelectILChapterSelect.UsingLevelSet = OuiRunSelectIL.UsingRuleset.LevelSets[setIndex];
			Overworld.Goto<OuiRunSelectILChapterSelect>();
		}

		public override IEnumerator Leave(Oui next) {
			yield break;
		}
	}
}
