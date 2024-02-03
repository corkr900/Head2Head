using Celeste.Mod.Head2Head.Shared;
using Celeste.Mod.UI;
using FMOD.Studio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Celeste.TextMenuExt;

namespace Celeste.Mod.Head2Head {

	[SettingName("Head2Head_Setting")]
	public class Head2HeadModuleSettings : EverestModuleSettings {

		public enum AutoStageSetting {
			Never,
			OnlyInLobby,
			Always,
		}

		public enum ShowHelpdeskInPauseMenu {
			Online = 0,
			InMatch = 1,
			InMatchOrLobby = 2,
			Always = 3,
		}

		public enum TimeServer {
			Windows,
			Pool,
			None,
		}

		#region Settings

		[SettingName("Head2Head_Setting_UseSRCARBRules")]
		[SettingSubText("Head2Head_Setting_UseSRCARBRules_Subtext")]
		public bool UseSRCRulesForARB { get; set; } = true;

		[SettingName("Head2Head_Setting_AutoStage")]
		[SettingSubText("Head2Head_Setting_AutoStage_Subtext")]
		public AutoStageSetting AutoStageNewMatches { get; set; } = AutoStageSetting.OnlyInLobby;

		[SettingName("Head2Head_Setting_ShowHDInPause")]
		[SettingSubText("Head2Head_Setting_ShowHDInPause_Subtext")]
		public ShowHelpdeskInPauseMenu ShowHelpdeskInPause { get; set; } = ShowHelpdeskInPauseMenu.Online;

		[SettingName("Head2Head_Setting_TimeServer")]
		[SettingSubText("Head2Head_Setting_TimeServer_Subtext")]
		public TimeServer NSTPTimeServer { get; set; } = TimeServer.Windows;

		public string RealExportLocation {
			get {
				return /*string.IsNullOrEmpty(ExportDirectory) ?*/
					Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
					/*: ExportDirectory*/;
			}
		}

		// Settings with manual handling
		[SettingIgnore]
		public Role ActiveRole { get; set; } = Role.None;
		[SettingIgnore]
		public string Ruleset { get; set; } = "default";  // TODO (!!!) handle this becoming invalid by uninstalling a mod
		[SettingIgnore]
		public float HudScale { get; set; } = 1.0f;
		[SettingIgnore]
		public float HudOpacityNotInMatch { get; set; } = 1.0f;
		[SettingIgnore]
		public float HudOpacityInMatch { get; set; } = 0.25f;
		[SettingIgnore]
		public float HudOpacityInOverworld { get; set; } = 0.5f;

		// Secret debug-only settings
		[SettingIgnore]
		public bool HidePauseMenuHelpdesk { get; set; } = false;

		#endregion

		#region Menu Building

		internal void CreateOptions(TextMenu menu, bool inGame, EventInstance snapshot)
		{
			Head2HeadModule.Instance.ScanModsForIntegrationMeta(true);

			AddSlider(menu, "Head2Head_Setting_HudScale", HudScale,
				new float[] { 0.2f, 0.3f, 0.4f, 0.5f, 0.6f, 0.7f, 0.8f, 0.9f, 1.0f, 1.1f, 1.2f, 1.3f, 1.4f, 1.5f, 1.6f, 1.7f, 1.8f, 1.9f, 2.0f },
				(float val) => HudScale = val);
			AddSlider(menu, "Head2Head_Setting_HudOpacityBeforeMatch", HudOpacityNotInMatch,
				new float[] { 0.0f, 0.1f, 0.25f, 0.5f, 1.0f },
				(float val) => HudOpacityNotInMatch = val);
			AddSlider(menu, "Head2Head_Setting_HudOpacityInMatch", HudOpacityInMatch,
				new float[] { 0.0f, 0.1f, 0.25f, 0.5f, 1.0f },
				(float val) => HudOpacityInMatch = val);
			AddSlider(menu, "Head2Head_Setting_HudOpacityInOverworld", HudOpacityInOverworld,
				new float[] { 0.0f, 0.1f, 0.25f, 0.5f, 1.0f },
				(float val) => HudOpacityInOverworld = val);

			EnumerableSlider<RulesetOption> rulesetSlider = AddSlider(menu, "Head2Head_Setting_Ruleset",
				new RulesetOption { DisplayName = Shared.Ruleset.Get(Ruleset).DisplayName, InternalValue = Ruleset },
				GetRulesetOptions(), null);
			EnumerableSlider<Role> roleSlider = AddSlider(menu, "Head2Head_Setting_Role", ActiveRole,
				GetRoleOptions(), null);

			rulesetSlider.Change(OnRulesetChanged(rulesetSlider, roleSlider));
			roleSlider.Change(OnRoleChanged(roleSlider));
		}

		private Action<RulesetOption> OnRulesetChanged(EnumerableSlider<RulesetOption> rulesetSlider, EnumerableSlider<Role> roleSlider) {
			return (RulesetOption opt) => {
				Ruleset = opt.InternalValue;
				EnforceRuleset();
				EnforceRole();
				UpdateSlider(roleSlider, GetRoleOptions);
				Head2HeadModule.InvokeMatchUpdatedEvent();
			};
		}

		private Action<Role> OnRoleChanged(EnumerableSlider<Role> roleSlider) {
			return (Role opt) => {
				ActiveRole = opt;
				EnforceRole();
				UpdateSlider(roleSlider, GetRoleOptions);
				Head2HeadModule.InvokeMatchUpdatedEvent();
			};
		}

		private void UpdateSlider<T>(EnumerableSlider<T> slider, Func<T[]> GetVals) {
			slider.Values.Clear();
			slider.Index = 0;
			slider.PreviousIndex = 0;
			T[] vals = GetVals();
			foreach (T role in vals) {
				slider.Add(role.ToString(), role, role.Equals(ActiveRole));
			}
		}

		private RulesetOption[] GetRulesetOptions() {
			List<RulesetOption> list = new List<RulesetOption>() {
				new RulesetOption { DisplayName = Dialog.Clean("Head2Head_DefaultRulesetName"), InternalValue = "default" },
			};
			foreach(KeyValuePair<string, Ruleset> rset in Shared.Ruleset.CustomRulesets()) {
				list.Add(new RulesetOption {
					DisplayName = Shared.Util.TranslatedIfAvailable(rset.Value.DisplayName),
					InternalValue = rset.Key,
				});
			}
			return list.ToArray();
		}

		private Role[] GetRoleOptions() {
			Ruleset rset = Shared.Ruleset.Get(Ruleset);
			if ((rset.Roles?.Count ?? 0) > 0) return rset.Roles.ToArray();
			return new Role[] { Role.None };
		}

		/// <summary>
		/// Ensures the current ruleset is valid. This should be done before validating role.
		/// If the ruleset is invalid, it will get set to "default"
		/// </summary>
		/// <returns>true if ruleset was changed, false if already valid</returns>
		private bool EnforceRuleset() {
			if (!Shared.Ruleset.IsValid(Ruleset)) {
				Ruleset = "default";
				return true;
			}
			return false;
		}

		/// <summary>
		/// Ensures the current role is valid. This should be done after validing ruleset.
		/// If the ruleset is invalid, it will get set to the first-listed role in the ruleset
		/// </summary>
		/// <returns>true if role was changed, false if already valid</returns>
		private bool EnforceRole() {
			Ruleset rset = Shared.Ruleset.Get(Ruleset);
			if (rset.Roles.Count > 0 && !rset.Roles.Contains(ActiveRole)) {
				ActiveRole = rset.Roles[0];
				return true;
			}
			return false;
		}

		private struct RulesetOption {
			public string DisplayName;
			public string InternalValue;

			public override string ToString() {
				return DisplayName;
			}
		}

		#endregion

		#region Helpers

		private EnumerableSlider<T> AddSlider<T>(TextMenu menu, string labelkey, T setting, IEnumerable<T> vals, Action<T> changed)
		{
			EnumerableSlider<T> slider = new EnumerableSlider<T>(
				Dialog.Get(labelkey), vals, setting);
			slider.Change(changed);
			menu.Add(slider);
			slider.AddDescription(menu, Dialog.Clean(labelkey + "_subtext"));
			return slider;
		}


		#endregion
	}

}
