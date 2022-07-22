using Celeste.Mod.Head2Head.Shared;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.UI {
	/// <summary>
	/// This class is a placeholder just to signal to the level select entity that the UI is being closed
	/// </summary>
	public class OuiRunSelectILExit : Oui {

		public override IEnumerator Enter(Oui from) {
			yield break;
		}

		public override IEnumerator Leave(Oui next) {
			yield break;
		}
	}
}
