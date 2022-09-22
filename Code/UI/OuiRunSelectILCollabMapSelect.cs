using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.UI {
	class OuiRunSelectILCollabMapSelect : Oui {

		public class CollabMap : Entity {
			private float EnterEaseTime { get { return 0.4f; } }
			private float SelectEaseTime { get { return 0.2f; } }
			private float SelectedXOffset { get { return 60f; } }
			private float IconSize { get { return 90f; } }

			public static CollabMap Add(Scene scene, string sid, float yPos) {
				AreaData data = AreaData.Get(sid);
				CollabMap map = new CollabMap() {
					Title = data.Name,
					Icon = string.IsNullOrEmpty(data.Icon) ? null : GFX.Gui.Has(data.Icon) ? GFX.Gui[data.Icon] : null,
					YPosition = yPos,
					Tag = Tags.HUD,
				};
				scene.Add(map);
				return map;
			}

			public string Title { get; set; }
			public MTexture Icon { get; set; }
			public float YPosition { get; set; }
			public bool Show { get; set; } = false;
			public bool Hovered { get; set; } = false;
			public bool Enabled { get; set; } = false;

			private float EnterEase = 0;
			private float SelectEase = 0;

			public Vector2 Size {
				get {
					Vector2 size = ActiveFont.Measure(Title);
					size.X += SelectedXOffset + IconSize;
					size.Y = Calc.Max(size.Y, IconSize);
					return size;
				}
			}

			public override void Update() {
				base.Update();

				EnterEase = Calc.Approach(EnterEase, Show ? 1 : 0, Engine.DeltaTime / EnterEaseTime);
				SelectEase = Calc.Approach(SelectEase, Hovered ? 1 : 0, Engine.DeltaTime / SelectEaseTime);
			}

			public override void Render() {
				base.Render();
				if (!Show && EnterEase <= 0) return;

				Vector2 size = Size;
				float xmin = -size.X;
				float xmax = 0;
				float xpos = Calc.LerpClamp(xmin, xmax, Ease.CubeIn(EnterEase));
				xpos += Calc.LerpClamp(0, SelectedXOffset, Ease.CubeIn(SelectEase));

				if (Icon != null) {
					Icon.DrawJustified(new Vector2(xpos, YPosition), new Vector2(0, 0.5f), Color.White, 0.5f);
					xpos += IconSize;
				}
				ActiveFont.DrawOutline(Title, new Vector2(xpos, YPosition), new Vector2(0, 0.5f), Vector2.One, Color.White, 2f, Color.Black);
			}
		}
		private MTexture Pointer { get { return MTN.Journal["poemArrow"]; } }
		private float YPosBase { get { return 200f; } }
		private float YPosStep { get { return 90f; } }

		private string levelSet;
		private Dictionary<string, List<CollabMap>> maps = new Dictionary<string, List<CollabMap>>();

		private bool entering = false;

		public override bool IsStart(Overworld overworld, Overworld.StartMode start) => false;

		public override void Added(Scene scene) {
			base.Added(scene);

			int count = AreaData.Areas.Count;
			float ypos = YPosBase;
			string lastSet = "";
			for (int i = 0; i < count; i++) {
				AreaData data = AreaData.Areas[i];
				string set = data.LevelSet;
				if (!maps.ContainsKey(set)) {
					maps.Add(set, new List<CollabMap>());
				}
				if (lastSet != set) ypos = YPosBase;
				else ypos += YPosStep;
				maps[set].Add(CollabMap.Add(scene, data.SID, ypos));
				lastSet = set;
			}
		}

		public override IEnumerator Enter(Oui from) {
			entering = true;
			foreach (KeyValuePair<string, List<CollabMap>> kvp in maps) {
				foreach (CollabMap map in kvp.Value) {
					map.Show = false;
					map.Enabled = kvp.Key == kvp.Key;
				}
			}

			foreach (CollabMap map in maps[levelSet]) {
				map.Show = true;
				yield return 0.05f;
			}
			entering = false;
		}

		public override IEnumerator Leave(Oui next) {
			while (entering) { yield return null; }
			foreach (KeyValuePair<string, List<CollabMap>> kvp in maps) {
				foreach (CollabMap map in kvp.Value) {
					map.Enabled = false;
				}
			}
			foreach (CollabMap map in maps[levelSet]) {
				map.Show = false;
				yield return 0.05f;
			}
		}

		// TODO (!!!) Navigation
	}
}
