using Celeste.Mod.Head2Head.Shared;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Celeste.Mod.Head2Head.UI {
	public class OuiRunSelectILMapList : Oui {
		private TextMenu menu;

		private const float onScreenX = 960f;
		private const float offScreenX = 2880f;

		private float alpha = 0f;
		private int type = -1;

		private List<TextMenuExt.IItemExt> items = new List<TextMenuExt.IItemExt>();

		private TextMenu CreateMenu(bool inGame, FMOD.Studio.EventInstance snapshot) {
			menu = new TextMenu();
			menu.CompactWidthMode = true;
			items.Clear();

			menu.Add(new TextMenu.Header(Dialog.Clean("maplist_title")));

			menu.Add(new TextMenu.SubHeader(Dialog.Clean("maplist_filters")));

			menu.Add(new TextMenu.Slider(Dialog.Clean("maplist_type"), value => {
				return value < 0
					? Dialog.Clean("maplist_type_everything")
					: Dialog.CleanLevelSet(OuiRunSelectIL.UsingRuleset.LevelSets[value].LevelSet);
			}, -1, OuiRunSelectIL.UsingRuleset.LevelSets.Count - 1, type).Change(value => {
				type = value;
				ReloadItems();
			}));

			menu.Add(new TextMenu.Button(Dialog.Clean("maplist_search")).Pressed(() => {
				Overworld.Goto<OuiRunSelectILMapSearch>();
			}));

			menu.Add(new TextMenu.SubHeader(Dialog.Clean("maplist_list")));

			ReloadItems();

			return menu;
		}

		private void ReloadItems() {
			menu.BatchMode = true;

			foreach (TextMenu.Item item in items.Cast<TextMenu.Item>()) {
				menu.Remove(item);
			}
			items.Clear();
			if (type < 0) {
				ReloadItems(OuiRunSelectIL.UsingRuleset.LevelSets[type]);
			}
			else {
				foreach (RunOptionsLevelSet set in OuiRunSelectIL.UsingRuleset.LevelSets) {
					ReloadItems(OuiRunSelectIL.UsingRuleset.LevelSets[type]);
				}
			}
			menu.BatchMode = false;

			// compute a delay so that options don't take more than a second to show up if many mods are installed.
			float delayBetweenOptions = 0.03f;
			if (items.Count > 0)
				delayBetweenOptions = Math.Min(0.03f, 1f / items.Count);

			// Do this afterwards as the menu has now properly updated its size.
			for (int i = 0; i < items.Count; i++)
				Add(new Coroutine(FadeIn(i, delayBetweenOptions, items[i])));

			if (menu.Height > menu.ScrollableMinSize) {
				menu.Position.Y = menu.ScrollTargetY;
			}
		}

		private void ReloadItems(RunOptionsLevelSet levelSet) {
			if (levelSet.LevelSet == "Head2Head") return;

			string setname = Util.TranslatedIfAvailable(levelSet.LevelSet);
			TextMenuExt.SubHeaderExt levelSetHeader = new TextMenuExt.SubHeaderExt(setname);
			levelSetHeader.Alpha = 0f;
			menu.Add(levelSetHeader);
			items.Add(levelSetHeader);
			
			foreach (RunOptionsILChapter chapter in levelSet.Chapters) {
				TextMenuExt.ButtonExt button = new TextMenuExt.ButtonExt(chapter.DisplayName);
				button.Alpha = 0f;
				button.Icon = chapter.IconSafe;
				button.IconWidth = 64f;
				menu.Add(button.Pressed(() => {
					Inspect(levelSet, chapter);
				}));
				items.Add(button);
			}
		}

		private IEnumerator FadeIn(int i, float delayBetweenOptions, TextMenuExt.IItemExt item) {
			yield return delayBetweenOptions * i;
			float ease = 0f;
			Vector2 offset = item.Offset;

			for (float p = 0f; p < 1f; p += Engine.DeltaTime * 4f) {
				ease = Ease.CubeOut(p);
				item.Alpha = ease;
				item.Offset = offset + new Vector2(0f, 64f * (1f - ease));
				yield return null;
			}

			item.Alpha = 1f;
			item.Offset = offset;
		}

		private void ReloadMenu() {
			Vector2 position = Vector2.Zero;

			int selected = -1;
			if (menu != null) {
				position = menu.Position;
				selected = menu.Selection;
				Scene.Remove(menu);
			}

			menu = CreateMenu(false, null);

			if (selected >= 0) {
				menu.Selection = selected;
				menu.Position = position;
			}
			IEnumerable<TextMenu> menus = Scene.Entities.OfType<TextMenu>();
			Scene.Remove(menus);
			Scene.Add(menu);
		}

		public override IEnumerator Enter(Oui from) {

			ReloadMenu();

			menu.Visible = (Visible = true);
			menu.Focused = false;

			for (float p = 0f; p < 1f; p += Engine.DeltaTime * 4f) {
				menu.X = offScreenX + -1920f * Ease.CubeOut(p);
				alpha = Ease.CubeOut(p);
				yield return null;
			}

			menu.Focused = true;
		}

		public override IEnumerator Leave(Oui next) {

			menu.Focused = false;

			Audio.Play(SFX.ui_main_whoosh_large_out);

			for (float p = 0f; p < 1f; p += Engine.DeltaTime * 4f) {
				menu.X = onScreenX + 1920f * Ease.CubeIn(p);
				alpha = 1f - Ease.CubeIn(p);
				yield return null;
			}

			menu.Visible = Visible = false;
			menu.RemoveSelf();
			menu = null;
		}

		public override void Update() {

			if (menu != null && menu.Focused && Selected) {
				if (Input.MenuCancel.Pressed || Input.Pause.Pressed || Input.ESC.Pressed) {
					Audio.Play(SFX.ui_main_button_back);
					Overworld.Goto<OuiRunSelectILChapterSelect>();
				}
			}
			base.Update();
		}

		public override void Render() {
			if (alpha > 0f)
				Draw.Rect(-10f, -10f, 1940f, 1100f, Color.Black * alpha * 0.4f);

			base.Render();
		}

		protected void Inspect(RunOptionsLevelSet set, RunOptionsILChapter chapter) {
			OuiRunSelectILChapterSelect.UsingLevelSet = set;
			string levelSetForLobby = chapter.CollabLevelSetForLobby;
			if (string.IsNullOrEmpty(levelSetForLobby)) {
				// not a collab lobby
				OuiRunSelectILChapterPanel.UsingChapter = chapter;
				Overworld.Goto<OuiRunSelectILChapterPanel>();
			}
			else {
				// is a collab lobby
				OuiRunSelectILCollabMapSelect.UsingLobby = chapter;
				Overworld.Goto<OuiRunSelectILCollabMapSelect>();
			}
		}

	}
}
