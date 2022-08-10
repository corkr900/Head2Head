using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Celeste.TextMenuExt;

namespace Celeste.Mod.Head2Head.Shared
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
			public MatchDefinition match;
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

		private static void SoftDisable(this ButtonExt btn, TextMenu menu, string newSubtext) {
			btn.TextColor = btn.TextColorDisabled;
			btn.AddDescription(menu, Dialog.Clean(newSubtext));
			DynamicData dd = new DynamicData(btn);
			dd.Set("H2H_SoftDisable", true);
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
			MatchDefinition defdrop = PlayerStatus.Current.CurrentMatch;
			ResultCategory? rescatdrop = defdrop?.GetPlayerResultCat(PlayerID.MyIDSafe);
			if (defdrop == null)
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
				if (def == null) return;
				if (def.State != MatchState.InProgress) return;
				def.State = MatchState.Completed;  // Broadcasts update
				cxt.Refresh(menu);
			});
			if (PlayerStatus.Current.CurrentMatch == null)
				btn.SoftDisable(menu, "Head2Head_menu_helpdesk_forceend_nocurrent");
			else if (PlayerStatus.Current.CurrentMatch.State != MatchState.InProgress)
				btn.SoftDisable(menu, "Head2Head_menu_helpdesk_forceend_notinprogress");
			else btn.AddDescription(menu, Dialog.Clean("Head2Head_menu_helpdesk_forceend_subtext"));

			// Clean
			btn = menu.AddButton("Head2Head_menu_helpdesk_clean", () => {
				// TODO (!!!)
			});
			if (true)
				btn.SoftDisable(menu, "Head2Head_menu_helpdesk_clean_notimplemented");
			else
				item.AddDescription(menu, Dialog.Clean("Head2Head_menu_helpdesk_clean_subtext"));

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
			Head2HeadModule.Instance.PurgeStaleData();
			foreach (MatchDefinition def in Head2HeadModule.knownMatches.Values)
			{
				item = new TextMenu.Button(def.DisplayName).Pressed(() => {
					cxt.match = def;
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

			if (cxt.match == null)
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
				menu.Add(new TextMenu.SubHeader(cxt.match.DisplayName));

				// Back
				item = new TextMenu.Button(Dialog.Clean("Head2Head_menu_back")).Pressed(() => {
					menu.OnCancel();
				});
				menu.Add(item);

				// Stage
				ButtonExt btn = menu.AddButton("Head2Head_menu_match_stage", () => {
					Head2HeadModule.Instance.StageMatch(cxt.match);
					cxt.Close(menu);
				});
				if (cxt.match.State != MatchState.Staged)
					btn.SoftDisable(menu, "Head2Head_menu_match_stage_wrongstatus");
				else if (cxt.match.MatchID == curmatch?.MatchID)
					btn.SoftDisable(menu, "Head2Head_menu_match_stage_alreadycurrent");
				else if (curmatch?.PlayerCanLeaveFreely(PlayerID.MyIDSafe) == false)
					btn.SoftDisable(menu, "Head2Head_menu_match_stage_dropfirst");
				else
					btn.AddDescription(menu, Dialog.Clean("Head2Head_menu_match_stage_subtext"));

				// Join
				btn = menu.AddButton("Head2Head_menu_match_join", () => {
					if (cxt.match.MatchID != PlayerStatus.Current.CurrentMatch?.MatchID)
					{
						Head2HeadModule.Instance.StageMatch(cxt.match);
					}
					Head2HeadModule.Instance.JoinStagedMatch();
					cxt.Close(menu);
				});
				if (cxt.match.State != MatchState.Staged)
					btn.SoftDisable(menu, "Head2Head_menu_match_stage_wrongstatus");
				else if (cxt.match.MatchID == curmatch?.MatchID
					  && !cxt.match.Players.Contains(PlayerID.MyIDSafe))
					btn.SoftDisable(menu, "Head2Head_menu_match_stage_alreadycurrent");
				else if (cxt.match.GetPlayerResultCat(PlayerID.MyIDSafe) >= ResultCategory.Joined)
					btn.SoftDisable(menu, "Head2Head_menu_match_join_alreadyjoined");
				else if (curmatch?.PlayerCanLeaveFreely(PlayerID.MyIDSafe) == false)
					btn.SoftDisable(menu, "Head2Head_menu_match_stage_dropfirst");
				else
					btn.AddDescription(menu, Dialog.Clean("Head2Head_menu_match_join_subtext"));

				// Rejoin
				btn = menu.AddButton("Head2Head_menu_match_rejoin", () => {
					// TODO (!!!)
				});
				if (true)
					btn.SoftDisable(menu, "Head2Head_menu_match_rejoin_notimplemented");
				else
					btn.AddDescription(menu, Dialog.Clean("Head2Head_menu_match_rejoin_subtext"));

				// Forget
				btn = menu.AddButton("Head2Head_menu_match_forget", () => {
					// TODO (!!!)
				});
				if (true)
					btn.SoftDisable(menu, "Head2Head_menu_match_forget_notimplemented");
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
