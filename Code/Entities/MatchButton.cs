using Celeste.Mod.Entities;
using Celeste.Mod.Head2Head.Shared;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.Entities {
	[CustomEntity("Head2Head/MatchSwitch")]
	public class MatchButton : Solid {
		public static ParticleType P_PressA;
		public static ParticleType P_PressB;

		private string action;
		private Sprite sprite;
		private DashSwitch.Sides side = DashSwitch.Sides.Right;
		private Vector2 unpressedTarget;
		private Vector2 pressedTarget;
		private Vector2 pressDirection;
		private float startY;
		private bool pressed;

		static MatchButton() {
			P_PressA = new ParticleType {
				Color = Calc.HexToColor("99e550"),
				Color2 = Calc.HexToColor("d9ffb5"),
				ColorMode = ParticleType.ColorModes.Blink,
				Size = 1f,
				SizeRange = 0f,
				SpeedMin = 60f,
				SpeedMax = 80f,
				LifeMin = 0.8f,
				LifeMax = 1.2f,
				DirectionRange = 0.6981317f,
				SpeedMultiplier = 0.2f
			};
			P_PressB = new ParticleType(DashSwitch.P_PressA) {
				SpeedMin = 100f,
				SpeedMax = 110f,
				DirectionRange = 0.34906584f
			};
		}

		public MatchButton(EntityData data, Vector2 offset)
			: base(data.Position + new Vector2(-8, -8), 0f, 0f, safe: true)
		{
			action = data.Attr("action", "Join");
			unpressedTarget = Position;
			Add(sprite = GFX.SpriteBank.Create(string.Format("Head2Head_MatchSwitch_{0}", action)));
			sprite.Play("idle");
			if (side == DashSwitch.Sides.Up || side == DashSwitch.Sides.Down) {
				Collider.Width = 16f;
				Collider.Height = 8f;
			}
			else {
				Collider.Width = 8f;
				Collider.Height = 16f;
			}
			switch (side) {
				case DashSwitch.Sides.Down:
					sprite.Position = new Vector2(8f, 8f);
					sprite.Rotation = (float)Math.PI / 2f;
					pressedTarget = Position + Vector2.UnitY * 8f;
					pressDirection = Vector2.UnitY;
					startY = base.Y;
					break;
				case DashSwitch.Sides.Up:
					sprite.Position = new Vector2(8f, 0f);
					sprite.Rotation = -(float)Math.PI / 2f;
					pressedTarget = Position + Vector2.UnitY * -8f;
					pressDirection = -Vector2.UnitY;
					break;
				case DashSwitch.Sides.Right:
					sprite.Position = new Vector2(8f, 8f);
					sprite.Rotation = 0f;
					pressedTarget = Position + Vector2.UnitX * 8f;
					pressDirection = Vector2.UnitX;
					break;
				case DashSwitch.Sides.Left:
					sprite.Position = new Vector2(0f, 8f);
					sprite.Rotation = (float)Math.PI;
					pressedTarget = Position + Vector2.UnitX * -8f;
					pressDirection = -Vector2.UnitX;
					break;
			}
			OnDashCollide = OnDashed;
		}

		public DashCollisionResults OnDashed(Player player, Vector2 direction) {
			if (!pressed && direction == pressDirection) {
				Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
				Audio.Play("event:/game/05_mirror_temple/button_activate", Position);
				sprite.Play("push");
				pressed = true;
				MoveTo(pressedTarget);
				Collidable = false;
				Position -= pressDirection * 2f;

				SceneAs<Level>().ParticlesFG.Emit(P_PressA, 10, Position + sprite.Position,
					direction.Perpendicular() * 6f, sprite.Rotation - (float)Math.PI);
				SceneAs<Level>().ParticlesFG.Emit(P_PressB, 4, Position + sprite.Position,
					direction.Perpendicular() * 6f, sprite.Rotation - (float)Math.PI);

				switch (action) {
					case "Stage":
						if (Head2HeadModule.Instance.CanStageMatch()) {
							Head2HeadModule.Instance.StageMatch();
						}
						break;
					case "Join":
						if (Head2HeadModule.Instance.CanJoinMatch()) {
							Head2HeadModule.Instance.JoinStagedMatch();
						}
						break;
					case "Start":
						if (Head2HeadModule.Instance.CanStartMatch()) {
							Head2HeadModule.Instance.BeginStagedMatch();
						}
						break;
				}
			}
			return DashCollisionResults.NormalCollision;
		}

		public override void Added(Scene scene) {
			base.Added(scene);
			UpdateSwitchState();
			Head2HeadModule.OnMatchCurrentMatchUpdated += UpdateSwitchState;
		}

		public override void Removed(Scene scene) {
			base.Removed(scene);
			Head2HeadModule.OnMatchCurrentMatchUpdated -= UpdateSwitchState;
		}

		private void OnPlayerStateChanged(PlayerStatus source, PlayerStateCategory oldstate, PlayerStateCategory newstate) {
			if (source != PlayerStatus.Current) return;
			UpdateSwitchState();
		}

		private void OnPlayerJoinedMatch(PlayerID player, string matchID) {
			UpdateSwitchState();
		}

		private void UpdateSwitchState() {
			if (Scene == null) return;  // IDK why but this is a thing but it seems to work
			bool pressable = (action == "Stage" && Head2HeadModule.Instance.CanStageMatch())
				|| (action == "Join" && Head2HeadModule.Instance.CanJoinMatch())
				|| (action == "Start" && Head2HeadModule.Instance.CanStartMatch());
			if (pressable != pressed) return;
			if (pressable) {
				sprite.Play("idle");
				pressed = false;
				MoveTo(unpressedTarget);
				Collidable = true;
				Position += pressDirection * 2f;
			}
			else {
				sprite.Play("push");
				pressed = true;
				MoveTo(pressedTarget);
				Collidable = false;
				Position -= pressDirection * 2f;
			}
		}
	}
}
