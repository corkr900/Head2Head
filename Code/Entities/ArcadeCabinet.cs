using Celeste.Mod.Entities;
using Celeste.Mod.Head2Head.Entities.Arcade;
using Celeste.Mod.Head2Head.UI;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.Entities {
	[CustomEntity("Head2Head/ArcadeCabinet")]
	public class ArcadeCabinet : Entity {
		private Sprite sprite;
		private TalkComponent talkComponent;
		private Player player;
		private ArcadeHUD hud;

		public ArcadeCabinet(EntityData data, Vector2 offset) {
			Position = data.Position + offset;
			Add(sprite = GFX.SpriteBank.Create("Head2Head_ArcadeCabinet"));
			sprite.Play("FourInARow");
			sprite.Position = new Vector2(-8, -16);
			Add(talkComponent = new TalkComponent(new Rectangle(-16, -16, 32, 32), new Vector2(0, -16), OpenUI) { PlayerMustBeFacing = false });
		}

		private void OpenUI(Player player) {
			this.player = player;
			player.StateMachine.State = Player.StDummy;
			player.SceneAs<Level>().PauseLock = true;
			talkComponent.Enabled = false;
			hud = new FourInARowHUD();
			hud.OnClose += CloseUI;
			Scene.Add(hud);
			Audio.Play("event:/ui/world_map/icon/select");
		}

		public void CloseUI() {
			hud = null;
			player.StateMachine.State = Player.StNormal;
			player.SceneAs<Level>().PauseLock = false;
			talkComponent.Enabled = true;
			Input.Dash.ConsumeBuffer();
			Input.CrouchDash.ConsumeBuffer();
			Input.Jump.ConsumeBuffer();
		}
	}
}
