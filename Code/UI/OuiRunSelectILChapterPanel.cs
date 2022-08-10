using Celeste.Mod.Head2Head.Entities;
using Celeste.Mod.Head2Head.Shared;
using Celeste.Mod.UI;
using FMOD.Studio;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.UI {
	public class OuiRunSelectILChapterPanel : Oui {
		// TODO prevent crash if there are no valid categories
		private class Option {
			public string Label;

			public string ID;

			public MTexture Icon;

			public MTexture Bg = GFX.Gui["areaselect/tab"];

			public Color BgColor = Calc.HexToColor("3c6180");

			public StandardCategory Category = StandardCategory.Clear;

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
				if (IconEase > 0f) {
					float num2 = Ease.CubeIn(IconEase);
					Color color2 = Color.Lerp(Color.White, Color.Black, Faded * 0.6f) * num2;
					Icon.DrawCentered(renderPosition, color2, (float)(Bg.Width - 50) / (float)Icon.Width * num * (2.5f - num2 * 1.5f));
				}
			}
		}

		public AreaKey Area;

		public AreaStats RealStats;

		public AreaStats DisplayedStats;

		public AreaData Data;

		public bool EnteringChapter;

		public const int ContentOffsetX = 440;

		public const int PanelHeight = 300;

		private bool initialized;

		private string chapter = "";

		private bool selectingMode = true;

		private float height;

		private bool resizing;

		private Wiggler wiggler;

		private Wiggler modeAppearWiggler;

		private MTexture card = new MTexture();

		private Vector2 contentOffset;

		private int checkpoint;

		private List<Option> modes = new List<Option>();

		private List<Option> categories = new List<Option>();

		private bool instantClose;

		public Vector2 OpenPosition => new Vector2(1070f, 100f);

		public Vector2 ClosePosition => new Vector2(2220f, 100f);

		public Vector2 IconOffset => new Vector2(690f, 86f);

		private Vector2 OptionsRenderPosition => Position + new Vector2(contentOffset.X, 128f + height);

		private int option {
			get {
				if (!selectingMode) {
					return checkpoint;
				}
				return (int)Area.Mode;
			}
			set {
				if (selectingMode) {
					Area.Mode = (AreaMode)value;
				}
				else {
					checkpoint = value;
				}
			}
		}

		private List<Option> options {
			get {
				if (!selectingMode) {
					return categories;
				}
				return modes;
			}
		}

		public OuiRunSelectILChapterPanel() {
			Add(wiggler = Wiggler.Create(0.4f, 4f));
			Add(modeAppearWiggler = Wiggler.Create(0.4f, 4f));
		}

		public override bool IsStart(Overworld overworld, Overworld.StartMode start) {
			return false;
		}

		public override IEnumerator Enter(Oui from) {
			if (instantClose) {
				Overworld.Goto<OuiRunSelectILChapterSelect>();
				Visible = false;
				instantClose = false;
				yield break;
			}
			else {
				Visible = true;
				Area.Mode = AreaMode.Normal;
				Reset();
				for (float p = 0f; p < 1f; p += Engine.DeltaTime * 4f) {
					yield return null;
					Position = ClosePosition + (OpenPosition - ClosePosition) * Ease.CubeOut(p);
				}
				Position = OpenPosition;
			}
		}

		private void Reset() {
			Area = ILSelector.LastArea.Local_Safe;
			Data = AreaData.Areas[Area.ID];
			RealStats = SaveData.Instance.Areas_Safe[Area.ID];
			if (SaveData.Instance.CurrentSession_Safe != null && SaveData.Instance.CurrentSession_Safe.OldStats != null && SaveData.Instance.CurrentSession_Safe.Area.ID == Area.ID) {
				DisplayedStats = SaveData.Instance.CurrentSession_Safe.OldStats;
				SaveData.Instance.CurrentSession_Safe = null;
			}
			else {
				DisplayedStats = RealStats;
			}
			height = GetModeHeight();
			modes.Clear();
			// TODO AltSideHelper support
			modes.Add(new Option {
				Label = Dialog.Clean(Data.Interlude ? "FILE_BEGIN" : "overworld_normal").ToUpper(),
				Icon = GFX.Gui["menu/play"],
				ID = "A"
			});
			if (Data.HasMode(AreaMode.BSide)) {
				modes.Add(new Option {
					Label = Dialog.Clean("overworld_remix"),
					Icon = GFX.Gui["menu/remix"],
					ID = "B"
				});
			}
			if (Data.HasMode(AreaMode.CSide)) {
				modes.Add(new Option {
					Label = Dialog.Clean("overworld_remix2"),
					Icon = GFX.Gui["menu/rmx2"],
					ID = "C"
				});
			}

			selectingMode = true;
			for (int i = 0; i < options.Count; i++) {
				options[i].SlideTowards(i, options.Count, snap: true);
			}
			chapter = Dialog.Get("area_chapter").Replace("{x}", Area.ChapterIndex.ToString().PadLeft(2));
			contentOffset = new Vector2(440f, 120f);
			initialized = true;
		}

		private int GetModeHeight() {
			AreaModeStats areaModeStats = RealStats.Modes[(int)Area.Mode];
			bool flag = areaModeStats.Strawberries.Count <= 0;
			if (!Data.Interlude_Safe && ((areaModeStats.Deaths > 0 && Area.Mode != 0) || areaModeStats.Completed || areaModeStats.HeartGem)) {
				flag = false;
			}
			if (!flag) {
				return 540;
			}
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

		public void Start(StandardCategory cat) {
			Focused = false;
			Audio.Play("event:/ui/world_map/chapter/checkpoint_start");

			if (ILSelector.ActiveSelector != null) {
				ILSelector.ActiveSelector.Area = new GlobalAreaKey(Area);
				ILSelector.ActiveSelector.Category = cat;
			}
			Overworld.Goto<OuiRunSelectILExit>();
		}

		private void Swap() {
			Focused = false;
			base.Overworld.ShowInputUI = !selectingMode;
			Add(new Coroutine(SwapRoutine()));
		}

		private IEnumerator SwapRoutine() {
			float fromHeight = height;
			//int toHeight = (selectingMode ? 730 : GetModeHeight());
			int toHeight = 730;
			resizing = true;
			PlayExpandSfx(fromHeight, toHeight);
			float offset = 800f;
			for (float p2 = 0f; p2 < 1f; p2 += Engine.DeltaTime * 4f) {
				yield return null;
				contentOffset.X = 440f + offset * Ease.CubeIn(p2);
				height = MathHelper.Lerp(fromHeight, toHeight, Ease.CubeOut(p2 * 0.5f));
			}
			selectingMode = !selectingMode;
			if (!selectingMode) {
				categories.Clear();

				StandardCategory[] cats = (StandardCategory[])Enum.GetValues(typeof(StandardCategory));
				int siblings = cats.Length;
				foreach (StandardCategory cat in cats) {
					if (!StandardMatches.IsCategoryValid(cat, new GlobalAreaKey(Area))) continue;
					categories.Add(new Option {
						Label = Dialog.Get(string.Format("Head2Head_CategoryName_{0}", cat.ToString())),
						BgColor = Calc.HexToColor("eabe26"),
						Icon = GFX.Gui[string.Format("Head2Head/Categories/{0}", cat.ToString())],
						Category = cat,
						CheckpointRotation = (float)Calc.Random.Choose(-1, 1) * Calc.Random.Range(0.05f, 0.2f),
						CheckpointOffset = new Vector2(Calc.Random.Range(-16, 16), Calc.Random.Range(-16, 16)),
						Large = false,
						Siblings = siblings
					});
				}
				option = 0;
				for (int j = 0; j < options.Count; j++) {
					options[j].SlideTowards(j, options.Count, snap: true);
				}
			}
			options[option].Pop = 1f;
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
				Overworld.Goto<OuiRunSelectILChapterSelect>();
				Overworld.Goto<OuiMapSearch>();
			}
			else if (instantClose) {
				Overworld.Goto<OuiRunSelectILChapterSelect>();
				Visible = false;
				instantClose = false;
			}
			else {
				orig_Update();
			}
		}

		public override void Render() {
			if (!initialized) {
				return;
			}
			Vector2 optionsRenderPosition = OptionsRenderPosition;
			for (int i = 0; i < options.Count; i++) {
				if (!options[i].OnTopOfUI) {
					options[i].Render(optionsRenderPosition, option == i, wiggler, modeAppearWiggler);
				}
			}
			bool flag = false;
			if (RealStats.Modes[(int)Area.Mode].Completed) {
				int mode = (int)Area.Mode;
				foreach (EntityData goldenberry in AreaData.Areas[Area.ID].Mode[mode].MapData.Goldenberries) {
					EntityID item = new EntityID(goldenberry.Level.Name, goldenberry.ID);
					if (RealStats.Modes[mode].Strawberries.Contains(item)) {
						flag = true;
						break;
					}
				}
			}
			MTexture mTexture = GFX.Gui[(!flag) ? _ModCardTexture("areaselect/cardtop") : _ModCardTexture("areaselect/cardtop_golden")];
			mTexture.Draw(Position + new Vector2(0f, -32f));
			MTexture mTexture2 = GFX.Gui[(!flag) ? _ModCardTexture("areaselect/card") : _ModCardTexture("areaselect/card_golden")];
			card = mTexture2.GetSubtexture(0, mTexture2.Height - (int)height, mTexture2.Width, (int)height, card);
			card.Draw(Position + new Vector2(0f, -32 + mTexture.Height));
			for (int j = 0; j < options.Count; j++) {
				if (options[j].OnTopOfUI) {
					options[j].Render(optionsRenderPosition, option == j, wiggler, modeAppearWiggler);
				}
			}
			ActiveFont.Draw(options[option].Label, optionsRenderPosition + new Vector2(0f, -140f), Vector2.One * 0.5f, Vector2.One * (1f + wiggler.Value * 0.1f), Color.Black * 0.8f);
			if (selectingMode) {
				base.Render();
			}
			if (!selectingMode) {
				Vector2 center = Position + new Vector2(contentOffset.X, 340f);
				for (int num = options.Count - 1; num >= 0; num--) {
					DrawCheckpoint(center, options[num], num);
				}
			}
			GFX.Gui["areaselect/title"].Draw(Position + new Vector2(_FixTitleLength(-60f), 0f), Vector2.Zero, Data.TitleBaseColor);
			GFX.Gui["areaselect/accent"].Draw(Position + new Vector2(_FixTitleLength(-60f), 0f), Vector2.Zero, Data.TitleAccentColor);
			string text = Dialog.Clean(AreaData.Get(Area).Name);
			if (Data.Interlude_Safe) {
				ActiveFont.Draw(text, Position + IconOffset + new Vector2(-100f, 0f), new Vector2(1f, 0.5f), Vector2.One * 1f, Data.TitleTextColor * 0.8f);
			}
			else {
				ActiveFont.Draw(chapter, Position + IconOffset + new Vector2(-100f, -2f), new Vector2(1f, 1f), Vector2.One * 0.6f, Data.TitleAccentColor * 0.8f);
				ActiveFont.Draw(text, Position + IconOffset + new Vector2(-100f, -18f), new Vector2(1f, 0f), Vector2.One * 1f, Data.TitleTextColor * 0.8f);
			}
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

		public IEnumerator orig_Enter(Oui from) {
			Visible = true;
			Area.Mode = AreaMode.Normal;
			Reset();
			for (float p = 0f; p < 1f; p += Engine.DeltaTime * 4f) {
				yield return null;
				Position = ClosePosition + (OpenPosition - ClosePosition) * Ease.CubeOut(p);
			}
			Position = OpenPosition;
		}

		public void orig_Update() {
			if (!initialized) {
				return;
			}
			base.Update();
			for (int i = 0; i < options.Count; i++) {
				Option option = options[i];
				option.Pop = Calc.Approach(option.Pop, (this.option == i) ? 1f : 0f, Engine.DeltaTime * 4f);
				option.Appear = Calc.Approach(option.Appear, 1f, Engine.DeltaTime * 3f);
				option.CheckpointSlideOut = Calc.Approach(option.CheckpointSlideOut, (this.option > i) ? 1 : 0, Engine.DeltaTime * 4f);
				option.Faded = Calc.Approach(option.Faded, (this.option != i && !option.Appeared) ? 1 : 0, Engine.DeltaTime * 4f);
				option.SlideTowards(i, options.Count, snap: false);
			}
			if (selectingMode && !resizing) {
				height = Calc.Approach(height, GetModeHeight(), Engine.DeltaTime * 1600f);
			}
			if (base.Selected && Focused) {
				if (Input.MenuLeft.Pressed && this.option > 0) {
					Audio.Play("event:/ui/world_map/chapter/tab_roll_left");
					this.option--;
					wiggler.Start();
					if (selectingMode) {
						PlayExpandSfx(height, GetModeHeight());
					}
					else {
						Audio.Play("event:/ui/world_map/chapter/checkpoint_photo_add");
					}
				}
				else if (Input.MenuRight.Pressed && this.option + 1 < options.Count) {
					Audio.Play("event:/ui/world_map/chapter/tab_roll_right");
					this.option++;
					wiggler.Start();
					if (selectingMode) {
						PlayExpandSfx(height, GetModeHeight());
					}
					else {
						Audio.Play("event:/ui/world_map/chapter/checkpoint_photo_remove");
					}
				}
				else if (Input.MenuConfirm.Pressed) {
					if (selectingMode) {
						Audio.Play("event:/ui/world_map/chapter/level_select");
						Swap();
					}
					else {
						Start(options[option].Category);
					}
				}
				else if (Input.MenuCancel.Pressed) {
					if (selectingMode) {
						Audio.Play("event:/ui/world_map/chapter/back");
						Overworld.Goto<OuiRunSelectILChapterSelect>();
					}
					else {
						Audio.Play("event:/ui/world_map/chapter/checkpoint_back");
						Swap();
					}
				}
			}
		}

		private string _ModCardTexture(string textureName) {
			string name = AreaData.Areas[Area.ID].Name;
			string text = textureName.Replace("areaselect/card", "areaselect/" + name + "_card");
			if (GFX.Gui.Has(text)) {
				textureName = text;
				return textureName;
			}
			string text2 = Area.GetLevelSet();
			string text3 = textureName.Replace("areaselect/", "areaselect/" + text2 + "/");
			if (GFX.Gui.Has(text3)) {
				textureName = text3;
				return textureName;
			}
			return textureName;
		}

		private float _FixTitleLength(float vanillaValue) {
			float x = ActiveFont.Measure(Dialog.Clean(AreaData.Get(Area).Name)).X;
			return vanillaValue - Math.Max(0f, x + vanillaValue - 490f);
		}
	}
}
