using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.ControlPanel {
	internal struct SerializeWelcomeArgs {

		public string Token { get; set; }
		public int Version { get; set; }
		public bool RandomizerInstalled { get; set; }

	}
}
