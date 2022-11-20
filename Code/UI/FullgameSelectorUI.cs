using Celeste.Mod.Head2Head.Shared;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.Entities {
	[Tracked]
	public class FullgameSelectorUI : Entity {
		private static readonly Vector2 CanvasSize = new Vector2(1920, 1080);

		public event Action OnRemove;
		private List<Option> categories;
		private static int HoveredOption = 0;
		private MTexture scarf = GFX.Gui["Head2Head/scarf"];
		private MTexture[] scarfSegments;
		private static readonly int scarfSegmentSize = 12;
		private float timer = 0f;

		public FullgameSelectorUI() {
			Tag = Tags.HUD;
		}

		public override void Added(Scene scene) {
			base.Added(scene);
			categories = GetCategories();
			HoveredOption = Calc.Clamp(HoveredOption, 0, categories.Count);
			UpdateUITargetPositions();
			scarfSegments = new MTexture[scarf.Height / scarfSegmentSize];
			for (int j = 0; j < scarfSegments.Length; j++) {
				scarfSegments[j] = scarf.GetSubtexture(0, j * scarfSegmentSize, scarf.Width, scarfSegmentSize);
			}
		}

		public override void Removed(Scene scene) {
			base.Removed(scene);
			OnRemove?.Invoke();
		}

		private List<Option> GetCategories() {
			// TODO Custom fullgame categories
			// TODO Remaining standard fullgame categories
			List<Option> ret = new List<Option>();
			StandardCategory[] standardCats = new StandardCategory[] {
				StandardCategory.AnyPercent,
				StandardCategory.AllRedBerries,
				StandardCategory.TrueEnding,
				StandardCategory.AllCassettes,
				StandardCategory.BnyPercent,
				StandardCategory.AllHearts,
				StandardCategory.OneHundredPercent,
				StandardCategory.AllChapters,
				StandardCategory.AllASides,
				StandardCategory.AllBSides,
				StandardCategory.AllCSides,
			};
			foreach(StandardCategory cat in standardCats) {
				ret.Add(new Option() { Cat = cat }.AfterInitialize());
			}

			return ret;
		}

		public override void Update() {
			base.Update();

			timer += Engine.DeltaTime;

			foreach (Option o in categories) {
				o.IconComponent?.Update();
				o.TitleComponent?.Update();
			}

			if (Input.MenuCancel.Pressed) {
				Audio.Play("event:/ui/world_map/chapter/back");
				Add(new Coroutine(CloseCoroutine()));
			}
			else if (Input.MenuRight.Pressed && HoveredOption < (categories?.Count ?? 0) - 1) {
				Audio.Play("event:/ui/world_map/icon/roll_right");
				HoveredOption++;
				UpdateUITargetPositions();
			}
			else if (Input.MenuLeft.Pressed && HoveredOption > 0) {
				Audio.Play("event:/ui/world_map/icon/roll_left");
				HoveredOption--;
				UpdateUITargetPositions();
			}
			else if (Input.MenuConfirm.Pressed) {
				Confirm();
			}
		}

		public override void Render() {
			base.Render();
			Vector2 AnchorPosition = new Vector2(CanvasSize.X / 2f, -50f);
			for (int i = 0; i < scarfSegments.Length; i++) {
				float wave = 8f * (float)Math.Sin(2f * (i / (float)scarfSegments.Length) - timer);
				scarfSegments[i].DrawJustified(
					AnchorPosition + Vector2.UnitY * (scarfSegments[i].Height * i) + Vector2.UnitX * wave,
					Vector2.UnitX / 2f);
			}
			if (categories != null) {
				foreach (Option o in categories) {
					o.IconComponent?.Render();
					o.TitleComponent?.Render();
				}
			}
		}

		private void UpdateUITargetPositions() {
			Vector2 AnchorPosition = new Vector2(CanvasSize.X / 2f, 0);
			for (int i = 0; i < (categories?.Count ?? 0); i++) {
				Vector2 basePosition = Vector2.UnitX * (i - HoveredOption) * 300f;
				basePosition += Vector2.UnitY * (100f * (float)Math.Cos(basePosition.X / CanvasSize.X * 2f * Math.PI) + 100f);
				basePosition += AnchorPosition;

				categories[i].IconComponent.Position = basePosition + (Vector2.UnitY * 50f);
				categories[i].TitleComponent.Position = basePosition + (Vector2.UnitY * 150f);
			}
		}

		private void Confirm() {
			if (Head2HeadModule.Instance.CanBuildMatch()) {
				Audio.Play("event:/ui/world_map/chapter/checkpoint_start");
				if (categories?.Count > 0) {
					Option o = categories[HoveredOption];
					MatchDefinition def;
					if (o.Cat == StandardCategory.Custom) {
						// TODO (!!!)
						throw new NotImplementedException("Custom Fullgame Categories have not been implemented yet");
					}
					else {
						def = StandardMatches.GetFullgameCategoryDefinition(o.Cat);
					}
					if (def == null) {
						Logger.Log("Head2Head.Err", "Fullgame category definition is null: " + o.Cat.ToString());
						return;
					}
					Head2HeadModule.Instance.buildingMatch = def;
					Head2HeadModule.Instance.StageMatch();
				}
			}
			else {
				Audio.Play("event:/ui/world_map/chapter/back");
			}
			Add(new Coroutine(CloseCoroutine()));
		}

		private IEnumerator CloseCoroutine() {
			foreach (Option o in categories) {
				o.IconComponent.Position = new Vector2(o.IconComponent.Position.X, -200f);
				o.TitleComponent.Position = new Vector2(o.IconComponent.Position.X, -50f);
			}
			yield return 0.25f;
			RemoveSelf();
		}

		private class Option {
			public StandardCategory Cat;

			public string CustomTitle;
			public MTexture CustomIcon;
			public CustomMatchTemplate Template;

			public string Title {
				get {
					if (!string.IsNullOrEmpty(CustomTitle)) return CustomTitle;
					return Util.TranslatedCategoryName(Cat);
				}
			}
			public MTexture Icon {
				get {
					if (CustomIcon != null) return CustomIcon;
					switch (Cat) {
						default:
							return null;
						case StandardCategory.AnyPercent:
							return GFX.Gui[Util.CategoryToIcon(StandardCategory.Clear)];
						case StandardCategory.AllRedBerries:
							return GFX.Gui[Util.CategoryToIcon(StandardCategory.ARB)];
					}
				}
			}

			public FGSComponent IconComponent;
			public FGSComponent TitleComponent;

			public Option AfterInitialize() {
				TitleComponent = new FGSText() {
					Text = Title,
				};
				MTexture tex = Icon ?? GFX.Gui[Util.CategoryToIcon(StandardCategory.Custom)];
				IconComponent = new FGSIcon() {
					Icon = tex,
				};
				return this;
			}
		}

		public abstract class FGSComponent {
			public bool Shown = true;
			public float EaseTime = 0.2f;
			private Vector2 EaseBase = new Vector2(CanvasSize.X / 2f, -200);
			private Vector2 EaseTarget = new Vector2(CanvasSize.X / 2f, -200);
			private float EaseProgress = 1;
			public Vector2 Position {
				get {
					float eased = Ease.CubeInOut(Calc.Clamp(EaseProgress, 0, 1));
					return new Vector2(Calc.LerpClamp(EaseBase.X, EaseTarget.X, eased), Calc.LerpClamp(EaseBase.Y, EaseTarget.Y, eased));
				}
				set {
					if (value.Equals(EaseTarget)) return;
					EaseBase = Position;
					EaseTarget = value;
					EaseProgress = 0;
				}
			}

			public Vector2 PositionImmediate {
				set {
					EaseBase = value;
					EaseTarget = value;
					EaseProgress = 1;
				}
				get { return Position; }
			}

			public void Update() {
				EaseProgress = Calc.Approach(EaseProgress, 1, Engine.DeltaTime / EaseTime);
			}

			public abstract void Render();
		}

		public class FGSIcon : FGSComponent {
			public MTexture Icon;
			public override void Render() {
				if (Shown) {
					Icon.DrawJustified(Position, new Vector2(0.5f, 0.5f));
				}
			}
		}

		public class FGSText : FGSComponent {
			public string Text;
			public override void Render() {
				if (Shown) {
					ActiveFont.DrawOutline(Text, Position, new Vector2(0.5f, 0.5f), Vector2.One, Color.White, 2f, Color.Black);
				}
			}
		}
	}
}
