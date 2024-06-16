using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.Entities.Arcade {
	public class ArcadeHUD : Entity {
		public ArcadeHUD() {
			Tag = Tags.HUD;
		}

		public Action OnClose { get; set; }

		public void Close() {
			OnClose?.Invoke();
			RemoveSelf();
		}
	}
}
