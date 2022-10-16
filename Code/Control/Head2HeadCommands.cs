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
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.Control {
	public static class Head2HeadCommands {
        private static Head2HeadModule module { get { return Head2HeadModule.Instance; } }
		private static string setVarHelpText =
			"hudscale: float | hudline: float | opacity1: float | opacity2: float";

		[Command("corkr_test", "this is a test command to do random stuff")]
		internal static void SendTestMsg(string arg) {
			CNetComm.Instance?.SendTestMessage();
		}

		[Command("h2h_add", "Add a phase to the match")]
		internal static void AddPhase(string arg) {
			arg = arg?.ToLower();
			bool useSRCrules = arg?.Contains("_src") ?? false;
			arg = arg?.Replace("_src", "");
			StandardCategory cat;
			switch (arg) {
				default:
					Engine.Commands.Log(string.Format("Did not recognize category: {0}", arg));
					return;
				case null:
				case "":
				case "clear":
				case "complete":
					cat = StandardCategory.Clear;
					break;
				case "fullclear":
				case "fc":
					cat = StandardCategory.FullClear;
					break;
				case "heartandcassette":
				case "heartcassette":
				case "hc":
				case "hac":
					cat = StandardCategory.HeartCassette;
					break;
				case "arb":
				case "berries":
				case "allredberries":
					cat = StandardCategory.ARB;
					break;
				case "cassette":
				case "cas":
					cat = StandardCategory.CassetteGrab;
					break;
			}
			module.AddMatchPhase(cat);
		}

		[Command("h2h_stage", "Stage the built match")]
		internal static void Stage(string arg) {
			module.StageMatch();
		}

		[Command("h2h_join", "Join the match")]
		public static void Join(string arg) {
			module.JoinStagedMatch();
		}

		[Command("h2h_start", "Start the match")]
		public static void Start(string arg) {
			module.BeginStagedMatch();
		}

		[Command("h2h_reset", "Start the match")]
		public static void Reset(string arg) {
			module.ResetCurrentMatch();
		}

		[Command("h2h", "set an arbitrary variable")]
		public static void SetVariable(string id, string val) {
			id = id?.ToLower() ?? "";
			val = val?.ToLower() ?? "";
			float valFloat = float.MinValue;
			bool isFloat = float.TryParse(val, out valFloat);

			switch (id) {
				case "messagecount":
				case "msgcnt":
					Engine.Commands.Log(CNetComm.MessageCounter);
					if (val == "reset") CNetComm.MessageCounter = 0;
					return;
				case "timelimit":
					if (!isFloat) {
						Engine.Commands.Log(string.Format("Error: value {0} is not a number", val));
						return;
					}
					if (valFloat < 1) {
						Engine.Commands.Log("Error: value must be at least 1");
						return;
					}
					Head2HeadModule.Instance.MatchTimeoutMinutes = (int)valFloat;
					Engine.Commands.Log(string.Format("Set Time Limit match time to {0} minutes", (int)valFloat));
					return;
				case "help":
				case "-h":
				default:
					Engine.Commands.Log(setVarHelpText);
					return;
			}
		}
	}
}
