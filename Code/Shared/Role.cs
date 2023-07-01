using System;

namespace Celeste.Mod.Head2Head.Shared {
	public class Role {
		internal static string role { get { return Head2HeadModule.Settings.Role; } }

		// Roles that do stuff:
		// debug
		// bta
		// wbta
		// bta-host

		public static bool hasBTAMatchPass { get; private set; } = false;

		public static bool IsDebug { get { return role.ToLower() == "debug"; } }

		internal static void GiveBTAMatchPass() {
			hasBTAMatchPass = true;
		}

		internal static void RemoveBTAPass() {
			hasBTAMatchPass = false;
		}

		#region Role-specific behavior

		public static bool LogMatchActions() {
			switch (role) {
				case "bta":
				case "wbta":
					return true;
				default:
					return false;
			}
		}

		public static bool AllowFullgame() {
			switch (role) {
				case "bta":
				case "wbta":
				case "bta-host":
				case "bta-practice":
					return false;
				default:
					return true;
			}
		}

		public static bool AllowMatchCreate() {
			switch (role) {
				default:
					return true;
				case "bta":
					return hasBTAMatchPass;
			}
		}

		public static bool AllowAutoStage(MatchDefinition def) {
			if (def.UseFreshSavefile && !AllowFullgame()) return false;
			bool hasReq = !string.IsNullOrEmpty(def.RequiredRole);
			switch (role) {
				default:
					return !hasReq;
				case "bta":
				case "wbta":
				case "bta-host":
					return def.RequiredRole == "bta";
			}
		}

		public static bool AllowMatchJoin(MatchDefinition def) {
			if (def.UseFreshSavefile && !AllowFullgame()) return false;
			bool hasReq = !string.IsNullOrEmpty(def.RequiredRole);
			switch (role) {
				default:
					return !hasReq;
				case "bta":
				case "wbta":
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
				case "wbta":
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
				case "wbta":
					return new StandardCategory[] {
						StandardCategory.Clear,
					};
				case "bta":
				case "bta-practice":
					return new StandardCategory[] {
						StandardCategory.Clear,
						StandardCategory.ARB,
						StandardCategory.ARBHeart,
						StandardCategory.FullClear,
					};
				case "bta-host":
					return new StandardCategory[] {
						StandardCategory.Clear,
						StandardCategory.ARB,
						StandardCategory.ARBHeart,
						StandardCategory.FullClear,
						StandardCategory.TimeLimit,
					};
			}
		}

		public static bool? ShowCategoryOverride(int? id, AreaMode areaMode, StandardCategory cat) {
			switch (role) {
				default:
					return null;
				case "bta":
				case "bta-practice":
				case "bta-host":
					if (areaMode != AreaMode.Normal) return null;
					if (id == 0) return role != "bta";
					if (cat == StandardCategory.ARB) return id == 5 || id == 7 || id == 8;
					else if (cat == StandardCategory.ARBHeart) return id == 1 || id == 2 || id == 3 || id == 4;
					else if (cat == StandardCategory.Clear) return (id == 1 && role != "bta") || id == 6;
					else if (cat == StandardCategory.TimeLimit) return id == 10 && role != "bta";
					else if (cat == StandardCategory.FullClear) return id == 9;
					return false;
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

		internal static bool AllowCustomCategories() {
			switch (role) {
				default:
					return true;
				case "bta":
				case "bta-host":
				case "bta-practice":
				case "wbta":
					return false;
			}
		}

		#endregion
	}
}
