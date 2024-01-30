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
		private List<ChapterIcon> icons = new List<ChapterIcon>();
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
		private int hoveredChapterIdx = 0;

		internal static RunOptionsLevelSet UsingLevelSet;

		private RunOptionsILChapter hoveredOption => UsingLevelSet.Chapters[hoveredChapterIdx];
		private ChapterIcon hoveredIcon  => icons[hoveredChapterIdx];

		public override void Added(Scene scene) {
			base.Added(scene);

			// Add Scarf things
			scarfSegments = new MTexture[scarf.Height / scarfSegmentSize];
			for (int j = 0; j < scarfSegments.Length; j++) {
				scarfSegments[j] = scarf.GetSubtexture(0, j * scarfSegmentSize, scarf.Width, scarfSegmentSize);
			}
			Depth = -20;
			for (int num = icons.Count - 1; num > -1; num--) {
				if (!string.IsNullOrEmpty(icons[num].ChapterOption.Data?.Meta?.Parent)) {
					icons[num].Hide();
				}
			}
		}

		public override IEnumerator Enter(Oui from) {
			if (UsingLevelSet == null) UsingLevelSet = OuiRunSelectIL.UsingRuleset.LevelSets[0];
			Visible = true;
			display = true;
			hoveredChapterIdx = Calc.Clamp(hoveredChapterIdx, 0, UsingLevelSet.Chapters.Count - 1);
			ChapterIcon.CurrentlyHoveredIndex = hoveredChapterIdx;
			levelSetScarf = GFX.Gui.GetOrDefault("areas/" + UsingLevelSet.LevelSet + "/hover", GFX.Gui["areas/hover"]);
			updateScarf();
			// Remove any old icons
			foreach (ChapterIcon icon in icons) {
				icon.RemoveSelf();
			}
			icons.Clear();
			// Add Icons
			for (int i = 0; i < UsingLevelSet.Chapters.Count; i++) {
				var opt = UsingLevelSet.Chapters[i];
				MTexture mTexture = GFX.Gui.GetOrDefault(opt.Icon, GFX.Gui["Head2Head/Categories/Custom"]);
				MTexture back = mTexture;
				ChapterIcon icon = new ChapterIcon(opt, mTexture, back, i);
				icons.Add(icon);
				Scene.Add(icon);
				icon.Position = icon.HiddenPosition;
				icon.Show();
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
			ChapterIcon selected = null;
			if (next is OuiChapterPanel) {
				(selected = icons[hoveredChapterIdx]).Select();
			}
			foreach (ChapterIcon current in icons) {
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
					Overworld.Goto<OuiRunSelectILMapList>();
					return;
				}
				if (Input.QuickRestart.Pressed) {
					Audio.Play("event:/ui/main/button_select");
					Audio.Play("event:/ui/main/whoosh_large_in");
					Overworld.Goto<OuiRunSelectILMapSearch>();
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
			Vector2 vector = new Vector2(960f, -scarf.Height * Ease.CubeInOut(1f - ease));
			for (int i = 0; i < scarfSegments.Length; i++) {
				float wave = Ease.CubeIn(i / (float)scarfSegments.Length);
				float x = wave * (float)Math.Sin(Scene.RawTimeActive * 4f + (scarfSegments.Length - i) * 0.05f) * 4f - wave * 16f;
				scarfSegments[i].DrawJustified(vector + new Vector2(x, i * scarfSegmentSize), new Vector2(0.5f, 0f));
			}
			if (!string.IsNullOrEmpty(hoveredOption.Title)) {
				ActiveFont.DrawOutline(hoveredOption.Title, vector + new Vector2(0, scarfSegments.Length * scarfSegmentSize + 5f),
					new Vector2(0.5f, 0f), Vector2.One * 0.7f,
					Color.White * Ease.CubeOut(maplistEase), 2f, Color.Black * Ease.CubeOut(maplistEase));
			}
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
				Vector2 vector1 = new Vector2(1920f - 64f * Ease.CubeOut(maplistEase), 952f);
				string text = Dialog.CleanLevelSet(UsingLevelSet.LevelSet);
				ActiveFont.DrawOutline(text, vector1, new Vector2(1f, 0.5f), Vector2.One * 0.7f, Color.White * Ease.CubeOut(maplistEase), 2f, Color.Black * Ease.CubeOut(maplistEase));
				Vector2 vector2 = ActiveFont.Measure(text) * 0.7f;
				Input.GuiDirection(new Vector2(0f, -1f)).DrawCentered(vector1 + new Vector2((0f - vector2.X) * 0.5f, (0f - vector2.Y) * 0.5f - 16f), Color.White * Ease.CubeOut(maplistEase), 0.5f);
				Input.GuiDirection(new Vector2(0f, 1f)).DrawCentered(vector1 + new Vector2((0f - vector2.X) * 0.5f, vector2.Y * 0.5f + 16f), Color.White * Ease.CubeOut(maplistEase), 0.5f);
			}
		}

		public void orig_Update() {
			if (Focused) {
				inputDelay -= Engine.DeltaTime;
				if (hoveredOption.Data != null) {
					Input.SetLightbarColor(hoveredOption.Data.TitleBaseColor);
				}
				if (Input.MenuCancel.Pressed) {
					Audio.Play("event:/ui/world_map/chapter/back");
					ILSelector.ChosenCategory = null;
					Overworld.Goto<OuiRunSelectILExit>();
				}
				else if (inputDelay <= 0f) {
					if (Input.MenuLeft.Pressed) {
						if (hoveredChapterIdx > 0) {
							Audio.Play("event:/ui/world_map/icon/roll_left");
							inputDelay = 0.15f;
							hoveredChapterIdx--;
							ChapterIcon.CurrentlyHoveredIndex = hoveredChapterIdx;
							hoveredIcon.Hovered(-1);
						}
					}
					else if (Input.MenuRight.Pressed) {
						if (hoveredChapterIdx < UsingLevelSet.Chapters.Count - 1) {
							Audio.Play("event:/ui/world_map/icon/roll_right");
							inputDelay = 0.15f;
							hoveredChapterIdx++;
							ChapterIcon.CurrentlyHoveredIndex = hoveredChapterIdx;
							hoveredIcon.Hovered(1);
						}
					}
					else if (Input.MenuConfirm.Pressed) {
						Audio.Play("event:/ui/world_map/icon/select");
						string levelSet = hoveredOption.CollabLevelSetForLobby;
						if (string.IsNullOrEmpty(levelSet)) {
							// not a collab lobby
							OuiRunSelectILChapterPanel.UsingChapter = hoveredOption;
							Overworld.Goto<OuiRunSelectILChapterPanel>();
						}
						else {
							// is a collab lobby
							OuiRunSelectILCollabMapSelect.UsingLobby = hoveredOption;
							Overworld.Goto<OuiRunSelectILCollabMapSelect>();
						}
					}
				}
			}
			ease = Calc.Approach(ease, display ? 1f : 0f, Engine.DeltaTime * 3f);
			base.Update();
		}

		private void updateScarf() {
			string text = "areas/" + hoveredOption.Data?.Name?.ToLowerInvariant() ?? "" + "_hover";
			if (!text.Equals(scarf.AtlasPath)) {
				scarf = GFX.Gui.GetOrDefault(text, levelSetScarf);
				scarfSegments = new MTexture[scarf.Height / scarfSegmentSize];
				for (int i = 0; i < scarfSegments.Length; i++) {
					scarfSegments[i] = scarf.GetSubtexture(0, i * scarfSegmentSize, scarf.Width, scarfSegmentSize);
				}
			}
		}
	}
}
