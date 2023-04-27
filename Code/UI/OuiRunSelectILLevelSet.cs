using Celeste.Mod.Head2Head.Entities;
using Celeste.Mod.Head2Head.Integration;
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
			int iD = ILSelector.LastArea.Local_Safe.ID;
			string levelSet = ILSelector.LastArea.Local_Safe.LevelSet;

			int count = AreaData.Areas.Count;
			for (int num = (count + iD + Direction) % count; num != iD; num = (count + num + Direction) % count) {
				AreaData areaData = AreaData.Get(num);
				if (areaData == null) continue;
				string set = areaData.GetLevelSet();
				if (string.IsNullOrEmpty(set) || set == levelSet) continue;
				if (set == "Head2Head") continue;

				if (CollabUtils2Integration.IsCollabUtils2Installed) {
					if (!string.IsNullOrEmpty(CollabUtils2Integration.GetCollabNameForSID?.Invoke(areaData.SID))
						&& string.IsNullOrEmpty(CollabUtils2Integration.GetLobbyLevelSet?.Invoke(areaData.SID))) {
						continue;  // Exclude non-lobbies from collabs
					}
				}

				// If we get here, this is our new level set
				ILSelector.LastArea = new GlobalAreaKey(areaData.ToKey());
				break;
			}
		}
	}
}
