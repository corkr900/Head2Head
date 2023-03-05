using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.Entities {
	[CustomEntity("Head2Head/FullgameSelector")]
	public class FullgameSelector : Entity {
		private Player player;
		private Sprite sprite;
		private TalkComponent talkComponent;

		public FullgameSelector(EntityData data, Vector2 offset) {
			Position = data.Position + offset;
			Add(sprite = GFX.SpriteBank.Create("Head2Head_FullgameSelector"));
			sprite.Play("idle");
			sprite.Position = new Vector2(-8, -16);
			Add(talkComponent = new TalkComponent(
				new Rectangle(-16, -16, 32, 32),
				new Vector2(0, -16),
				(Player player) => {
					OpenUI(player);
				}
			) { PlayerMustBeFacing = false });
		}

		public override void Added(Scene scene) {
			base.Added(scene);
			UpdateEnabledState();
			Head2HeadModule.OnMatchCurrentMatchUpdated += UpdateEnabledState;
		}

		public override void Removed(Scene scene) {
			base.Removed(scene);
			Head2HeadModule.OnMatchCurrentMatchUpdated -= UpdateEnabledState;
		}

		private void UpdateEnabledState() {
			talkComponent.Enabled = Head2HeadModule.Instance.CanBuildFullgameMatch();
		}

		private void OpenUI(Player player) {
			this.player = player;
			player.StateMachine.State = Player.StDummy;
			player.SceneAs<Level>().PauseLock = true;

			FullgameSelectorUI ui = new FullgameSelectorUI();
			ui.OnRemove += CloseUI;
			Scene.Add(ui);
			Audio.Play("event:/ui/world_map/icon/select");
		}

		public void CloseUI() {
			player.StateMachine.State = Player.StNormal;
			player.SceneAs<Level>().PauseLock = false;
		}
	}
}
