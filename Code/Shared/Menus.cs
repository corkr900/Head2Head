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
		public static void Helpdesk(Level level, int returnIndex, bool fromPauseMenu)
		{
			level.Paused = true;
			TextMenu menu = new TextMenu();
			menu.AutoScroll = false;
			menu.Position = new Vector2((float)Engine.Width / 2f, (float)Engine.Height / 2f - 100f);

			TextMenu.Item item;

			// Head 2 Head Helpdesk
			menu.Add(new TextMenu.Header(Dialog.Clean("Head2Head_menu_helpdesk")));

			// Browse
			item = new TextMenu.Button(Dialog.Clean("Head2Head_menu_helpdesk_browse")).Pressed(() => {
				// TODO (!!!)
			});
			menu.Add(item);
			//item.AddDescription(menu, Dialog.Clean("Head2Head_menu_helpdesk_browse_subtext"));

			// Rejoin
			item = new TextMenu.Button(Dialog.Clean("Head2Head_menu_helpdesk_rejoin")).Pressed(() => {
				// TODO (!!!)
			});
			menu.Add(item);
			item.AddDescription(menu, Dialog.Clean("Head2Head_menu_helpdesk_rejoin_subtext"));

			// Drop Out
			item = new TextMenu.Button(Dialog.Clean("Head2Head_menu_helpdesk_dropout")).Pressed(() => {
				// TODO (!!!)
			});
			menu.Add(item);
			item.AddDescription(menu, Dialog.Clean("Head2Head_menu_helpdesk_dropout_subtext"));

			// Force End
			item = new TextMenu.Button(Dialog.Clean("Head2Head_menu_helpdesk_forceend")).Pressed(() => {
				// TODO (!!!)
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
				Audio.Play("event:/ui/main/button_back");
				menu.RemoveSelf();
				if (fromPauseMenu)
				{
					level.Pause(returnIndex, minimal: false);
				}
				else
				{
					DynamicData dd = new DynamicData(level);
					dd.Set("unpauseTimer", 0.15f);
					level.Paused = false;
					Audio.Play("event:/ui/game/unpause");
				}
			};
			menu.Selection = menu.FirstPossibleSelection;
			level.Add(menu);
		}
	}
}
