using Celeste.Mod.Head2Head.Entities;
using Celeste.Mod.Head2Head.Shared;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.UI {
	public class ChapterIcon : Entity {

		public const float IdleSize = 100f;
		public const float HoverSize = 144f;
		public const float HoverSpacing = 80f;
		public const float IdleY = 130f;
		public const float HoverY = 140f;
		public const float Spacing = 32f;

		public readonly RunOptionsILChapter ChapterOption;
		public readonly int IndexInSet;
		public static int CurrentlyHoveredIndex;
		private int knownHoveredIndex;

		public Vector2 Scale = Vector2.One;
		public float Rotation;
		public float sizeEase = 1f;
		private Wiggler newWiggle;
		private bool selected;
		private Tween tween;
		private Wiggler wiggler;
		private bool wiggleLeft;
		private int rotateDir = -1;
		private Vector2 shake;
		private MTexture front;
		private MTexture back;

		public Vector2 IdlePosition {
			get {
				float x = 960f + (IndexInSet - CurrentlyHoveredIndex) * 132f;
				if (IndexInSet < CurrentlyHoveredIndex) {
					x -= 80f;
				}
				else if (IndexInSet > CurrentlyHoveredIndex) {
					x += 80f;
				}
				float y = 130f;
				if (IndexInSet == CurrentlyHoveredIndex) {
					y = 140f;
				}
				return new Vector2(x, y);
			}
		}

		public Vector2 HiddenPosition => new Vector2(IdlePosition.X, -100f);

		public bool IsSelected => selected;

		public ChapterIcon(RunOptionsILChapter chap, MTexture front, MTexture back, int indexInSet) {
			knownHoveredIndex = -1;
			ChapterOption = chap;
			IndexInSet = indexInSet;
			Tag = (int)Tags.HUD | (int)Tags.PauseUpdate;
			this.front = front;
			this.back = back;
			Add(wiggler = Wiggler.Create(0.35f, 2f, delegate (float f)
			{
				Rotation = (wiggleLeft ? (0f - f) : f) * 0.4f;
				Scale = Vector2.One * (1f + f * 0.5f);
			}));
			Add(newWiggle = Wiggler.Create(0.8f, 2f));
			newWiggle.StartZero = true;
			Position = HiddenPosition;
		}

		public void Hovered(int dir) {
			wiggleLeft = dir < 0;
			wiggler.Start();
		}

		public void Select() {
			Audio.Play("event:/ui/world_map/icon/flip_right");
			selected = true;
			Vector2 from = Position;
			StartTween(0.6f, delegate (Tween t)
			{
				SetSelectedPercent(from, t.Percent);
			});
		}

		public void SnapToSelected() {
			selected = true;
			StopTween();
		}

		public void Unselect() {
			Audio.Play("event:/ui/world_map/icon/flip_left");
			selected = false;
			Vector2 to = IdlePosition;
			StartTween(0.6f, delegate (Tween t)
			{
				SetSelectedPercent(to, 1f - t.Percent);
			});
		}

		public void Hide() {
			Scale = Vector2.One;
			selected = false;
			Vector2 from = Position;
			StartTween(0.25f, delegate
			{
				Position = Vector2.Lerp(from, HiddenPosition, tween.Eased);
			});
		}

		public void Show() {
			knownHoveredIndex = -1;
			Scale = Vector2.One;
			selected = false;
			Vector2 from = Position;
			StartTween(0.25f, delegate
			{
				Position = Vector2.Lerp(from, IdlePosition, tween.Eased);
			});
		}

		private void StartTween(float duration, Action<Tween> callback, Ease.Easer easer = null) {
			StopTween();
			Add(tween = Tween.Create(Tween.TweenMode.Oneshot, easer, duration, start: true));
			tween.OnUpdate = callback;
			tween.OnComplete = delegate
			{
				tween = null;
			};
		}

		private void StopTween() {
			if (tween != null) {
				Remove(tween);
			}
			tween = null;
		}

		private void SetSelectedPercent(Vector2 from, float progress) {
			OuiRunSelectILChapterPanel uI = (Scene as Overworld)?.GetUI<OuiRunSelectILChapterPanel>();
			Vector2 vector = uI.OpenPosition + uI.IconOffset;
			SimpleCurve simpleCurve = new SimpleCurve(from, vector, (from + vector) / 2f + new Vector2(0f, 30f));
			float scale = 1f + ((progress < 0.5f) ? (progress * 2f) : ((1f - progress) * 2f));
			Scale.X = (float)Math.Cos(Ease.SineInOut(progress) * ((float)Math.PI * 2f)) * scale;
			Scale.Y = scale;
			Position = simpleCurve.GetPoint(Ease.Invert(Ease.CubeInOut)(progress));
			Rotation = Ease.UpDown(Ease.SineInOut(progress)) * ((float)Math.PI / 180f) * 15f * (float)rotateDir;
			if (progress <= 0f) {
				rotateDir = -1;
			}
			else if (progress >= 1f) {
				rotateDir = 1;
			}
		}

		public override void Update() {
			sizeEase = Calc.Approach(sizeEase, (CurrentlyHoveredIndex == IndexInSet) ? 1f : 0f, Engine.DeltaTime * 4f);
			if (knownHoveredIndex != CurrentlyHoveredIndex) {
				knownHoveredIndex = CurrentlyHoveredIndex;
				if (CurrentlyHoveredIndex == IndexInSet) {
					Depth = -50;
				}
				else {
					Depth = -45;
				}
				Vector2 from = Position;
				Vector2 to = Vector2.Zero;
				if (selected) {
					OuiRunSelectILChapterPanel uI = (Scene as Overworld).GetUI<OuiRunSelectILChapterPanel>();
					to = uI.Position + uI.IconOffset;
				}
				else {
					to = IdlePosition;
				}
				StartTween(0.2f, delegate {
					Position = Vector2.Lerp(from, to, tween.Eased);
				}, Ease.CubeOut);
			}
			if (Scene.OnInterval(1.5f)) {
				newWiggle.Start();
			}
			base.Update();
		}

		public override void Render() {
			MTexture mTexture = front;
			Vector2 scale = Scale;
			int width = mTexture.Width;
			if (scale.X < 0f) {
				mTexture = back;
			}
			scale *= (100f + 44f * Ease.CubeInOut(sizeEase)) / width;
			if (SaveData.Instance?.Assists.MirrorMode == true) {
				scale.X = 0f - scale.X;
			}
			mTexture.DrawCentered(Position + shake, Color.White, scale, Rotation);
		}
	}
}
