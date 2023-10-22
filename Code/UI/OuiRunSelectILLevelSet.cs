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
		public int Direction;

		public override IEnumerator Enter(Oui from) {
			if (Direction == 0) {
				Overworld.Goto<OuiRunSelectILChapterSelect>();
			}
			else {
				Direction = Math.Sign(Direction);
				yield return 0.25f;
				SetNext();
			}
			if (Direction > 0) {
				Audio.Play("event:/ui/world_map/chapter/pane_expand");
			}
			else {
				Audio.Play("event:/ui/world_map/chapter/pane_contract");
			}
			Overworld.Goto<OuiRunSelectILChapterSelect>();
		}

		public override IEnumerator Leave(Oui next) {
			yield break;
		}

		public void SetNext() {
			if (Direction < 0) {
				OuiRunSelectIL.PreviousLevelSet(ILSelector.LastLevelSetIndex, ref ILSelector.LastLevelSetIndex);
				ILSelector.LastChapterIndex = Calc.Clamp(ILSelector.LastChapterIndex, 0, OuiRunSelectIL.GetNumOptionsInSet(ILSelector.LastLevelSetIndex) - 1);
			}
			else {
				OuiRunSelectIL.NextLevelSet(ILSelector.LastLevelSetIndex, ref ILSelector.LastLevelSetIndex);
				ILSelector.LastChapterIndex = Calc.Clamp(ILSelector.LastChapterIndex, 0, OuiRunSelectIL.GetNumOptionsInSet(ILSelector.LastLevelSetIndex) - 1);
			}
		}
	}
}
