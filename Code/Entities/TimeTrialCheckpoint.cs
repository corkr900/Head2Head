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
	[CustomEntity("Head2Head/TimeTrialCheckpoint")]
	[Tracked]
	public class TimeTrialCheckpoint : Entity {
		public class ConfettiRenderer : Entity {
			private struct Particle {
				public Vector2 Position;
				public Color Color;
				public Vector2 Speed;
				public float Timer;
				public float Percent;
				public float Duration;
				public float Alpha;
				public float Approach;
			}


			private static readonly Color[] startColors = new Color[2]
			{
				Calc.HexToColor("99e550"),
				Calc.HexToColor("6abe30"),
			};
			private static readonly Color[] checkpointColors = new Color[2]
			{
				Calc.HexToColor("beba27"),
				Calc.HexToColor("938f1f"),
			};
			private static readonly Color[] finishColors = new Color[2]
			{
				Calc.HexToColor("111111"),
				Calc.HexToColor("eeeeee"),
			};

			private Particle[] particles = new Particle[30];

			public ConfettiRenderer(Vector2 position, bool isStart, bool isFinish)
				: base(position) {
				Depth = -10010;
				for (int i = 0; i < particles.Length; i++) {
					particles[i].Position = Position + new Vector2(Calc.Random.Range(-3, 3), Calc.Random.Range(-3, 3));
					particles[i].Color = Calc.Random.Choose(isStart ? startColors : isFinish ? finishColors : checkpointColors);
					particles[i].Timer = Calc.Random.NextFloat();
					particles[i].Duration = Calc.Random.Range(2, 4);
					particles[i].Alpha = 1f;
					float angleRadians = -(float)Math.PI / 2f + Calc.Random.Range(-0.5f, 0.5f);
					int num = Calc.Random.Range(140, 220);
					particles[i].Speed = Calc.AngleToVector(angleRadians, num);
				}
			}

			public override void Update() {
				for (int i = 0; i < particles.Length; i++) {
					particles[i].Position += particles[i].Speed * Engine.DeltaTime;
					particles[i].Speed.X = Calc.Approach(particles[i].Speed.X, 0f, 80f * Engine.DeltaTime);
					particles[i].Speed.Y = Calc.Approach(particles[i].Speed.Y, 20f, 500f * Engine.DeltaTime);
					particles[i].Timer += Engine.DeltaTime;
					particles[i].Percent += Engine.DeltaTime / particles[i].Duration;
					particles[i].Alpha = Calc.ClampedMap(particles[i].Percent, 0.9f, 1f, 1f, 0f);
					if (particles[i].Speed.Y > 0f) {
						particles[i].Approach = Calc.Approach(particles[i].Approach, 5f, Engine.DeltaTime * 16f);
					}
				}
			}

			public override void Render() {
				for (int i = 0; i < particles.Length; i++) {
					float num = 0f;
					Vector2 position = particles[i].Position;
					if (particles[i].Speed.Y < 0f) {
						num = particles[i].Speed.Angle();
					}
					else {
						num = (float)Math.Sin(particles[i].Timer * 4f) * 1f;
						position += Calc.AngleToVector((float)Math.PI / 2f + num, particles[i].Approach);
					}
					GFX.Game["particles/confetti"].DrawCentered(position + Vector2.UnitY, Color.Black * (particles[i].Alpha * 0.5f), 1f, num);
					GFX.Game["particles/confetti"].DrawCentered(position, particles[i].Color * particles[i].Alpha, 1f, num);
				}
			}
		}


		private int width;
		private int height;
		//private Vector2 SpritePosition;
		private Sprite sprite;
		private bool isStart;
		private bool isFinish;
		private int checkpointNumber;
		private Trigger trigger;

		private Vector2 flagBasePosition { get { return new Vector2(width / 2f, height); } }

		public TimeTrialCheckpoint(EntityData data, Vector2 offset) : base(data.Position + offset) {
			width = data.Width;
			height = data.Height;
			isStart = data.Bool("isStart", false);
			isFinish = data.Bool("isFinish", false);
			checkpointNumber = data.Int("checkpointNumber", 1);
			Collider = new Hitbox(width, height);
			string spriteStr = isStart ? "Head2Head_TimeTrial_Start"
				: isFinish ? "Head2Head_TimeTrial_Finish"
				: "Head2Head_TimeTrial_Checkpoint";
			Add(sprite = GFX.SpriteBank.Create(spriteStr));
			sprite.Position = flagBasePosition;
		}

		public override void Added(Scene scene) {
			base.Added(scene);
			Scene.Add(trigger = new TimeTrialCheckpointTrigger(Position, width, height, Enter, Leave));
			UpdateSprite();
		}

		public void UpdateSprite() {
			if (isStart) {
				if (PlayerStatus.Current.InTimeTrial) sprite.Play("raise");
				else sprite.Play("idle");
			}
			else if (isFinish) {
				if (PlayerStatus.Current.lobbyTimer > 0 && !PlayerStatus.Current.InTimeTrial) sprite.Play("raise");
				else sprite.Play("idle");
			}
			else {
				if (PlayerStatus.Current.lobbyCP >= checkpointNumber) sprite.Play("raise");
				else sprite.Play("idle");
			}
		}

		public override void Removed(Scene scene) {
			base.Removed(scene);
			trigger?.RemoveSelf();
			trigger = null;
		}

		private void Enter(Player player) {
			if (isStart) {
				Scene.Add(new ConfettiRenderer(Position + flagBasePosition, isStart, isFinish));
				Audio.Play("event:/game/07_summit/checkpoint_confetti", Position);
				PlayerStatus.Current.StartLobbyRace();
				foreach (TimeTrialCheckpoint ttc in Scene.Tracker.GetEntities<TimeTrialCheckpoint>()) {
					ttc.UpdateSprite();
				}
			}
			else if (PlayerStatus.Current.RunningLobbyRace) {
				if (checkpointNumber == PlayerStatus.Current.lobbyCP + 1) {
					Audio.Play("event:/game/07_summit/checkpoint_confetti", Position);
					PlayerStatus.Current.lobbyCP = checkpointNumber;
					if (isFinish) {
						PlayerStatus.Current.FinishLobbyRace();
						Scene.Add(new ConfettiRenderer(Position + flagBasePosition, isStart, isFinish));
					}
					else {
						Scene.Add(new ConfettiRenderer(Position + flagBasePosition, isStart, isFinish));
					}
					foreach (TimeTrialCheckpoint ttc in Scene.Tracker.GetEntities<TimeTrialCheckpoint>()) {
						ttc.UpdateSprite();
					}
				}
			}
		}

		private void Leave(Player player) {
			if (isStart && !PlayerStatus.Current.RunningLobbyRace) {
				PlayerStatus.Current.StartLobbyRaceTimer();
				UpdateSprite();
			}
		}
	}

	public class TimeTrialCheckpointTrigger : Trigger {
		public Action<Player> Enter;
		public Action<Player> Leave;

		public TimeTrialCheckpointTrigger(Vector2 position, int width, int height, Action<Player> enter, Action<Player> leave)
			: base(new EntityData() { Width = width, Height = height }, position)
		{
			Enter = enter;
			Leave = leave;
		}

		public override void OnEnter(Player player) {
			base.OnEnter(player);
			Enter(player);
		}

		public override void OnLeave(Player player) {
			base.OnLeave(player);
			Leave(player);
		}
	}
}
