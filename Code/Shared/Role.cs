using Celeste.Mod.Head2Head.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Celeste.Mod.Head2Head.UI;

namespace Celeste.Mod.Head2Head.Shared {
	public class Role {
		internal static string role { get { return Head2HeadModule.Settings.GetRole(); } }
		public static bool hasBTAMatchPass { get; private set; } = false;

		internal static void GiveBTAMatchPass() {
			hasBTAMatchPass = true;
		}

		public static bool AllowMatchCreate() {
			switch (role) {
				default:
					return true;
				case "bta":
					return hasBTAMatchPass;
			}
		}

		public static bool AllowMatchJoin(MatchDefinition def) {
			bool hasReq = !string.IsNullOrEmpty(def.RequiredRole);
			switch (role) {
				default:
					return !hasReq;
				case "bta":
					return def.RequiredRole == "bta";
				case "bta-host":
					return false;
			}
		}

		public static bool AllowMatchStart(bool hasJoinedMatch) {
			switch (role) {
				default:
					return hasJoinedMatch;
				case "bta-host":
					return true;
				case "bta":
					return false;
			}
		}

		public static void HandleMatchCreation(MatchDefinition def) {
			switch (role) {
				default:
					return;
				case "bta":
					hasBTAMatchPass = false;
					def.RequiredRole = "bta";
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

		public static StandardCategory[] GetValidCategories() {
			switch (role) {
				default:
					return new StandardCategory[] {
						StandardCategory.Clear,
						StandardCategory.ARB,
						StandardCategory.ARBHeart,
						StandardCategory.CassetteGrab,
						StandardCategory.HeartCassette,
						StandardCategory.FullClear,
						StandardCategory.MoonBerry,
						StandardCategory.FullClearMoonBerry,
					};
				case "debug":
					return (StandardCategory[])Enum.GetValues(typeof(StandardCategory));
				case "bta":
				case "bta-host":
				case "bta-practice":
					return new StandardCategory[] {
						StandardCategory.Clear,
						StandardCategory.OneFifthBerries,
						StandardCategory.OneThirdBerries,
					};

			}
		}

		public static bool SkipCountdown() {
			switch (role) {
				default:
					return false;
				case "bta-host":
					return true;
			}
		}

		internal static bool AllowKillingMatch() {
			switch (role) {
				default:
					return true;
				case "bta":
					return false;
			}
		}
	}
}
