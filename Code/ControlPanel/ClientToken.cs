using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.ControlPanel {
	internal static class ClientToken {
		public static string Server => "ZZZZZZZZZZZZZZZZ";

		private static ulong _counter = 1 << 32;

		public static string GetNew() {
			return (++_counter).ToString("X");
		}
	}
}
