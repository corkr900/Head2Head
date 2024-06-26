﻿using Celeste.Mod.Head2Head.Entities;
using Celeste.Mod.Head2Head.Integration;
using Celeste.Mod.Head2Head.Shared;
using Celeste.Mod.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.UI {
	public class OuiRunSelectILChapterPanel : Oui {

		private class Option {
			public string Label;
			public string ID;
			public MTexture Icon;
			public MTexture Bg = GFX.Gui["areaselect/tab"];
			public Color BgColor = Calc.HexToColor("3c6180");
			public RunOptionsILSide OptSide = null;
			public RunOptionsILCategory OptCategory = null;
			public float Pop;
			public bool Large = true;
			public int Siblings;
			public float Slide;
			public float Appear = 1f;
			public float IconEase = 1f;
			public bool Appeared;
			public float Faded;
			public float CheckpointSlideOut;
			public float CheckpointRotation;
			public Vector2 CheckpointOffset;

			public float Scale {
				get {
					if (Siblings < 5) {
						return 1f;
					}
					return 0.8f;
				}
			}

			public bool OnTopOfUI => Pop > 0.5f;

			public void SlideTowards(int i, int count, bool snap) {
				float num = (float)count / 2f - 0.5f;
				float num2 = (float)i - num;
				if (snap) {
					Slide = num2;
				}
				else {
					Slide = Calc.Approach(Slide, num2, Engine.DeltaTime * 4f);
				}
			}

			public Vector2 GetRenderPosition(Vector2 center) {
				float num = (float)(Large ? 170 : 130) * Scale;
				if (Siblings > 0 && num * (float)Siblings > 750f) {
					num = 750 / Siblings;
				}
				Vector2 result = center + new Vector2(Slide * num, (float)Math.Sin(Pop * (float)Math.PI) * 70f - Pop * 12f);
				result.Y += (1f - Ease.CubeOut(Appear)) * -200f;
				result.Y -= (1f - Scale) * 80f;
				return result;
			}

			public void Render(Vector2 center, bool selected, Wiggler wiggler, Wiggler appearWiggler) {
				float num = Scale + (selected ? (wiggler.Value * 0.25f) : 0f) + (Appeared ? (appearWiggler.Value * 0.25f) : 0f);
				Vector2 renderPosition = GetRenderPosition(center);
				Color color = Color.Lerp(BgColor, Color.Black, (1f - Pop) * 0.6f);
				Bg.DrawCentered(renderPosition + new Vector2(0f, 10f), color, (Appeared ? Scale : num) * new Vector2(Large ? 1f : 0.9f, 1f));
				if (Icon != null && IconEase > 0f) {
					float num2 = Ease.CubeIn(IconEase);
					Color color2 = Color.Lerp(Color.White, Color.Black, Faded * 0.6f) * num2;
					Icon.DrawCentered(renderPosition, color2, (Bg.Width - 50f) / Icon.Width * num * (2.5f - num2 * 1.5f));
				}
			}
		}

		public const int ContentOffsetX = 440;
		public const int PanelHeight = 300;

		internal static RunOptionsILChapter UsingChapter;
		internal static RunOptionsILSide UsingSide;

		private bool initialized;
		private float height;
		private bool resizing;
		private Wiggler wiggler;
		private Wiggler modeAppearWiggler;
		private MTexture card = new MTexture();
		private Vector2 contentOffset;
		private int hoveredCategoryIdx;
		private int hoveredModeIdx;
		private List<Option> modes = new List<Option>();
		private List<Option> categories = new List<Option>();

		public Vector2 OpenPosition => new Vector2(1070f, 100f);
		public Vector2 ClosePosition => new Vector2(2220f, 100f);
		public Vector2 IconOffset => new Vector2(690f, 86f);
		private Vector2 OptionsRenderPosition => Position + new Vector2(contentOffset.X, 128f + height);

		private int hoveredIdx {
			get => selectingMode ? hoveredModeIdx : hoveredCategoryIdx;
			set {
				if (selectingMode) hoveredModeIdx = value;
				else hoveredCategoryIdx = value;
			}
		}
			
		internal bool selectingMode => UsingSide == null;
		private Option hoveredOption => selectingMode ? modes[hoveredModeIdx] : categories[hoveredCategoryIdx];
		private List<Option> options => selectingMode ? modes : categories;

		public OuiRunSelectILChapterPanel() {
			Add(wiggler = Wiggler.Create(0.4f, 4f));
			Add(modeAppearWiggler = Wiggler.Create(0.4f, 4f));
		}

		public override bool IsStart(Overworld overworld, Overworld.StartMode start) {
			return false;
		}

		public override IEnumerator Enter(Oui from) {
			Position = ClosePosition;
			Visible = true;
			UsingSide = null;
			Reset();
			for (float p = 0f; p < 1f; p += Engine.DeltaTime * 4f) {
				yield return null;
				Position = ClosePosition + (OpenPosition - ClosePosition) * Ease.CubeOut(p);
			}
			Position = OpenPosition;
		}

		private void Reset() {
			height = GetModeHeight();
			modes.Clear();
			foreach (RunOptionsILSide side in UsingChapter.Sides) {
				modes.Add(new Option {
					Label = side.Label,
					Icon = side.Icon,
					ID = side.ID,
					OptSide = side,
				});
			}
			UsingSide = null;
			hoveredCategoryIdx = 0;
			hoveredModeIdx = 0;
			if (modes.Count > 0) {
				for (int i = 0; i < modes.Count; i++) {
					modes[i].SlideTowards(i, modes.Count, snap: true);
				}
			}
			contentOffset = new Vector2(440f, 120f);
			initialized = true;
		}

		private int GetModeHeight() {
			//var option = OuiRunSelectIL.GetChapterOption(ILSelector.LastLevelSetIndex, ILSelector.LastChapterIndex);
			//if (option.Data == null || RealStats == null) return 300;
			//AreaModeStats areaModeStats = RealStats.Modes[(int)Area.Mode];
			//bool flag = areaModeStats.Strawberries.Count <= 0;
			//if (!Data.Interlude_Safe && ((areaModeStats.Deaths > 0 && Area.Mode != 0) || areaModeStats.Completed || areaModeStats.HeartGem)) {
			//	flag = false;
			//}
			//if (!flag) {
			//	return 540;
			//}
			return PanelHeight;
		}

		public override IEnumerator Leave(Oui next) {
			Add(new Coroutine(EaseOut()));
			yield break;
		}

		public IEnumerator EaseOut(bool removeChildren = true) {
			for (float p = 0f; p < 1f; p += Engine.DeltaTime * 4f) {
				Position = OpenPosition + (ClosePosition - OpenPosition) * Ease.CubeIn(p);
				yield return null;
			}
			if (!Selected) {
				Visible = false;
			}
		}

		private void Start(Option opt) {
			Focused = false;
			Audio.Play("event:/ui/world_map/chapter/checkpoint_start");
			ILSelector.ChosenCategory = opt.OptCategory;
			Overworld.Goto<OuiRunSelectILExit>();
		}

		private void Swap() {
			Focused = false;
			Overworld.ShowInputUI = !selectingMode;
			Add(new Coroutine(SwapRoutine()));
		}

		private IEnumerator SwapRoutine() {
			// Safeguards against a crash i can't reliably reproduce
			if (options.Count == 0) yield break;
			// Now to the normal stuff
			float fromHeight = height;
			int toHeight = 730;
			resizing = true;
			PlayExpandSfx(fromHeight, toHeight);
			float offset = 800f;
			for (float p2 = 0f; p2 < 1f; p2 += Engine.DeltaTime * 4f) {
				yield return null;
				contentOffset.X = 440f + offset * Ease.CubeIn(p2);
				height = MathHelper.Lerp(fromHeight, toHeight, Ease.CubeOut(p2 * 0.5f));
			}
			if (selectingMode) {
				UsingSide = hoveredOption.OptSide;
			}
			else {
				UsingSide = null;
			}
			if (!selectingMode) {
				categories.Clear();
				int siblings = UsingChapter.Sides[hoveredModeIdx].Categories.Count;
				foreach (RunOptionsILCategory cat in UsingChapter.Sides[hoveredModeIdx].Categories) {
					categories.Add(new Option {
						Label = Util.TranslatedIfAvailable(cat.Title),
						BgColor = Calc.HexToColor("eabe26"),
						Icon = GFX.Gui.GetOrDefault(cat.IconPath, GFX.Gui["menu/play"]),
						OptCategory = cat,
						CheckpointRotation = Calc.Random.Choose(-1, 1) * Calc.Random.Range(0.05f, 0.2f),
						CheckpointOffset = new Vector2(Calc.Random.Range(-16, 16), Calc.Random.Range(-16, 16)),
						Large = false,
						Siblings = siblings,
					});
				}
				hoveredCategoryIdx = 0;
				for (int j = 0; j < categories.Count; j++) {
					categories[j].SlideTowards(j, categories.Count, snap: true);
				}
			}
			hoveredOption.Pop = 1f;
			for (float p2 = 0f; p2 < 1f; p2 += Engine.DeltaTime * 4f) {
				yield return null;
				height = MathHelper.Lerp(fromHeight, toHeight, Ease.CubeOut(Math.Min(1f, 0.5f + p2 * 0.5f)));
				contentOffset.X = 440f + offset * (1f - Ease.CubeOut(p2));
			}
			contentOffset.X = 440f;
			height = toHeight;
			Focused = true;
			resizing = false;
		}

		public override void Update() {
			if (Selected && Focused && Input.QuickRestart.Pressed) {
				Overworld.Goto<OuiRunSelectILMapSearch>();
			}
			if (!initialized) {
				return;
			}
			base.Update();
			for (int i = 0; i < options.Count; i++) {
				Option option = options[i];
				option.Pop = Calc.Approach(option.Pop, (hoveredIdx == i) ? 1f : 0f, Engine.DeltaTime * 4f);
				option.Appear = Calc.Approach(option.Appear, 1f, Engine.DeltaTime * 3f);
				option.CheckpointSlideOut = Calc.Approach(option.CheckpointSlideOut, (hoveredIdx > i) ? 1 : 0, Engine.DeltaTime * 4f);
				option.Faded = Calc.Approach(option.Faded, (hoveredIdx != i && !option.Appeared) ? 1 : 0, Engine.DeltaTime * 4f);
				option.SlideTowards(i, options.Count, snap: false);
			}
			if (selectingMode && !resizing) {
				height = Calc.Approach(height, GetModeHeight(), Engine.DeltaTime * 1600f);
			}
			if (Selected && Focused) {
				if (Input.MenuLeft.Pressed && hoveredIdx > 0) {
					Audio.Play("event:/ui/world_map/chapter/tab_roll_left");
					hoveredIdx--;
					wiggler.Start();
					if (selectingMode) {
						PlayExpandSfx(height, GetModeHeight());
					}
					else {
						Audio.Play("event:/ui/world_map/chapter/checkpoint_photo_add");
					}
				}
				else if (Input.MenuRight.Pressed && hoveredIdx + 1 < options.Count) {
					Audio.Play("event:/ui/world_map/chapter/tab_roll_right");
					hoveredIdx++;
					wiggler.Start();
					if (selectingMode) {
						PlayExpandSfx(height, GetModeHeight());
					}
					else {
						Audio.Play("event:/ui/world_map/chapter/checkpoint_photo_remove");
					}
				}
				else if (Input.MenuConfirm.Pressed && options.Count > 0) {
					if (selectingMode) {
						Audio.Play("event:/ui/world_map/chapter/level_select");
						Swap();
					}
					else {
						Start(hoveredOption);
					}
				}
				else if (Input.MenuCancel.Pressed) {
					if (selectingMode) {
						Audio.Play("event:/ui/world_map/chapter/back");
						GoBack();
					}
					else {
						Audio.Play("event:/ui/world_map/chapter/checkpoint_back");
						Swap();
					}
				}
			}
		}

		public override void Render() {
			if (!initialized) {
				return;
			}

			Vector2 optionsRenderPosition = OptionsRenderPosition;
			for (int i = 0; i < options.Count; i++) {
				if (!options[i].OnTopOfUI) {
					options[i].Render(optionsRenderPosition, hoveredIdx == i, wiggler, modeAppearWiggler);
				}
			}

			MTexture mTexture = GFX.Gui[_ModCardTexture("areaselect/cardtop")];
			mTexture.Draw(Position + new Vector2(0f, -32f));
			MTexture mTexture2 = GFX.Gui[_ModCardTexture("areaselect/card")];
			card = mTexture2.GetSubtexture(0, mTexture2.Height - (int)height, mTexture2.Width, (int)height, card);
			card.Draw(Position + new Vector2(0f, -32 + mTexture.Height));
			for (int j = 0; j < options.Count; j++) {
				if (options[j].OnTopOfUI) {
					options[j].Render(optionsRenderPosition, hoveredIdx == j, wiggler, modeAppearWiggler);
				}
			}
			if (hoveredIdx >= 0 && hoveredIdx < options.Count) {
				ActiveFont.Draw(hoveredOption.Label, optionsRenderPosition + new Vector2(0f, -140f), Vector2.One * 0.5f, Vector2.One * (1f + wiggler.Value * 0.1f), Color.Black * 0.8f);
			}
			else {
				ActiveFont.Draw(Dialog.Clean("Head2Head_Selector_NoValidCategories"), optionsRenderPosition + new Vector2(0f, -140f), Vector2.One * 0.5f, Vector2.One * (1f + wiggler.Value * 0.1f), Color.Black * 0.8f);
			}
			if (selectingMode) {
				base.Render();
			}
			if (!selectingMode) {
				Vector2 center = Position + new Vector2(contentOffset.X, 340f);
				for (int num = options.Count - 1; num >= 0; num--) {
					DrawCheckpoint(center, options[num], num);
				}
			}
			GFX.Gui["areaselect/title"].Draw(Position + new Vector2(_FixTitleLength(-60f), 0f), Vector2.Zero, UsingChapter.Data?.TitleBaseColor ?? Color.DarkSlateGray);
			GFX.Gui["areaselect/accent"].Draw(Position + new Vector2(_FixTitleLength(-60f), 0f), Vector2.Zero, UsingChapter.Data?.TitleAccentColor ?? Color.LightBlue);
			ActiveFont.Draw(UsingChapter.DisplayName, Position + IconOffset + new Vector2(-100f, -18f), new Vector2(1f, 0f), Vector2.One * 1f, (UsingChapter.Data?.TitleTextColor ?? Color.AntiqueWhite) * 0.8f);
		}

		private void DrawCheckpoint(Vector2 center, Option option, int checkpointIndex) {
			
		}

		private void PlayExpandSfx(float currentHeight, float nextHeight) {
			if (nextHeight > currentHeight) {
				Audio.Play("event:/ui/world_map/chapter/pane_expand");
			}
			else if (nextHeight < currentHeight) {
				Audio.Play("event:/ui/world_map/chapter/pane_contract");
			}
		}

		private string _ModCardTexture(string textureName) {
			string name = UsingChapter.Data?.Name ?? "";
			string text = textureName.Replace("areaselect/card", "areaselect/" + name + "_card");
			if (GFX.Gui.Has(text)) {
				textureName = text;
				return textureName;
			}
			string levelSet = UsingChapter.Data?.LevelSet ?? "";
			string levelSetTexture = textureName.Replace("areaselect/", "areaselect/" + levelSet + "/");
			if (GFX.Gui.Has(levelSetTexture)) {
				textureName = levelSetTexture;
				return textureName;
			}
			return textureName;
		}

		private float _FixTitleLength(float vanillaValue) {
			float x = ActiveFont.Measure(Dialog.Clean(UsingChapter.Data?.Name)).X;
			return vanillaValue - Math.Max(0f, x + vanillaValue - 490f);
		}

		private void GoBack() {
			string lobby = UsingChapter.CollabLobby;
			if (string.IsNullOrEmpty(lobby)) {
				Overworld.Goto<OuiRunSelectILChapterSelect>();
			}
			else {
				Overworld.Goto<OuiRunSelectILCollabMapSelect>();
			}
		}
	}
}
