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
	public class TimeTrialCheckpoint : Entity {
		private int width;
		private int height;
		private Vector2 SpritePosition;
		private Sprite sprite;
		private bool isStart;
		private bool isFinish;
		private int checkpointNumber;
		private Trigger trigger;
		private bool triggered;

		public TimeTrialCheckpoint(EntityData data, Vector2 offset) : base(data.Position + offset) {
			width = data.Width;
			height = data.Height;
			isStart = data.Bool("isStart", false);
			isFinish = data.Bool("isFinish", false);
			checkpointNumber = data.Int("checkpointNumber", 1);
			if (data.Nodes.Length > 0) {
				SpritePosition = data.Nodes[0] + offset;
			}
			else {
				SpritePosition = Position + new Vector2(width / 2f, height);
			}
			Collider = new Hitbox(width, height);
			Add(sprite = GFX.SpriteBank.Create("litTorch"));
			sprite.Position = SpritePosition;
		}

		public override void Added(Scene scene) {
			base.Added(scene);
			Scene.Add(trigger = new TimeTrialCheckpointTrigger(Position, width, height, Enter, Leave));
		}

		public override void Removed(Scene scene) {
			base.Removed(scene);
			trigger?.RemoveSelf();
			trigger = null;
		}

		private void Enter(Player player) {
			if (PlayerStatus.Current.RunningLobbyRace) {
				if (checkpointNumber == PlayerStatus.Current.lobbyCP + 1) {
					triggered = true;
					PlayerStatus.Current.lobbyCP = checkpointNumber;
					if (isFinish) {
						PlayerStatus.Current.FinishLobbyRace();
					}
				}
			}
			else if (isStart) {
				PlayerStatus.Current.StartLobbyRace();
			}
		}

		private void Leave(Player player) {
			if (isStart && !PlayerStatus.Current.RunningLobbyRace) {
				PlayerStatus.Current.StartLobbyRaceTimer();
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
