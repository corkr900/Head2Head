using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

			public void Close(TextMenu curr)
			{
				if (curr != null) curr.RemoveSelf();
				DynamicData dd = new DynamicData(level);
				dd.Set("unpauseTimer", 0.15f);
				level.Paused = false;
				Audio.Play("event:/ui/game/unpause");
			}
		}

		public static void Helpdesk(HelpdeskMenuContext cxt)
		{
			cxt.level.Paused = true;
			TextMenu menu = new TextMenu();
			menu.AutoScroll = false;
			menu.Position = new Vector2((float)Engine.Width / 2f, (float)Engine.Height / 2f - 100f);
			TextMenu.Item item;

			// Head 2 Head Helpdesk
			menu.Add(new TextMenu.Header(Dialog.Clean("Head2Head_menu_helpdesk")));

			// Browse
			item = new TextMenu.Button(Dialog.Clean("Head2Head_menu_helpdesk_browse")).Pressed(() => {
				cxt.GoTo(BrowseMatches, menu);
			});
			menu.Add(item);
			//item.AddDescription(menu, Dialog.Clean("Head2Head_menu_helpdesk_browse_subtext"));

			// Drop Out
			item = new TextMenu.Button(Dialog.Clean("Head2Head_menu_helpdesk_dropout")).Pressed(() => {
				// TODO (!!!)
			});
			menu.Add(item);
			item.AddDescription(menu, Dialog.Clean("Head2Head_menu_helpdesk_dropout_subtext"));

			// Force End
			item = new TextMenu.Button(Dialog.Clean("Head2Head_menu_helpdesk_forceend")).Pressed(() => {
				MatchDefinition def = PlayerStatus.Current.CurrentMatch;
				if (def == null) return;
				if (def.State != MatchState.InProgress) return;
				def.State = MatchState.Completed;  // Broadcasts update
				cxt.Close(menu);
			});
			menu.Add(item);
			item.AddDescription(menu, Dialog.Clean("Head2Head_menu_helpdesk_forceend_subtext"));

			// Clean
			item = new TextMenu.Button(Dialog.Clean("Head2Head_menu_helpdesk_clean")).Pressed(() => {
				// TODO (!!!)
			});
			menu.Add(item);
			item.AddDescription(menu, Dialog.Clean("Head2Head_menu_helpdesk_clean_subtext"));

			// Cancel
			item = new TextMenu.Button(Dialog.Clean("menu_return_cancel")).Pressed(() => {
				menu.OnCancel();
			});
			menu.Add(item);

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
			else
			{
				// Header
				menu.Add(new TextMenu.Header(cxt.match.DisplayName));

				// Stage
				if (cxt.match.State == MatchState.Staged && cxt.match.MatchID != PlayerStatus.Current.CurrentMatch?.MatchID)
				{
					item = new TextMenu.Button(Dialog.Clean("Head2Head_menu_match_stage")).Pressed(() => {
						// TODO (!!!) un-join the current match if it's staged and I've already joined (or just prevent doing this if joined)
						Head2HeadModule.Instance.StageMatch(cxt.match);
						cxt.Close(menu);
						// TODO (!!!) this should cause the join / start buttons to update somewhere downstream
					});
					menu.Add(item);
					item.AddDescription(menu, Dialog.Clean("Head2Head_menu_match_stage_subtext"));
				}

				// Join
				if (cxt.match.State == MatchState.Staged && cxt.match.GetPlayerResultCat(PlayerID.MyIDSafe) == ResultCategory.NotJoined)
				{
					item = new TextMenu.Button(Dialog.Clean("Head2Head_menu_match_join")).Pressed(() => {
						if (cxt.match.MatchID != PlayerStatus.Current.CurrentMatch?.MatchID)
						{
							Head2HeadModule.Instance.StageMatch(cxt.match);
						}
						Head2HeadModule.Instance.JoinStagedMatch();
						cxt.Close(menu);
						// TODO (!!!) this should cause the join / start buttons to update somewhere downstream
					});
					menu.Add(item);
					item.AddDescription(menu, Dialog.Clean("Head2Head_menu_match_join_subtext"));
				}

				// Rejoin
				// TODO (!!!)

				// Forget
				// TODO (!!!)

				// Back
				item = new TextMenu.Button(Dialog.Clean("Head2Head_menu_back")).Pressed(() => {
					menu.OnCancel();
				});
				menu.Add(item);
			}


			// handle Cancel button
			menu.OnCancel = () => {
				cxt.Back(menu);
			};
			menu.Selection = menu.FirstPossibleSelection;
			cxt.level.Add(menu);
		}
	}
}
