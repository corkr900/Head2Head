﻿using Celeste.Mod.Head2Head.Entities;
using Celeste.Mod.Head2Head.Shared;
using FMOD.Studio;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.UI {
	public class OuiRunSelectILMapList : Oui {
		public List<OuiRunSelectILChapterIcon> OuiIcons;

		private TextMenu menu;

		private const float onScreenX = 960f;
		private const float offScreenX = 2880f;

		private float alpha = 0f;

		private int type = 2;
		private int side = 0;

		private List<TextMenuExt.IItemExt> items = new List<TextMenuExt.IItemExt>();

		private List<string> sets = new List<string>();

		private TextMenu CreateMenu(bool inGame, EventInstance snapshot) {
			menu = new TextMenu();
			menu.CompactWidthMode = true;
			items.Clear();

			menu.Add(new TextMenu.Header(Dialog.Clean("maplist_title")));

			menu.Add(new TextMenu.SubHeader(Dialog.Clean("maplist_filters")));

			sets.Clear();
			foreach (AreaData area in AreaData.Areas) {
				string levelSet = area.LevelSet;
				if (string.IsNullOrEmpty(levelSet))
					continue;
				if (levelSet == "Celeste")
					continue;
				if (sets.Contains(levelSet))
					continue;
				sets.Add(levelSet);
			}

			menu.Add(new TextMenu.Slider(Dialog.Clean("maplist_type"), value => {
				if (value == 0)
					return Dialog.Clean("levelset_celeste");
				if (value == 1)
					return Dialog.Clean("maplist_type_everything");
				if (value == 2)
					return Dialog.Clean("maplist_type_allmods");
				return Dialog.CleanLevelSet(sets[value - 3]);
			}, 0, 2 + sets.Count, type).Change(value => {
				type = value;
				ReloadItems();
			}));

			menu.Add(new TextMenu.Slider(Dialog.Clean("maplist_side"), value => ((char)('A' + value)).ToString(), 0, Enum.GetValues(typeof(AreaMode)).Length - 1, side).Change(value => {
				side = value;
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

			string filterSet = null;
			if (type == 0) {
				filterSet = "Celeste";
			}
			else if (type >= 3) {
				filterSet = sets[type - 3];
			}

			string lastLevelSet = null;
			LevelSetStats levelSetStats = null;
			int levelSetAreaOffset = 0;
			int levelSetUnlockedAreas = int.MaxValue;
			int levelSetUnlockedModes = int.MaxValue;
			string name;

			SaveData save = SaveData.Instance;
			List<AreaStats> areaStatsAll = save.Areas;
			for (int i = 0; i < AreaData.Areas.Count; i++) {
				AreaData area = AreaData.Get(i);
				if (area == null || !area.HasMode((AreaMode)side)) continue;
				if (!save.DebugMode && !string.IsNullOrEmpty(area.Meta?.Parent)) continue;
				string levelSet = area.LevelSet;
				if (type != 1 && ((filterSet == null && levelSet == "Celeste") || (filterSet != null && filterSet != levelSet))) continue;

				name = area.Name;
				name = name.DialogCleanOrNull() ?? name.SpacedPascalCase();

				if (lastLevelSet != levelSet) {
					lastLevelSet = levelSet;
					levelSetStats = SaveData.Instance.GetLevelSetStatsFor(levelSet);
					levelSetAreaOffset = levelSetStats.AreaOffset;
					levelSetUnlockedAreas = levelSetStats.UnlockedAreas;
					levelSetUnlockedModes = levelSetStats.UnlockedModes;
					string setname = Dialog.CleanLevelSet(levelSet);
					TextMenuExt.SubHeaderExt levelSetHeader = new TextMenuExt.SubHeaderExt(setname);
					levelSetHeader.Alpha = 0f;
					menu.Add(levelSetHeader);
					items.Add(levelSetHeader);
				}

				TextMenuExt.ButtonExt button = new TextMenuExt.ButtonExt(name);
				button.Alpha = 0f;

				if (area.Icon != "areas/null")
					button.Icon = area.Icon;
				button.IconWidth = 64f;

				if (levelSet == "Celeste" && i > levelSetAreaOffset + levelSetUnlockedAreas)
					button.Disabled = true;
				if (side == 1 && !areaStatsAll[i].Cassette)
					button.Disabled = true;
				if (side >= 2 && levelSetUnlockedModes < (side + 1))
					button.Disabled = true;

				menu.Add(button.Pressed(() => {
					Inspect(area, (AreaMode)side);
				}));
				items.Add(button);
			}

			((TextMenu)menu).BatchMode = false;

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

		protected void Inspect(AreaData area, AreaMode mode = AreaMode.Normal) {
			GlobalAreaKey areaKey = new GlobalAreaKey(area.ToKey(mode));
			if (areaKey.IsOverworld || !areaKey.IsValidInstalledMap || areaKey.Equals(GlobalAreaKey.Head2HeadLobby)) {
				Audio.Play("event:/ui/main/button_invalid");
				return;
			}
			Focused = false;
			Audio.Play(SFX.ui_world_icon_select);
			RunOptionsILChapter option = OuiRunSelectIL.GetChapterOption(areaKey.SID, ref ILSelector.LastLevelSetIndex, ref ILSelector.LastChapterIndex);
			if (OuiIcons != null && area.ID < OuiIcons.Count) OuiIcons[area.ID].Select();
			string lobby = option.CollabLevelSetForLobby;
			if (string.IsNullOrEmpty(lobby)) {
				// not a collab lobby
				Overworld.Goto<OuiRunSelectILChapterPanel>();
			}
			else {
				// is a collab lobby
				OuiRunSelectIL.GetChapterOption(lobby, ref ILSelector.LastLevelSetIndex, ref ILSelector.LastChapterIndex);
				Overworld.Goto<OuiRunSelectILCollabMapSelect>();
			}
		}

	}
}
