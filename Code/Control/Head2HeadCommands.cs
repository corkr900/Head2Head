using Celeste.Mod.Head2Head.ControlPanel;
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
        //private static Head2HeadModule module { get { return Head2HeadModule.Instance; } }

		//[Command("h2h_matchpass", "give yourself a bta match pass")]
		//internal static void MatchPass(string arg) {
		//	Role.GiveBTAMatchPass();
		//	Head2HeadModule.InvokeMatchUpdatedEvent();
		//}

		/// <summary>
		/// Hide the helpdesk from the UI
		/// </summary>
		/// <param name="arg"></param>
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

		[Command("h2h_websocket", "Resets the head 2 head websocket")]
		public static void Websocket(string arg) {
			arg = arg?.ToLower();
			if (arg == "stop") {
				ControlPanelCore.EndServer();
			}
			else if (arg == "start") {
				ControlPanelCore.TryInitServer();
			}
			else {
				ControlPanelCore.EndServer();
				ControlPanelCore.TryInitServer();
			}
		}

		[Command("h2h_myID", "Display my PlayerID")]
		public static void H2HMyID(string arg) {
			Engine.Commands.Log(PlayerID.MyID?.SerializedID ?? "null");
		}

		[Command("h2h", "IF YOU'RE NOT corkr900, DON'T USE THIS. General use command for whatever i'm debugging.")]
		public static void H2HCMD(string arg) {
			//Ruleset.Current.FindRequiredMods();
		}

	}
}
