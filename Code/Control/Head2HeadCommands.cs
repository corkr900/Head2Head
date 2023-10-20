using Celeste.Mod.Head2Head.Integration;
using Celeste.Mod.Head2Head.IO;
using Celeste.Mod.Head2Head.Shared;
using Celeste.Mod.Head2Head.UI;
using Celeste.Mod.Meta;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.Control {
	public static class Head2HeadCommands {
        private static Head2HeadModule module { get { return Head2HeadModule.Instance; } }

		//[Command("h2h_matchpass", "give yourself a bta match pass")]
		//internal static void MatchPass(string arg) {
		//	Role.GiveBTAMatchPass();
		//	Head2HeadModule.InvokeMatchUpdatedEvent();
		//}

		//[Command("h2h_add", "Add a phase to the match")]
		//internal static void AddPhase(string arg) {
		//	arg = arg?.ToLower();
		//	bool useSRCrules = arg?.Contains("_src") ?? false;
		//	arg = arg?.Replace("_src", "");
		//	StandardCategory cat;
		//	switch (arg) {
		//		default:
		//			Engine.Commands.Log(string.Format("Did not recognize category: {0}", arg));
		//			return;
		//		case null:
		//		case "":
		//		case "clear":
		//		case "complete":
		//			cat = StandardCategory.Clear;
		//			break;
		//		case "fullclear":
		//		case "fc":
		//			cat = StandardCategory.FullClear;
		//			break;
		//		case "heartandcassette":
		//		case "heartcassette":
		//		case "hc":
		//		case "hac":
		//			cat = StandardCategory.HeartCassette;
		//			break;
		//		case "arb":
		//		case "berries":
		//		case "allredberries":
		//			cat = StandardCategory.ARB;
		//			break;
		//		case "cassette":
		//		case "cas":
		//			cat = StandardCategory.CassetteGrab;
		//			break;
		//	}
		//	module.AddMatchPhase(cat);
		//}

		//[Command("h2h_stage", "Stage the built match")]
		//internal static void Stage(string arg) {
		//	module.StageMatch();
		//}

		//[Command("h2h_join", "Join the match")]
		//public static void Join(string arg) {
		//	module.JoinStagedMatch();
		//}

		//[Command("h2h_start", "Start the match")]
		//public static void Start(string arg) {
		//	module.BeginStagedMatch();
		//}

		//[Command("h2h_reset", "Start the match")]
		//public static void Reset(string arg) {
		//	module.ResetCurrentMatch();
		//}

		//[Command("h2h", "set an arbitrary variable")]
		//public static void SetVariable(string id, string val) {
		//	id = id?.ToLower() ?? "";
		//	val = val?.ToLower() ?? "";
		//	float valFloat = float.MinValue;
		//	bool isFloat = float.TryParse(val, out valFloat);

		//	switch (id) {
		//		case "messagecount":
		//		case "msgcnt":
		//			Engine.Commands.Log(CNetComm.MessageCounter);
		//			if (val == "reset") CNetComm.MessageCounter = 0;
		//			return;
		//		case "timelimit":
		//			if (!isFloat) {
		//				Engine.Commands.Log(string.Format("Error: value {0} is not a number", val));
		//				return;
		//			}
		//			if (valFloat < 1) {
		//				Engine.Commands.Log("Error: value must be at least 1");
		//				return;
		//			}
		//			Head2HeadModule.Instance.MatchTimeoutMinutes = (int)valFloat;
		//			Engine.Commands.Log(string.Format("Set Time Limit match time to {0} minutes", (int)valFloat));
		//			return;
		//		case "help":
		//		case "-h":
		//		default:
		//			Engine.Commands.Log("unknown argument");
		//			return;
		//	}
		//}

		//[Command("h2h_build", "test function to build and stage a full-game match")]
		//public static void BuildFullGame() {
		//	MatchDefinition def = StandardMatches.FullGameAnyPct();
		//	Head2HeadModule.Instance.buildingMatch = def;
		//	Head2HeadModule.Instance.StageMatch();
		//	Engine.Commands.Log("Done");
		//}

		[Command("h2h_rando", "buh")]
		public static void Rando(string arg) {
			Thread BuilderThread = null;
			object Settings;
			DynamicData dd_Settings = null;
			Func<object, AreaKey> RandoLogic_GenerateMap = null;
			object RandoModuleInstance = null;
			DynamicData dd_RandoModuleInstance = null;
			Action<AreaKey?> RandoModule_StartMe_Set = null;
			Func<AreaKey?> RandoModule_StartMe_Get = null;
			Action<object, object> RandoModule_SavedData_SavedSettings_Set = null;

			try {
				Type t_RandoSettings = Type.GetType("Celeste.Mod.Randomizer.RandoSettings,Randomizer");
				Settings = Activator.CreateInstance(t_RandoSettings);
				dd_Settings = DynamicData.For(Settings);

				Type t_RandoLogic = Type.GetType("Celeste.Mod.Randomizer.RandoLogic,Randomizer");

				MethodInfo m_RandoLogic_GenerateMap = t_RandoLogic.GetMethod("GenerateMap", BindingFlags.Static | BindingFlags.Public);
				RandoLogic_GenerateMap = (object settings)
					=> (AreaKey)m_RandoLogic_GenerateMap.Invoke(null, new object[] { settings });
				
				Type t_RandoModule = Type.GetType("Celeste.Mod.Randomizer.RandoModule,Randomizer");
				FieldInfo f_RandoModule_Instance = t_RandoModule.GetField("Instance", BindingFlags.Static | BindingFlags.Public);
				RandoModuleInstance = f_RandoModule_Instance.GetValue(null);
				dd_RandoModuleInstance = DynamicData.For(RandoModuleInstance);
				PropertyInfo p_RandoModule_SavedData = t_RandoModule.GetProperty("SavedData", BindingFlags.Instance | BindingFlags.Public);
				Type t_RandoModuleSettings = p_RandoModule_SavedData.PropertyType;
				PropertyInfo p_RandoModuleSettings_SavedSettings = t_RandoModuleSettings.GetProperty("SavedSettings", BindingFlags.Instance | BindingFlags.Public);
				RandoModule_SavedData_SavedSettings_Set = (object randoModuleInstance, object settings) => {
					object SavedData = p_RandoModule_SavedData.GetValue(randoModuleInstance);
					p_RandoModuleSettings_SavedSettings.SetValue(SavedData, settings);
				};
				FieldInfo f_RandoModule_StartMe = t_RandoModule.GetField("StartMe", BindingFlags.Static | BindingFlags.Public);
				RandoModule_StartMe_Set = (AreaKey? key) => {
					f_RandoModule_StartMe.SetValue(null, key);
				};
				RandoModule_StartMe_Get = () => {
					return (AreaKey?)f_RandoModule_StartMe.GetValue(null);
				};
			}
			catch (Exception e) {
				return;
			}

			dd_Settings.Invoke("EnableMap", new AreaKey(0, AreaMode.Normal));
			dd_Settings.Invoke("EnableMap", new AreaKey(1, AreaMode.Normal));
			dd_Settings.Invoke("EnableMap", new AreaKey(2, AreaMode.Normal));
			dd_Settings.Invoke("EnableMap", new AreaKey(3, AreaMode.Normal));
			dd_Settings.Invoke("EnableMap", new AreaKey(4, AreaMode.Normal));
			dd_Settings.Invoke("EnableMap", new AreaKey(5, AreaMode.Normal));
			dd_Settings.Invoke("EnableMap", new AreaKey(6, AreaMode.Normal));
			dd_Settings.Invoke("EnableMap", new AreaKey(7, AreaMode.Normal));
			//dd_Settings.Invoke("EnableMap", new AreaKey(8, AreaMode.Normal));
			dd_Settings.Invoke("EnableMap", new AreaKey(9, AreaMode.Normal));
			dd_Settings.Invoke("EnableMap", new AreaKey(10, AreaMode.Normal));

			BuilderThread = new Thread(() => {
				//Settings.Enforce();
				dd_Settings.Invoke("Enforce");

				AreaKey newArea;
				try {
					newArea = RandoLogic_GenerateMap(Settings);
				}
				catch (ThreadAbortException) {
					return;
				}
				//catch (GenerationError e) {
				//	//errortext.Title = e.Message;
				//	//errortext.FadeVisible = true;
				//	//reenableMenu();
				//	return;
				//}
				catch (Exception e) {
					//errortext.Title = "Encountered an error - Check log.txt for details";
					//Logger.LogDetailed(e, "randomizer");
					//errortext.FadeVisible = true;
					//reenableMenu();
					Engine.Commands.Log(e.InnerException?.Message);
					return;
				}

				// save settings
				//RandoModule.Instance.SavedData.SavedSettings = Settings.Copy();
				RandoModule_SavedData_SavedSettings_Set(RandoModuleInstance, dd_Settings.Invoke("Copy"));
				//RandoModule.Instance.SaveSettings();
				dd_RandoModuleInstance.Invoke("SaveSettings");

				//RandoModule.StartMe = newArea;
				RandoModule_StartMe_Set(newArea);

				//while (RandoModule.StartMe != null) {
				while (RandoModule_StartMe_Get() != null) {
					Thread.Sleep(10);
				}
				BuilderThread = null;
			});
			BuilderThread.Start();
		}

		[Command("h2h_hidehelpdesk", "Toggles the helpdesk pause menu option. Pass 1 as the first argument to hide, 0 to show, or nothing to toggle.")]
		public static void SetHelpdeskPauseVisible(string arg) {
			if (Head2HeadModule.Settings == null) {
				Engine.Commands.Log("Cannot set: settings is null");
				return;
			}
			bool newState = arg == "1" ? true : arg == "0" ? false : !Head2HeadModule.Settings.HidePauseMenuHelpdesk;
			Head2HeadModule.Settings.HidePauseMenuHelpdesk = newState;
			Engine.Commands.Log($"Set HidePauseMenuHelpdesk to {newState}");
		}
	}
}
