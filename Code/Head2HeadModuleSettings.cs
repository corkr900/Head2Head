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

		#region Settings

		[SettingName("Head2Head_Setting_ReturnToLobby")]
		[SettingSubText("Head2Head_Setting_ReturnToLobby_Subtext")]
		public bool ReturnToLobby { get; set; } = true;
		[SettingName("Head2Head_Setting_AutoLaunchPhase")]
		[SettingSubText("Head2Head_Setting_AutoLaunchPhase_Subtext")]
		public bool AutoLaunchNextPhase { get; set; } = true;
		[SettingName("Head2Head_Setting_Role")]
		[SettingSubText("Head2Head_Setting_Role_Subtext")]
		[SettingInGame(false)]
		public string Role { get; set; } = "";
		internal string GetRole() => Role;
		public bool UseActionLog {
			get {
				return Role == "bta";
			}
		}

		public string RealExportLocation {
			get {
				return /*string.IsNullOrEmpty(ExportDirectory) ?*/
					Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
					/*: ExportDirectory*/;
			}
		}

		// Settings with manual handling
		[SettingIgnore]
		public float HudScale { get; set; } = 1.0f;
		[SettingIgnore]
		public float HudOpacityNotInMatch { get; set; } = 1.0f;
		[SettingIgnore]
		public float HudOpacityInMatch { get; set; } = 0.25f;
		[SettingIgnore]
		public float HudOpacityInOverworld { get; set; } = 0.5f;

		#endregion

		#region Menu Building

		internal void CreateOptions(TextMenu menu, bool inGame, EventInstance snapshot)
		{
			AddSlider(menu, "Head2Head_Setting_HudScale", HudScale,
				new float[] { 0.1f, 0.25f, 0.5f, 0.75f, 1.0f, 1.25f, 1.5f, 2.0f },
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
		}

		#endregion

		#region Helpers

		private void AddSlider<T>(TextMenu menu, string labelkey, T setting, IEnumerable<T> vals, Action<T> changed)
		{
			TextMenuExt.EnumerableSlider<T> slider = new TextMenuExt.EnumerableSlider<T>(
				Dialog.Get(labelkey), vals, setting);
			slider.Change(changed);
			menu.Add(slider);
			slider.AddDescription(menu, Dialog.Clean(labelkey + "_subtext"));
		}


		#endregion
	}

}
