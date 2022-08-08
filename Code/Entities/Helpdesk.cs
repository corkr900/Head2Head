using Celeste.Mod.Entities;
using Celeste.Mod.Head2Head.Shared;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.Entities
{
	[CustomEntity("Head2Head/Helpdesk")]
	public class Helpdesk : Entity
	{
		private Sprite sprite;

		public Helpdesk(EntityData data, Vector2 offset)
		{
			//map = data.Attr("map");
			Position = data.Position + offset;
			Add(sprite = GFX.SpriteBank.Create("Head2Head_Helpdesk"));
			sprite.Play("idle");
			sprite.Position = new Vector2(-8, -16);
			Add(new TalkComponent(
				new Rectangle(-16, -16, 32, 32),
				new Vector2(0, -16),
				(Player player) => {
					new Menus.HelpdeskMenuContext(player.SceneAs<Level>(), 0, false).GoTo(Menus.Helpdesk, null);
				}
			)
			{ PlayerMustBeFacing = false });
		}
	}
}
