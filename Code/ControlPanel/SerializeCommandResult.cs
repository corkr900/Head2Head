using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.ControlPanel {
	public struct SerializeCommandResult {
		public bool Result { get; set; }
		public string Info { get; set; }
		public string RequestID { get; set; }
	}
}
