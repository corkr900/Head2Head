﻿using System;

namespace Celeste.Mod.Head2Head.Shared {

	public enum Role {
		None,
		Participant,
		Practice,
		Host,
	}

	public static class RoleLogic {
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
			if (def.ChangeSavefile && !AllowFullgame()) return false;
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
			if (def.ChangeSavefile && !AllowFullgame()) return false;
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
						//StandardCategory.ARB,
						//StandardCategory.ARBHeart,
						//StandardCategory.FullClear,
					};
				case "bta-host":
					return new StandardCategory[] {
						StandardCategory.Clear,
						//StandardCategory.ARB,
						//StandardCategory.ARBHeart,
						//StandardCategory.FullClear,
					};
			}
		}

		public static bool? ShowCategoryOverride(int? id, AreaMode areaMode, StandardCategory cat) {
			switch (role) {
				default:
					return null;
				case "bta":
					if (id == 0 || id == 8 || id == 10) return false;
					return null;
				case "bta-practice":
				case "bta-host":
					if (id == 8) return false;
					if (id == 10 && cat == StandardCategory.Clear) return false;
					return null;
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
