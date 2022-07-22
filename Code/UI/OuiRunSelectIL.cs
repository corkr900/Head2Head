using Celeste.Mod.Head2Head.Entities;
using Celeste.Mod.Head2Head.Shared;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.UI {
	public class OuiRunSelectIL : Oui {

		public static bool Start;

		public override bool IsStart(Overworld overworld, Overworld.StartMode start) {
			if (Start) {
				Start = false;
				Add(new Coroutine(Enter(null)));
				return true;
			}

			return false;
		}

		public override IEnumerator Enter(Oui from) {
			Audio.Play("event:/ui/world_map/icon/select");
			Overworld.Goto<OuiRunSelectILChapterSelect>();
			yield break;
		}

		public override IEnumerator Leave(Oui next) {
			yield break;
		}
	}
}
