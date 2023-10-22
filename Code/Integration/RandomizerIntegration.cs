using Celeste.Mod.CelesteNet;
using Celeste.Mod.Head2Head.Shared;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Celeste.Mod.Head2Head.Integration.RandomizerIntegration;

namespace Celeste.Mod.Head2Head.Integration {
	public class RandomizerIntegration {

		public static bool RandomizerLoaded { get; private set; } = false;

		private static Type t_RandoSettings;
		private static MethodInfo m_RandoLogic_GenerateMap = null;
		private static PropertyInfo p_RandoModule_SavedData = null;
		private static PropertyInfo p_RandoModuleSettings_SavedSettings = null;
		private static FieldInfo f_RandoModule_StartMe = null;
		private static FieldInfo f_RandoModule_Instance = null;

		private static Func<object, AreaKey> RandoLogic_GenerateMap = null;
		private static Action<AreaKey?> RandoModule_StartMe_Set = null;
		//private static Func<AreaKey?> RandoModule_StartMe_Get = null;
		//private static Action<object, object> RandoModule_SavedData_SavedSettings_Set = null;

		internal static ExternalEnum SeedTypeEnum;
		internal static ExternalEnum LogicTypeEnum;
		internal static ExternalEnum MapLengthEnum;
		internal static ExternalEnum NumDashesEnum;
		internal static ExternalEnum DifficultyEnum;
		internal static ExternalEnum DifficultyEagernessEnum;
		internal static ExternalEnum ShineLightsEnum;
		internal static ExternalEnum DarknessEnum;
		internal static ExternalEnum StrawberryDensityEnum;

		public static void Load() {
			RandomizerLoaded = false;
			try {
				// Get all the types / fieldinfo / methodinfo necessary
				t_RandoSettings = Type.GetType("Celeste.Mod.Randomizer.RandoSettings,Randomizer");

				Type t_RandoLogic = Type.GetType("Celeste.Mod.Randomizer.RandoLogic,Randomizer");
				m_RandoLogic_GenerateMap = t_RandoLogic.GetMethod("GenerateMap", BindingFlags.Static | BindingFlags.Public);

				Type t_RandoModule = Type.GetType("Celeste.Mod.Randomizer.RandoModule,Randomizer");
				p_RandoModule_SavedData = t_RandoModule.GetProperty("SavedData", BindingFlags.Instance | BindingFlags.Public);
				f_RandoModule_Instance = t_RandoModule.GetField("Instance", BindingFlags.Static | BindingFlags.Public);
				f_RandoModule_StartMe = t_RandoModule.GetField("StartMe", BindingFlags.Static | BindingFlags.Public);

				Type t_RandoModuleSettings = p_RandoModule_SavedData.PropertyType;
				p_RandoModuleSettings_SavedSettings = t_RandoModuleSettings.GetProperty("SavedSettings", BindingFlags.Instance | BindingFlags.Public);

				SeedTypeEnum = new ExternalEnum("Celeste.Mod.Randomizer.SeedType,Randomizer");
				LogicTypeEnum = new ExternalEnum("Celeste.Mod.Randomizer.LogicType,Randomizer");
				MapLengthEnum = new ExternalEnum("Celeste.Mod.Randomizer.MapLength,Randomizer");
				NumDashesEnum = new ExternalEnum("Celeste.Mod.Randomizer.NumDashes,Randomizer");
				DifficultyEnum = new ExternalEnum("Celeste.Mod.Randomizer.Difficulty,Randomizer");
				DifficultyEagernessEnum = new ExternalEnum("Celeste.Mod.Randomizer.DifficultyEagerness,Randomizer");
				ShineLightsEnum = new ExternalEnum("Celeste.Mod.Randomizer.ShineLights,Randomizer");
				DarknessEnum = new ExternalEnum("Celeste.Mod.Randomizer.Darkness,Randomizer");
				StrawberryDensityEnum = new ExternalEnum("Celeste.Mod.Randomizer.StrawberryDensity,Randomizer");

				// Build the delegates for ease of use
				RandoLogic_GenerateMap = (object settings) => {
					return (AreaKey)m_RandoLogic_GenerateMap.Invoke(null, new object[] { settings });
				};

				RandoModule_StartMe_Set = (AreaKey? key) => {
					f_RandoModule_StartMe.SetValue(null, key);
				};

				//RandoModule_StartMe_Get = () => {
				//	return (AreaKey?)f_RandoModule_StartMe.GetValue(null);
				//};

				//RandoModule_SavedData_SavedSettings_Set = (object randoModuleInstance, object settings) => {
				//	object SavedData = p_RandoModule_SavedData.GetValue(randoModuleInstance);
				//	p_RandoModuleSettings_SavedSettings.SetValue(SavedData, settings);
				//};

				RandomizerLoaded = true;
			}
			catch(Exception e) {
				RandomizerLoaded = false;
			}
		}

		internal static void Unload() {
			RandomizerLoaded = false;
		}

		/// <summary>
		/// Class implemented to build out a RandoSettings object
		/// </summary>
		public class SettingsBuilder {

			public string Seed { get; set; }
			public string LogicType { get; set; }
			public string Difficulty { get; set; }
			public string NumDashes { get; set; }
			public string DifficultyEagerness { get; set; }
			public string MapLength { get; set; }
			public string ShineLights { get; set; }
			public string Darkness { get; set; }
			public string StrawberryDensity { get; set; }

			public SettingsBuilder() {
				
			}

			/// <summary>
			/// Builds the actual settings object
			/// </summary>
			/// <returns></returns>
			public object Build() {
				if (!RandomizerLoaded) return null;

				object settings = Activator.CreateInstance(t_RandoSettings);
				DynamicData dd_settings = DynamicData.For(settings);
				// Hardcoded setup
				AddValidLevels(dd_settings);
				dd_settings.Set("SeedType", SeedTypeEnum.ToVal("Custom"));
				dd_settings.Set("Rules", "");
				// Stuff that could actually change from one match to another
				dd_settings.Set("Seed", Seed);
				dd_settings.Set("Algorithm", LogicTypeEnum.ToVal(LogicType));
				dd_settings.Set("Difficulty", DifficultyEnum.ToVal(Difficulty));
				dd_settings.Set("Dashes", NumDashesEnum.ToVal(NumDashes));
				dd_settings.Set("DifficultyEagerness", DifficultyEagernessEnum.ToVal(DifficultyEagerness));
				dd_settings.Set("Length", MapLengthEnum.ToVal(MapLength));
				dd_settings.Set("Lights", ShineLightsEnum.ToVal(ShineLights));
				dd_settings.Set("Darkness", DarknessEnum.ToVal(Darkness));
				dd_settings.Set("Strawberriesy", StrawberryDensityEnum.ToVal(StrawberryDensity));
				return settings;
			}

			private void AddValidLevels(DynamicData dd_Settings) {
				dd_Settings.Invoke("EnableMap", new AreaKey(0, AreaMode.Normal));
				dd_Settings.Invoke("EnableMap", new AreaKey(1, AreaMode.Normal));
				dd_Settings.Invoke("EnableMap", new AreaKey(2, AreaMode.Normal));
				dd_Settings.Invoke("EnableMap", new AreaKey(3, AreaMode.Normal));
				dd_Settings.Invoke("EnableMap", new AreaKey(4, AreaMode.Normal));
				dd_Settings.Invoke("EnableMap", new AreaKey(5, AreaMode.Normal));
				dd_Settings.Invoke("EnableMap", new AreaKey(6, AreaMode.Normal));
				dd_Settings.Invoke("EnableMap", new AreaKey(7, AreaMode.Normal));
				dd_Settings.Invoke("EnableMap", new AreaKey(9, AreaMode.Normal));
				dd_Settings.Invoke("EnableMap", new AreaKey(10, AreaMode.Normal));

				dd_Settings.Invoke("EnableMap", new AreaKey(1, AreaMode.BSide));
				dd_Settings.Invoke("EnableMap", new AreaKey(2, AreaMode.BSide));
				dd_Settings.Invoke("EnableMap", new AreaKey(3, AreaMode.BSide));
				dd_Settings.Invoke("EnableMap", new AreaKey(4, AreaMode.BSide));
				dd_Settings.Invoke("EnableMap", new AreaKey(5, AreaMode.BSide));
				dd_Settings.Invoke("EnableMap", new AreaKey(6, AreaMode.BSide));
				dd_Settings.Invoke("EnableMap", new AreaKey(7, AreaMode.BSide));
				dd_Settings.Invoke("EnableMap", new AreaKey(9, AreaMode.BSide));

				dd_Settings.Invoke("EnableMap", new AreaKey(1, AreaMode.CSide));
				dd_Settings.Invoke("EnableMap", new AreaKey(2, AreaMode.CSide));
				dd_Settings.Invoke("EnableMap", new AreaKey(3, AreaMode.CSide));
				dd_Settings.Invoke("EnableMap", new AreaKey(4, AreaMode.CSide));
				dd_Settings.Invoke("EnableMap", new AreaKey(5, AreaMode.CSide));
				dd_Settings.Invoke("EnableMap", new AreaKey(6, AreaMode.CSide));
				dd_Settings.Invoke("EnableMap", new AreaKey(7, AreaMode.CSide));
				dd_Settings.Invoke("EnableMap", new AreaKey(9, AreaMode.CSide));
			}

			internal void RandomizeSeed(int? seedseed = null) {
				Seed = "";
				var ra = seedseed == null ? new Random() : new Random(seedseed.Value);
				for (int i = 0; i < 6; i++) {
					var val = ra.Next(36);
					if (val < 10) {
						Seed += ((char)('0' + val)).ToString();
					}
					else {
						Seed += ((char)('a' + val - 10)).ToString();
					}
				}
			}
		}

		public static IEnumerator<AreaKey?> Begin(object settings) {
			Thread BuilderThread = null;
			DynamicData dd_Settings = DynamicData.For(settings);
			object RandoModuleInstance = null;
			DynamicData dd_RandoModuleInstance = null;

			try {
				RandoModuleInstance = f_RandoModule_Instance.GetValue(null);
				dd_RandoModuleInstance = DynamicData.For(RandoModuleInstance);
			}
			catch (Exception e) {
				yield break;
			}

			AreaKey? newArea = null;

			BuilderThread = new Thread(() => {
				dd_Settings.Invoke("Enforce");
				try {
					newArea = RandoLogic_GenerateMap(settings);
				}
				catch (ThreadAbortException) {
					return;
				}
				catch (Exception e) {
					Engine.Commands.Log($"Failed to generate Randomizer level: {e.InnerException?.Message ?? "Unknown error"}");
					Logger.Log(LogLevel.Error, "Head2Head", $"Failed to generate Randomizer level: {e.InnerException?.Message ?? "Unknown error"}");
					Logger.Log(LogLevel.Error, "Head2Head", e.StackTrace);
					return;
				}
				BuilderThread = null;
			});
			BuilderThread.Start();

			while (newArea == null && BuilderThread.IsAlive) {
				yield return null;
			}
			if (newArea == null) {
				Logger.Log(LogLevel.Error, "Head2Head", $"No Randomizer lever was generated. Launch will not proceed.");
				if (PlayerStatus.Current.IsInMatch(true)) {
					PlayerStatus.Current.CurrentMatch?.PlayerDNF(DNFReason.RandomizerError);
				}
				yield break;
			}
			if (!PlayerStatus.Current.IsInMatch(true)) {
				Logger.Log(LogLevel.Warn, "Head2Head", $"Player is not in a match. Randomizer launch will not proceed.");
				yield break;
			}
			PlayerStatus.Current.RandomizerArea = new GlobalAreaKey(newArea.Value);
			RandoModule_StartMe_Set(newArea);
		}
	}

	public static class RandoIntegrationExtensions {
		public static SettingsBuilder ReadRandoSettings(this CelesteNetBinaryReader r) {
			SettingsBuilder sb = new SettingsBuilder();
			sb.Seed = r.ReadString();
			sb.LogicType = r.ReadString();
			sb.Difficulty = r.ReadString();
			sb.NumDashes = r.ReadString();
			sb.DifficultyEagerness = r.ReadString();
			sb.MapLength = r.ReadString();
			sb.ShineLights = r.ReadString();
			sb.Darkness = r.ReadString();
			sb.StrawberryDensity = r.ReadString();
			return sb;
		}

		public static void Write(this CelesteNetBinaryWriter w, SettingsBuilder settings) {
			w.Write(settings.Seed ?? "");
			w.Write(settings.LogicType ?? "");
			w.Write(settings.Difficulty ?? "");
			w.Write(settings.NumDashes ?? "");
			w.Write(settings.DifficultyEagerness ?? "");
			w.Write(settings.MapLength ?? "");
			w.Write(settings.ShineLights ?? "");
			w.Write(settings.Darkness ?? "");
			w.Write(settings.StrawberryDensity ?? "");
		}
	}
}
