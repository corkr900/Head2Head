﻿using Celeste.Mod.Head2Head.IO;
using Celeste.Mod.Head2Head.Shared;
using Celeste.Mod.Head2Head.UI;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
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
				case "help":
				case "-h":
				default:
					Engine.Commands.Log(setVarHelpText);
					return;
				case "hudscale":
					if (!isFloat) goto case "help";
					H2HHudRenderer.hudScale = valFloat;
					return;
				case "hudline":
					if (!isFloat) goto case "help";
					H2HHudRenderer.lineOffset = valFloat;
					return;
				case "opacity1":
					if (!isFloat) goto case "help";
					H2HHudRenderer.bannerOpacity_beforematch = valFloat;
					return;
				case "opacity2":
					if (!isFloat) goto case "help";
					H2HHudRenderer.bannerOpacity_inmatch = valFloat;
					return;
			}
		}
	}
}
