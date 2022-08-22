using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.Shared {
	public class Role {
		private static string role { get { return Head2HeadModule.Settings.GetRole(); } }
		public static bool AllowMatchCreate() {
			switch (role) {
				default:
					return true;
				case "bta-blue":
				case "bta-red":
				case "bta-yellow":
				case "bta-cracked":
				case "bta-lunar":
					return false;
			}
		}

		public static bool AllowMatchJoin(MatchDefinition def) {
			bool hasReq = !string.IsNullOrEmpty(def.RequiredRole);
			switch (role) {
				default:
					return !hasReq;
				case "bta-blue":
				case "bta-red":
				case "bta-yellow":
				case "bta-cracked":
				case "bta-lunar":
					return def.RequiredRole == "bta";
			}
		}

		public static bool AllowMatchStart(bool hasJoinedMatch) {
			switch (role) {
				default:
					return hasJoinedMatch;
				case "bta-host":
					return true;
				case "bta-blue":
				case "bta-red":
				case "bta-yellow":
				case "bta-cracked":
				case "bta-lunar":
					return false;
			}
		}

		public static void HandleMatchCreation(MatchDefinition def) {
			switch (role) {
				default:
					return;
				case "bta-host":
					def.RequiredRole = "bta";
					return;
			}
		}

		public static bool LeaveUnjoinedMatchOnStart() {
			switch (role) {
				default:
					return true;
				case "bta-host":
					return false;
			}
		}
	}
}
