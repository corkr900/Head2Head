using Celeste.Mod.CelesteNet;
using Celeste.Mod.Head2Head.Entities;
using Celeste.Mod.Head2Head.Integration;
using Celeste.Mod.Head2Head.Shared;
using Celeste.Mod.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.UI {
	class OuiRunSelectILChapterSelect : Oui {

		private List<OuiRunSelectILChapterIcon> icons = new List<OuiRunSelectILChapterIcon>();
		private Dictionary<int, OuiRunSelectILChapterIcon> specialIcons = new Dictionary<int, OuiRunSelectILChapterIcon>();
		private int indexToSnap = -1;
		private const int scarfSegmentSize = 2;
		private MTexture scarf = GFX.Gui["areas/hover"];
		private MTexture[] scarfSegments;
		private float ease;
		private bool display;
		private float inputDelay;
		private MTexture levelSetScarf;
		private float maplistEase;
		private float searchEase;
		private float levelsetEase;
		private string currentLevelSet;

		private int area {
			get {
				var option = OuiRunSelectIL.GetChapterOption(ILSelector.LastLevelSetIndex, ILSelector.LastChapterIndex);
				return option?.Data?.ID ?? 0;
			}
			set {
				OuiRunSelectIL.GetChapterOption(value, ref ILSelector.LastLevelSetIndex, ref ILSelector.LastChapterIndex);
			}
		}

		public override void Added(Scene scene) {
			base.Added(scene);

			int count = AreaData.Areas.Count;
			// Add Normal Icons
			for (int id = 0; id < count; id++) {
				MTexture mTexture = GFX.Gui[AreaData.Areas[id].Icon];
				MTexture back = GFX.Gui.Has(AreaData.Areas[id].Icon + "_back") ? GFX.Gui[AreaData.Areas[id].Icon + "_back"] : mTexture;
				OuiRunSelectILChapterIcon icon = new OuiRunSelectILChapterIcon(id, mTexture, back);
				icons.Add(icon);
				Scene.Add(icon);
			}
			// Add Scarf things
			scarfSegments = new MTexture[scarf.Height / scarfSegmentSize];
			for (int j = 0; j < scarfSegments.Length; j++) {
				scarfSegments[j] = scarf.GetSubtexture(0, j * scarfSegmentSize, scarf.Width, scarfSegmentSize);
			}
			if (indexToSnap >= 0) {
				area = indexToSnap;
				icons[indexToSnap].SnapToSelected();
			}
			Depth = -20;
			for (int num = icons.Count - 1; num > -1; num--) {
				if (!string.IsNullOrEmpty(AreaData.Get(icons[num].Area)?.GetMeta()?.Parent)) {
					icons[num].Area = -1;
					icons[num].Hide();
				}
			}
		}

		public override IEnumerator Enter(Oui from) {
			ILSelector.LastChapterIndex = Calc.Clamp(ILSelector.LastChapterIndex, 0, OuiRunSelectIL.GetNumOptionsInSet(ILSelector.LastLevelSetIndex) - 1);
			Visible = true;
			display = true;
			currentLevelSet = OuiRunSelectIL.LevelSetIdxToSet(ILSelector.LastLevelSetIndex);
			OuiChapterSelectIcon unselected = null;
			if (from is OuiChapterPanel) {
				(unselected = icons[area]).Unselect();
				if (area != area) {
					unselected.Hide();
				}
			}
			levelSetScarf = GFX.Gui.GetOrDefault("areas/" + currentLevelSet + "/hover", GFX.Gui["areas/hover"]);
			updateScarf();
			LevelSetStats stats = null;
			// Process normal icons
			foreach (OuiChapterSelectIcon current in icons) {
				AreaData areaData = AreaData.Get(current.Area);
				if (areaData != null && areaData.GetLevelSet() == currentLevelSet) {
					stats = stats ?? Util.GetSetStats(areaData.LevelSet);
					if (stats == null) continue;
					int id = areaData.ToKey().ID;
					if ((string.IsNullOrEmpty(currentLevelSet) || id <= Math.Max(1, stats.AreaOffset + stats.MaxArea)) && current != unselected) {
						current.Position = current.HiddenPosition;
						current.Show();
						current.AssistModeUnlockable = false;
					}
				}
			}
			// Process special icons
			foreach (OuiChapterSelectIcon icon in specialIcons.Values) {
				icon.RemoveSelf();
			}
			specialIcons.Clear();
			var chapters = OuiRunSelectIL.SelectableLevelSets[ILSelector.LastLevelSetIndex].Chapters;
			// Add Special Icons
			for (int j = 0; j < chapters.Count; j++) {
				var opt = chapters[j];
				if (opt.IsSpecial) {
					MTexture mTexture = GFX.Gui.GetOrDefault(opt.Icon, GFX.Gui["Head2Head/Categories/Custom"]);
					MTexture back = mTexture;
					OuiRunSelectILChapterIcon icon = new OuiRunSelectILChapterIcon(0, mTexture, back, opt.SpecialID);
					specialIcons.Add(opt.SpecialID, icon);
					Scene.Add(icon);
					icon.Position = icon.HiddenPosition;
					icon.Show();
					icon.AssistModeUnlockable = false;
				}
			}
			if (from is OuiChapterPanel) {
				yield return 0.25f;
			}
		}

		public override IEnumerator Leave(Oui next) {
			display = false;
			yield return EaseOut(next);
		}

		private IEnumerator EaseOut(Oui next) {
			OuiChapterSelectIcon selected = null;
			if (next is OuiChapterPanel) {
				var curOption = OuiRunSelectIL.GetChapterOption(ILSelector.LastLevelSetIndex, ILSelector.LastChapterIndex);
				if (curOption.IsSpecial) {
					(selected = specialIcons[curOption.SpecialID]).Select();
				}
				else {
					(selected = icons[area]).Select();
				}
			}
			foreach (OuiChapterSelectIcon current in icons) {
				AreaData areaData = AreaData.Get(current.Area);
				if (areaData != null && !(areaData.GetLevelSet() != currentLevelSet)) {
					if (selected != current) {
						current.Hide();
					}
				}
			}
			foreach (OuiChapterSelectIcon current in specialIcons.Values) {
				if (selected != current) {
					current.Hide();
				}
			}
			Visible = false;
			yield break;
		}

		public override void Update() {
			if (Focused && display) {
				if (Input.Pause.Pressed || Input.ESC.Pressed) {
					Audio.Play("event:/ui/main/button_select");
					Audio.Play("event:/ui/main/whoosh_large_in");
					Overworld.Goto<OuiRunSelectILMapList>().OuiIcons = icons;
					return;
				}
				if (Input.QuickRestart.Pressed) {
					Audio.Play("event:/ui/main/button_select");
					Audio.Play("event:/ui/main/whoosh_large_in");
					Overworld.Goto<OuiRunSelectILMapSearch>().OuiIcons = icons;
					return;
				}
			}
			if (Focused && display && inputDelay <= Engine.DeltaTime) {
				if (Input.MenuUp.Pressed) {
					Audio.Play("event:/ui/world_map/chapter/pane_contract");
					Audio.Play("event:/ui/world_map/icon/roll_left");
					Overworld.Goto<OuiRunSelectILLevelSet>().Direction = -1;
					return;
				}
				if (Input.MenuDown.Pressed) {
					Audio.Play("event:/ui/world_map/chapter/pane_expand");
					Audio.Play("event:/ui/world_map/icon/roll_right");
					Overworld.Goto<OuiRunSelectILLevelSet>().Direction = 1;
					return;
				}
			}
			orig_Update();
			if (Focused && display) {
				updateScarf();
			}
			maplistEase = Calc.Approach(maplistEase, (display && Focused) ? 1f : 0f, Engine.DeltaTime * 4f);
			searchEase = Calc.Approach(searchEase, (display && Focused) ? 1f : 0f, Engine.DeltaTime * 4f);
			levelsetEase = Calc.Approach(levelsetEase, (display && Focused) ? 1f : 0f, Engine.DeltaTime * 4f);
		}

		public override void Render() {
			orig_Render();
			if (maplistEase > 0f) {
				Vector2 position = new Vector2(128f * Ease.CubeOut(maplistEase), 952f);
				GFX.Gui["menu/maplist"].DrawCentered(position, Color.White * Ease.CubeOut(maplistEase));
				(Input.GuiInputController(Input.PrefixMode.Latest) ? Input.GuiButton(Input.Pause, Input.PrefixMode.Latest) : Input.GuiButton(Input.ESC, Input.PrefixMode.Latest)).Draw(position, Vector2.Zero, Color.White * Ease.CubeOut(maplistEase));
			}
			if (searchEase > 0f) {
				Vector2 position2 = new Vector2(128f * Ease.CubeOut(searchEase), 952f);
				position2.Y -= 128f;
				GFX.Gui["menu/mapsearch"].DrawCentered(position2, Color.White * Ease.CubeOut(searchEase));
				Input.GuiKey(Input.FirstKey(Input.QuickRestart)).Draw(position2, Vector2.Zero, Color.White * Ease.CubeOut(searchEase));
			}
			if (levelsetEase > 0f) {
				Vector2 vector = new Vector2(1920f - 64f * Ease.CubeOut(maplistEase), 952f);
				string text = DialogExt.CleanLevelSet(currentLevelSet);
				ActiveFont.DrawOutline(text, vector, new Vector2(1f, 0.5f), Vector2.One * 0.7f, Color.White * Ease.CubeOut(maplistEase), 2f, Color.Black * Ease.CubeOut(maplistEase));
				Vector2 vector2 = ActiveFont.Measure(text) * 0.7f;
				Input.GuiDirection(new Vector2(0f, -1f)).DrawCentered(vector + new Vector2((0f - vector2.X) * 0.5f, (0f - vector2.Y) * 0.5f - 16f), Color.White * Ease.CubeOut(maplistEase), 0.5f);
				Input.GuiDirection(new Vector2(0f, 1f)).DrawCentered(vector + new Vector2((0f - vector2.X) * 0.5f, vector2.Y * 0.5f + 16f), Color.White * Ease.CubeOut(maplistEase), 0.5f);
			}
		}

		public void orig_Update() {
			if (Focused) {
				inputDelay -= Engine.DeltaTime;
				if (area >= 0 && area < AreaData.Areas.Count) {
					Input.SetLightbarColor(AreaData.Get(area).TitleBaseColor);
				}
				if (Input.MenuCancel.Pressed) {
					Audio.Play("event:/ui/world_map/chapter/back");
					if (ILSelector.ActiveSelector != null) {
						ILSelector.ActiveSelector.Area = GlobalAreaKey.Overworld;
						ILSelector.ActiveSelector.Category = StandardCategory.Clear;
					}
					Overworld.Goto<OuiRunSelectILExit>();
				}
				else if (inputDelay <= 0f) {
					if (Input.MenuLeft.Pressed) {
						if (ILSelector.LastChapterIndex > 0) {
							Audio.Play("event:/ui/world_map/icon/roll_left");
							inputDelay = 0.15f;
							ILSelector.LastChapterIndex--;
							var option = OuiRunSelectIL.GetChapterOption(ILSelector.LastLevelSetIndex, ILSelector.LastChapterIndex);
							var icon = option.IsSpecial ? specialIcons[option.SpecialID] : icons[option.Data.ID];
							icon.Hovered(-1);
						}
					}
					else if (Input.MenuRight.Pressed) {
						int numOptions = OuiRunSelectIL.GetNumOptionsInSet(ILSelector.LastLevelSetIndex);
						if (ILSelector.LastChapterIndex < numOptions - 1) {
							Audio.Play("event:/ui/world_map/icon/roll_right");
							inputDelay = 0.15f;
							ILSelector.LastChapterIndex++;
							var option = OuiRunSelectIL.GetChapterOption(ILSelector.LastLevelSetIndex, ILSelector.LastChapterIndex);
							var icon = option.IsSpecial ? specialIcons[option.SpecialID] : icons[option.Data.ID];
							icon.Hovered(1);
						}
					}
					else if (Input.MenuConfirm.Pressed) {
						Audio.Play("event:/ui/world_map/icon/select");
						RunOptionsILChapter option = OuiRunSelectIL.GetChapterOption(ILSelector.LastLevelSetIndex, ILSelector.LastChapterIndex);
						string levelSet = option.CollabLevelSetForLobby;
						if (string.IsNullOrEmpty(levelSet)) {
							// not a collab lobby
							Overworld.Goto<OuiRunSelectILChapterPanel>();
						}
						else {
							// is a collab lobby
							Overworld.Goto<OuiRunSelectILCollabMapSelect>();
						}
					}
				}
			}
			ease = Calc.Approach(ease, display ? 1f : 0f, Engine.DeltaTime * 3f);
			base.Update();
		}

		private void updateScarf() {
			string text = "areas/" + AreaData.Areas[area].Name.ToLowerInvariant() + "_hover";
			if (!text.Equals(scarf.AtlasPath)) {
				scarf = GFX.Gui.GetOrDefault(text, levelSetScarf);
				scarfSegments = new MTexture[scarf.Height / scarfSegmentSize];
				for (int i = 0; i < scarfSegments.Length; i++) {
					scarfSegments[i] = scarf.GetSubtexture(0, i * scarfSegmentSize, scarf.Width, scarfSegmentSize);
				}
			}
		}

		public void orig_Render() {
			Vector2 vector = new Vector2(960f, -scarf.Height * Ease.CubeInOut(1f - ease));
			for (int i = 0; i < scarfSegments.Length; i++) {
				float num = Ease.CubeIn(i / (float)scarfSegments.Length);
				float x = num * (float)Math.Sin(Scene.RawTimeActive * 4f + i * 0.05f) * 4f - num * 16f;
				scarfSegments[i].DrawJustified(vector + new Vector2(x, i * scarfSegmentSize), new Vector2(0.5f, 0f));
			}
		}
	}
}
