using System;
using System.Collections.Generic;

namespace Celeste.Mod.Head2Head.Shared {

	public enum Role {
		None,
		Debug,
		Participant,
		Practice,
		Host,
	}

	public static class RoleLogic {
		internal static Role ActiveRole => Head2HeadModule.Settings.ActiveRole;
		internal static string ActiveRulesetID => Head2HeadModule.Settings.Ruleset;

		// Roles that do stuff:
		// debug
		// bta
		// wbta
		// bta-host

		public static bool HasBTAMatchPass { get; private set; } = false;

		public static bool IsDebug { get { return ActiveRole == Role.Debug; } }

		internal static void GiveBTAMatchPass() {
			HasBTAMatchPass = true;
		}

		internal static void RemoveBTAPass() {
			HasBTAMatchPass = false;
		}

		#region Role-specific behavior

		public static bool LogMatchActions() {
			switch (ActiveRole) {
				case Role.Participant:
					return true;
				default:
					return false;
			}
		}

		public static bool AllowFullgame() {
			switch (ActiveRole) {
				case Role.Participant:
				case Role.Host:
				case Role.Practice:
					return false;
				default:
					return true;
			}
		}

		public static bool AllowMatchCreate() {
			switch (ActiveRole) {
				default:
					return true;
				case Role.Participant:
					return HasBTAMatchPass;
			}
		}

		private static bool IsCurrentRoleAllowed(MatchDefinition def) {
			if (def.AllowedRoles == null || def.AllowedRoles.Count == 0)
				return ActiveRole == Role.None || ActiveRole == Role.Debug || ActiveRole == Role.Practice;
			else return def.AllowedRoles.Contains(ActiveRole);
		}

		private static bool IsCurrentRulesetAllowed(MatchDefinition def) {
			if (string.IsNullOrEmpty(def.RequiredRuleset)) return true;
			else return def.RequiredRuleset == ActiveRulesetID;
		}

		public static bool IsCurrentRoleAndRulesetAllowed(MatchDefinition def)
			=> IsCurrentRoleAllowed(def) && IsCurrentRulesetAllowed(def);

		public static bool AllowAutoStage(MatchDefinition def) {
			if (def.ChangeSavefile && !AllowFullgame()) return false;
			if (ActiveRole == Role.Host) return IsCurrentRulesetAllowed(def);
			return IsCurrentRoleAndRulesetAllowed(def);
		}

		public static bool AllowMatchJoin(MatchDefinition def) {
			if (def.ChangeSavefile && !AllowFullgame()) return false;
			if (ActiveRole == Role.Host) return false;
			return IsCurrentRoleAndRulesetAllowed(def);
		}

		public static bool AllowMatchStart(bool hasJoinedMatch) {
			return ActiveRole switch {
				Role.Host => true,
				Role.Participant => false,
				_ => hasJoinedMatch,
			};
		}

		public static void HandleMatchCreation(MatchDefinition def) {
			switch (ActiveRole) {
				default:
					return;
				case Role.Host:
				case Role.Participant:
					HasBTAMatchPass = false;
					def.AllowedRoles = new List<Role>() {
						Role.Participant,
					};
					def.RequiredRuleset = ActiveRulesetID;
					return;
			}
		}

		public static bool LeaveUnjoinedMatchOnStart() {
			switch (ActiveRole) {
				default:
					return true;
				case Role.Host:
					return false;
			}
		}

		public static bool SkipCountdown() {
			switch (ActiveRole) {
				default:
					return false;
				case Role.Host:
					return true;
			}
		}

		internal static bool AllowKillingMatch() {
			switch (ActiveRole) {
				default:
					return true;
				case Role.Participant:
					return false;
			}
		}

		internal static bool CanGrantMatchPass() {
			return ActiveRole switch {
				Role.Host => true,
				Role.Debug => true,
				_ => false
			};
		}

		#endregion
	}
}
