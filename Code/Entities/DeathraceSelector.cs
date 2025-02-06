using Celeste.Mod.Entities;
using Celeste.Mod.Head2Head.UI;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.Entities {
	[CustomEntity("Head2Head/DeathracePicker")]
	internal class DeathraceSelector : Entity {

		private Sprite sprite;
		private TalkComponent talkComponent;
		//private Player player;

		public DeathraceSelector(EntityData data, Vector2 offset) {
			Position = data.Position + offset;
			Add(sprite = GFX.SpriteBank.Create("Head2Head_DeathraceSelector"));
			sprite.Play("idle");
			sprite.Position = new Vector2(-8, -16);
			Add(talkComponent = new TalkComponent(
				new Rectangle(-16, -16, 32, 32),
				new Vector2(0, -16),
				OpenUI
			) { PlayerMustBeFacing = false });
		}

		private void OpenUI(Player p) {
			// TODO
		}
	}
}
