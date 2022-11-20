using Celeste.Mod.Head2Head.Shared;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.Entities {
	public class FullgameSelectorUI : Entity {
		private List<Option> categories;

		private static readonly Vector2 CanvasSize = new Vector2(1920, 1080);

		public FullgameSelectorUI() {
			Tag = Tags.HUD;
		}

		public override void Added(Scene scene) {
			base.Added(scene);
			categories = GetCategories();
			int count = 0;
			foreach (Option cat in categories) {
				Vector2 posOffset = Vector2.UnitX * count * 200f;
				cat.TitleComponent = new FGSText() {
					Text = cat.Title,
					PositionImmediate = new Vector2(CanvasSize.X / 2f, 150f) + posOffset,
				};
				MTexture tex = cat.Icon ?? GFX.Gui[Util.CategoryToIcon(StandardCategory.Custom)];
				cat.IconComponent = new FGSIcon() {
					Icon = tex,
					PositionImmediate = new Vector2(CanvasSize.X / 2f, 50f) + posOffset,
				};
				count++;
			}
		}

		private List<Option> GetCategories() {
			// TODO Custom fullgame categories
			List<Option> ret = new List<Option>();
			ret.Add(new Option() {
				Cat = StandardCategory.AnyPercent,
			});
			ret.Add(new Option() {
				Cat = StandardCategory.AllRedBerries,
			});
			return ret;
		}

		public override void Update() => base.Update();

		public override void Render() {
			base.Render();
			if (categories != null) {
				foreach (Option o in categories) {
					o.IconComponent?.Render();
					o.TitleComponent?.Render();
				}
			}
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
		}

		public abstract class FGSComponent {
			public bool Shown = true;
			public float EaseTime = 0.2f;
			private Vector2 EaseBase;
			private Vector2 EaseTarget;
			private float EaseProgress;
			protected Vector2 Position {
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
