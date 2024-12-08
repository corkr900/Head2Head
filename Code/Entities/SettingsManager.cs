using Celeste.Mod.Entities;
using Monocle;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Celeste.Mod.Head2Head.UI;

namespace Celeste.Mod.Head2Head.Entities {
	[CustomEntity("Head2Head/SettingsManager")]
	internal class SettingsManager : Entity {

		private Sprite sprite;
		private TalkComponent talkComponent;
		//private Player player;

		public SettingsManager(EntityData data, Vector2 offset) {
			//map = data.Attr("map");
			Position = data.Position + offset;
			Add(sprite = GFX.SpriteBank.Create("Head2Head_SettingsManager"));
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

		private void OpenUI(Player p) {
			//player = p;
			//p.StateMachine.State = Player.StDummy;
			//p.SceneAs<Level>().PauseLock = true;

			new Menus.HelpdeskMenuContext(p.SceneAs<Level>(), 0, false).GoTo(Menus.SettingsManager, null);
		}
	}
}
