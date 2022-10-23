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

		private List<OuiChapterSelectIcon> icons = new List<OuiChapterSelectIcon>();

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
				return ILSelector.LastArea.Local_Safe.ID;
			}
			set {
				ILSelector.LastArea = new GlobalAreaKey(value);
			}
		}

		public override void Added(Scene scene) {
			base.Added(scene);

			int count = AreaData.Areas.Count;
			for (int i = 0; i < count; i++) {
				MTexture mTexture = GFX.Gui[AreaData.Areas[i].Icon];
				MTexture back = (GFX.Gui.Has(AreaData.Areas[i].Icon + "_back") ? GFX.Gui[AreaData.Areas[i].Icon + "_back"] : mTexture);
				OuiRunSelectILChapterIcon icon = new OuiRunSelectILChapterIcon(i, mTexture, back);
				icons.Add(icon);
				Scene.Add(icon);
			}
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
			GetMinMaxArea(out int areaOffs, out int areaMax);
			area = Calc.Clamp(area, areaOffs, areaMax);
			Visible = true;
			EaseCamera();
			display = true;
			currentLevelSet = ILSelector.LastArea.Local_Safe.LevelSet;
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
			foreach (OuiChapterSelectIcon current in icons) {
				AreaData areaData = AreaData.Get(current.Area);
				if (areaData != null && areaData.GetLevelSet() == currentLevelSet) {
					stats = stats ?? Util.GetSetStats(areaData.LevelSet);
					int id = areaData.ToKey().ID;
					if ((string.IsNullOrEmpty(currentLevelSet) || id <= Math.Max(1, stats.AreaOffset + stats.MaxArea)) && current != unselected) {
						current.Position = current.HiddenPosition;
						current.Show();
						current.AssistModeUnlockable = false;
					}
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
				(selected = icons[area]).Select();
			}
			foreach (OuiChapterSelectIcon current in icons) {
				AreaData areaData = AreaData.Get(current.Area);
				if (areaData != null && !(areaData.GetLevelSet() != currentLevelSet)) {
					if (selected != current) {
						current.Hide();
					}
				}
			}
			Visible = false;
			yield break;
		}

		public override void Update() {
			if (Focused && display) {
				if (Input.Pause.Pressed || Input.ESC.Pressed) {
					Overworld.Maddy.Hide();
					Audio.Play("event:/ui/main/button_select");
					Audio.Play("event:/ui/main/whoosh_large_in");
					Overworld.Goto<OuiMapList>().OuiIcons = icons;
					return;
				}
				if (Input.QuickRestart.Pressed) {
					Overworld.Maddy.Hide();
					Audio.Play("event:/ui/main/button_select");
					Audio.Play("event:/ui/main/whoosh_large_in");
					Overworld.Goto<OuiMapSearch>().OuiIcons = icons;
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
				GetMinMaxArea(out int areaOffs, out int areaMax);
				if (area < areaOffs) {
					area = areaOffs;
				}
				else {
					if (area > areaMax) {
						area = areaMax;
					}
					while (area > 0 && icons[area].GetIsHidden()) {
						area--;
					}
				}
				if ((Input.MenuLeft.Pressed && (area - 1 < 0 || icons[area - 1].GetIsHidden())) || (Input.MenuRight.Pressed && (area + 1 >= icons.Count || icons[area + 1].GetIsHidden()))) {
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

		private void EaseCamera() {
			AreaData areaData = AreaData.Areas[area];
			base.Overworld.Mountain.EaseCamera(area, areaData.MountainIdle, null, nearTarget: true, areaData.GetMeta()?.Mountain?.Rotate ?? (areaData.GetLevelSet() == "Celeste" && area == 10));
			base.Overworld.Mountain.Model.EaseState(areaData.MountainState);
		}

		private void GetMinMaxArea(out int areaOffs, out int areaMax) {
			LevelSetStats stats = Util.GetSetStats(ILSelector.LastArea.Local_Safe.LevelSet);
			areaOffs = stats.AreaOffset;
			areaMax = areaOffs + stats.MaxArea;
		}

		public void orig_Update() {
			LevelSetStats stats = Util.GetSetStats(currentLevelSet);
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
					Overworld.Maddy.Hide();
				}
				else if (inputDelay <= 0f) {
					if (area > stats.AreaOffset && Input.MenuLeft.Pressed) {
						Audio.Play("event:/ui/world_map/icon/roll_left");
						inputDelay = 0.15f;
						area--;
						icons[area].Hovered(-1);
						EaseCamera();
						Overworld.Maddy.Hide();
					}
					else if (Input.MenuRight.Pressed) {
						if (area < stats.AreaOffset + stats.MaxArea) {
							Audio.Play("event:/ui/world_map/icon/roll_right");
							inputDelay = 0.15f;
							area++;
							icons[area].Hovered(1);
							if (area <= stats.AreaOffset + stats.MaxArea) {
								EaseCamera();
							}
							Overworld.Maddy.Hide();
						}
					}
					else if (Input.MenuConfirm.Pressed) {
						Audio.Play("event:/ui/world_map/icon/select");
						if (string.IsNullOrEmpty(CollabUtils2Integration.GetLobbyLevelSet(ILSelector.LastArea.SID))) {
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
			Vector2 vector = new Vector2(960f, (float)(-scarf.Height) * Ease.CubeInOut(1f - ease));
			for (int i = 0; i < scarfSegments.Length; i++) {
				float num = Ease.CubeIn((float)i / (float)scarfSegments.Length);
				float x = num * (float)Math.Sin(base.Scene.RawTimeActive * 4f + (float)i * 0.05f) * 4f - num * 16f;
				scarfSegments[i].DrawJustified(vector + new Vector2(x, i * scarfSegmentSize), new Vector2(0.5f, 0f));
			}
		}
	}
}
