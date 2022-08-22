using Celeste.Mod.Head2Head.IO;
using Celeste.Mod.Head2Head.Shared;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Celeste.TextMenuExt;

namespace Celeste.Mod.Head2Head.UI
{
	public static class Menus
	{
		public class HelpdeskMenuContext
		{
			public HelpdeskMenuContext(Level level, int returnIndex, bool fromPause)
			{
				this.level = level;
				this.returnToIndex.Push(returnIndex);
				this.fromPauseMenu = fromPause;
			}

			public Level level;
			public bool fromPauseMenu;
			public Stack<int> returnToIndex = new Stack<int>();
			public Stack<Action<HelpdeskMenuContext>> menus = new Stack<Action<HelpdeskMenuContext>>();
			public string matchID;
			public MatchDefinition match {
				get {
					if (string.IsNullOrEmpty(matchID)) return null;
					if (!Head2HeadModule.knownMatches.ContainsKey(matchID)) return null;
					return Head2HeadModule.knownMatches[matchID];
				}
			}
			public PlayerID player;

			public void GoTo(Action<HelpdeskMenuContext> target, TextMenu current)
			{
				Audio.Play("event:/ui/main/button_confirm");  // TODO make sure this is correct
				returnToIndex.Push(current == null ? 0 : current.IndexOf(current.Current));
				menus.Push(target);
				current?.RemoveSelf();
				target.Invoke(this);
			}

			public void Back(TextMenu current)
			{
				Audio.Play("event:/ui/main/button_back");
				current.RemoveSelf();
				menus.Pop();
				int index = returnToIndex.Pop();
				if (menus.Count != 0)
				{
					menus.Peek().Invoke(this);
				}
				else if (fromPauseMenu)
				{
					level.Pause(index, minimal: false);
				}
				else
				{
					Close(null);
				}
			}

			public void Refresh(TextMenu current) {
				current.RemoveSelf();
				menus.Peek().Invoke(this);
			}

			public void Close(TextMenu curr)
			{
				if (curr != null) curr.RemoveSelf();
				DynamicData dd = new DynamicData(level);
				dd.Set("unpauseTimer", 0.15f);
				level.Paused = false;
				Audio.Play("event:/ui/game/unpause");
			}
		}

		#region Helpers

		private static ButtonExt AddButton(this TextMenu menu, string labelDesc, Action onPress) {
			ButtonExt btn = new ButtonExt(Dialog.Clean(labelDesc));
			DynamicData dd = new DynamicData(btn);
			dd.Set("H2H_SoftDisable", false);
			btn.Pressed(() => {
				DynamicData dd2 = new DynamicData(btn);
				if (dd2.Get<bool>("H2H_SoftDisable")) return;
				onPress();
			});
			menu.Add(btn);
			return btn;
		}

		private static void SoftDisable(this ButtonExt btn, TextMenu menu, string newSubtext, params string[] fmtArgs) {
			btn.TextColor = btn.TextColorDisabled;
			btn.AddDescription(menu, string.Format(GetDialogWithLineBreaks(newSubtext), fmtArgs));
			DynamicData dd = new DynamicData(btn);
			dd.Set("H2H_SoftDisable", true);
		}

		private static string GetDialogWithLineBreaks(string key) {
			return Dialog.Get(key).Replace("{n}", "\n").Replace("{break}", "\n");
		}

		#endregion

		#region Actual Menus

		public static void Helpdesk(HelpdeskMenuContext cxt)
		{
			cxt.level.Paused = true;
			TextMenu menu = new TextMenu();
			menu.AutoScroll = false;
			menu.Position = new Vector2((float)Engine.Width / 2f, (float)Engine.Height / 2f - 100f);
			TextMenu.Item item;
			ButtonExt btn;
			MatchDefinition def_menu = PlayerStatus.Current.CurrentMatch;

			// Head 2 Head Helpdesk
			menu.Add(new TextMenu.Header(Dialog.Clean("Head2Head_menu_helpdesk")));

			// Back
			item = new TextMenu.Button(Dialog.Clean("Head2Head_menu_back")).Pressed(() => {
				menu.OnCancel();
			});
			menu.Add(item);

			// Browse
			item = new TextMenu.Button(Dialog.Clean("Head2Head_menu_helpdesk_browse")).Pressed(() => {
				cxt.GoTo(BrowseMatches, menu);
			});
			menu.Add(item);
			//item.AddDescription(menu, Dialog.Clean("Head2Head_menu_helpdesk_browse_subtext"));

			// Drop Out
			btn = menu.AddButton("Head2Head_menu_helpdesk_dropout", () => {
				MatchDefinition def = PlayerStatus.Current.CurrentMatch;
				if (def == null) return;
				ResultCategory cat = def.GetPlayerResultCat(PlayerID.MyIDSafe);
				if (cat == ResultCategory.NotJoined
					|| cat == ResultCategory.Completed
					|| cat == ResultCategory.DNF) return;
				def.PlayerDNF();
				cxt.Refresh(menu);
			});
			ResultCategory? rescatdrop = def_menu?.GetPlayerResultCat(PlayerID.MyIDSafe);
			if (def_menu == null)
				btn.SoftDisable(menu, "Head2Head_menu_helpdesk_forceend_nocurrent");
			else if (rescatdrop.Value == ResultCategory.NotJoined)
				btn.SoftDisable(menu, "Head2Head_menu_helpdesk_dropout_notjoined");
			else if (rescatdrop == ResultCategory.Completed || rescatdrop == ResultCategory.DNF)
				btn.SoftDisable(menu, "Head2Head_menu_helpdesk_dropout_completed");
			else
				btn.AddDescription(menu, Dialog.Clean("Head2Head_menu_helpdesk_dropout_subtext"));

			// Force End
			btn = menu.AddButton("Head2Head_menu_helpdesk_forceend", () => {
				MatchDefinition def = PlayerStatus.Current.CurrentMatch;
				if (def != null && def.State == MatchState.InProgress) {
					def.State = MatchState.Completed;  // Broadcasts update
				}
				cxt.Refresh(menu);
			});
			if (def_menu == null)
				btn.SoftDisable(menu, "Head2Head_menu_helpdesk_forceend_nocurrent");
			else if (def_menu.State != MatchState.InProgress)
				btn.SoftDisable(menu, "Head2Head_menu_helpdesk_forceend_notinprogress");
			else btn.AddDescription(menu, Dialog.Clean("Head2Head_menu_helpdesk_forceend_subtext"));

			// Clean
			btn = menu.AddButton("Head2Head_menu_helpdesk_clean", () => {
				Head2HeadModule.Instance.PurgeAllData();
				CNetComm.Instance.SendScanRequest(false);
				cxt.Close(menu);
			});
			btn.AddDescription(menu, Dialog.Clean("Head2Head_menu_helpdesk_clean_subtext"));

			// Scan & Rejoin
			if (!Head2HeadModule.Instance.PlayerCompletedARoom && def_menu == null) {
				btn = menu.AddButton("Head2Head_menu_helpdesk_rejoin", () => {
					CNetComm.Instance.SendScanRequest(true);
					cxt.Close(menu);
				});
				btn.AddDescription(menu, Dialog.Clean("Head2Head_menu_helpdesk_rejoin_subtext"));
			}

			// Return to Lobby
			if (!PlayerStatus.Current.CurrentArea.Equals(GlobalAreaKey.Head2HeadLobby)) {
				btn = menu.AddButton("Head2Head_menu_helpdesk_returntolobby", () => {
					cxt.Close(menu);
					new FadeWipe(menu.Scene, false, () => {
						LevelEnter.Go(new Session(GlobalAreaKey.Head2HeadLobby.Local.Value), false);
					});
				});
			}

			// handle Cancel button
			menu.OnCancel = () => {
				cxt.Back(menu);
			};
			menu.Selection = menu.FirstPossibleSelection;
			cxt.level.Add(menu);
		}

		public static void BrowseMatches(HelpdeskMenuContext cxt)
		{
			cxt.level.Paused = true;
			TextMenu menu = new TextMenu();
			menu.AutoScroll = false;
			menu.Position = new Vector2((float)Engine.Width / 2f, (float)Engine.Height / 2f - 100f);
			TextMenu.Item item;

			// Head 2 Head Helpdesk
			menu.Add(new TextMenu.Header(Dialog.Clean("Head2Head_menu_helpdesk_browse")));

			// Back
			item = new TextMenu.Button(Dialog.Clean("Head2Head_menu_back")).Pressed(() => {
				menu.OnCancel();
			});
			menu.Add(item);

			// Loop over known matches
			Head2HeadModule.Instance.DiscardStaleData();
			foreach (MatchDefinition def in Head2HeadModule.knownMatches.Values)
			{
				item = new TextMenu.Button(def.DisplayName).Pressed(() => {
					cxt.matchID = def.MatchID;
					cxt.GoTo(KnownMatchMenu, menu);
				});
				menu.Add(item);
				string desc = string.Format(Dialog.Get("Head2Head_menu_browsematchdescription"),
					def.Owner.Name, def.Players.Count, Util.TranslatedMatchState(def.State));
				item.AddDescription(menu, desc);
			}

			// handle Cancel button
			menu.OnCancel = () => {
				cxt.Back(menu);
			};
			menu.Selection = menu.FirstPossibleSelection;
			cxt.level.Add(menu);
		}

		public static void KnownMatchMenu(HelpdeskMenuContext cxt)
		{
			cxt.level.Paused = true;
			TextMenu menu = new TextMenu();
			menu.AutoScroll = false;
			menu.Position = new Vector2((float)Engine.Width / 2f, (float)Engine.Height / 2f - 100f);
			TextMenu.Item item;
			MatchDefinition cxtMatch = cxt.match;

			if (cxtMatch == null)
			{
				// Header
				menu.Add(new TextMenu.Header(Dialog.Clean("Head2Head_menu_match_invalidmatch")));

				// Back
				item = new TextMenu.Button(Dialog.Clean("Head2Head_menu_back")).Pressed(() => {
					menu.OnCancel();
				});
				menu.Add(item);
			}
			else {
				MatchDefinition curmatch = PlayerStatus.Current.CurrentMatch;

				// Header
				menu.Add(new TextMenu.SubHeader(cxtMatch.DisplayName));

				// Back
				item = new TextMenu.Button(Dialog.Clean("Head2Head_menu_back")).Pressed(() => {
					menu.OnCancel();
				});
				menu.Add(item);

				// Stage
				ButtonExt btn = menu.AddButton("Head2Head_menu_match_stage", () => {
					Head2HeadModule.Instance.StageMatch(cxtMatch);
					cxt.Close(menu);
				});
				GlobalAreaKey? vchk = cxtMatch.VersionCheck();
				/*if (cxtMatch.State != MatchState.Staged)
					btn.SoftDisable(menu, "Head2Head_menu_match_stage_wrongstatus");
				else*/ if (cxtMatch.MatchID == curmatch?.MatchID)
					btn.SoftDisable(menu, "Head2Head_menu_match_stage_alreadycurrent");
				else if (vchk != null)
					btn.SoftDisable(menu, "Head2Head_menu_match_join_versionmismatch", vchk?.LocalVersion.ToString(), vchk?.Version.ToString());
				else if (curmatch?.PlayerCanLeaveFreely(PlayerID.MyIDSafe) == false)
					btn.SoftDisable(menu, "Head2Head_menu_match_stage_dropfirst");
				else
					btn.AddDescription(menu, Dialog.Clean("Head2Head_menu_match_stage_subtext"));

				// Join
				btn = menu.AddButton("Head2Head_menu_match_join", () => {
					if (cxtMatch.MatchID != curmatch?.MatchID)
					{
						Head2HeadModule.Instance.StageMatch(cxtMatch);
					}
					Head2HeadModule.Instance.JoinStagedMatch();
					cxt.Close(menu);
				});
				if (cxtMatch.State != MatchState.Staged)
					btn.SoftDisable(menu, "Head2Head_menu_match_stage_wrongstatus");
				else if (cxtMatch.GetPlayerResultCat(PlayerID.MyIDSafe) >= ResultCategory.Joined)
					btn.SoftDisable(menu, "Head2Head_menu_match_join_alreadyjoined");
				else if (curmatch?.PlayerCanLeaveFreely(PlayerID.MyIDSafe) == false)
					btn.SoftDisable(menu, "Head2Head_menu_match_stage_dropfirst");
				else
					btn.AddDescription(menu, Dialog.Clean("Head2Head_menu_match_join_subtext"));

				// Export Log
				if (ActionLogger.LogFileExists(cxtMatch.MatchID)) {
					btn = menu.AddButton("Head2Head_menu_match_export", () => {
						ActionLogger.Export(cxtMatch.MatchID);
						cxt.Close(menu);
					});
					btn.AddDescription(menu, Dialog.Clean("Head2Head_menu_match_export_subtext"));
				}

				// Forget
				btn = menu.AddButton("Head2Head_menu_match_forget", () => {
					if (cxtMatch.MatchID == PlayerStatus.Current.CurrentMatchID) {
						ResultCategory cat2 = cxtMatch.GetPlayerResultCat(PlayerID.MyIDSafe);
						if (cat2 == ResultCategory.Joined || cat2 == ResultCategory.InMatch) return;
						PlayerStatus.Current.CurrentMatch = null;
						PlayerStatus.Current.Updated();
					}
					Head2HeadModule.knownMatches.Remove(cxt.matchID);
					cxt.Back(menu);
				});
				ResultCategory cat = cxtMatch.GetPlayerResultCat(PlayerID.MyIDSafe);
				if (cxtMatch.MatchID == PlayerStatus.Current.CurrentMatchID
					&& (cat == ResultCategory.Joined || cat == ResultCategory.InMatch))
					btn.SoftDisable(menu, "Head2Head_menu_match_forget_current");
				else
					btn.AddDescription(menu, Dialog.Clean("Head2Head_menu_match_forget_subtext"));
			}

			// handle Cancel button
			menu.OnCancel = () => {
				cxt.Back(menu);
			};
			menu.Selection = menu.FirstPossibleSelection;
			cxt.level.Add(menu);
		}

		#endregion
	}
}
