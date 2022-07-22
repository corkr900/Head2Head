using Celeste.Mod.Head2Head.Entities;
using Celeste.Mod.Head2Head.Shared;
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
			// TODO handle collab lobbies better
			int iD = ILSelector.LastArea.Local_Safe.ID;
			string levelSet = ILSelector.LastArea.Local_Safe.LevelSet;

			int count = AreaData.Areas.Count;
			for (int num = (count + iD + Direction) % count; num != iD; num = (count + num + Direction) % count) {
				AreaData areaData = AreaData.Get(num);
				string set = areaData.GetLevelSet();
				if (areaData == null || (set != levelSet && set != "Head2Head")) {
					ILSelector.LastArea = new GlobalAreaKey(areaData.ToKey());
					break;
				}
			}
		}
	}
}
