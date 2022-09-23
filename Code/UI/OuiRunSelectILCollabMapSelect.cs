using Celeste.Mod.Head2Head.Entities;
using Celeste.Mod.Head2Head.Integration;
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
	class OuiRunSelectILCollabMapSelect : Oui {

		public class CollabMap : Entity {
			private float EnterEaseTime { get { return 0.3f; } }
			private float SelectEaseTime { get { return 0.15f; } }
			private float SelectedXOffset { get { return 60f; } }
			private float IconSize { get { return 90f; } }

			public static CollabMap Add(Scene scene, int localID, float yPos) {
				AreaData data = AreaData.Get(localID);
				string sid = data.SID;
				CollabMap map = new CollabMap() {
					LocalID = localID,
					Title = Dialog.Clean(data.Name),
					Icon = string.IsNullOrEmpty(data.Icon) ? null : GFX.Gui.Has(data.Icon) ? GFX.Gui[data.Icon] : null,
					YPosition = yPos,
					Tag = Tags.HUD,
				};
				scene.Add(map);
				return map;
			}

			public int LocalID { get; private set; }
			public string Title { get; private set; }
			public MTexture Icon { get; private set; }
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

		private static MTexture Pointer { get { return MTN.Journal["poemArrow"]; } }
		private static float YPosBase { get { return 200f; } }
		private static float YPosStep { get { return 90f; } }
		private static Dictionary<string, List<CollabMap>> maps { get; set; } = new Dictionary<string, List<CollabMap>>();


		private bool entering = false;

		public override bool IsStart(Overworld overworld, Overworld.StartMode start) => false;

		public override void Added(Scene scene) {
			base.Added(scene);
			// Get all the lobbies
			maps.Clear();
			int count = AreaData.Areas.Count;
			for (int i = 0; i < count; i++) {
				AreaData data = AreaData.Areas[i];
				string collabSet = CollabUtils2Integration.GetLobbyLevelSet(data.SID);
				if (string.IsNullOrEmpty(collabSet)) continue;  // Not a collab lobby
				if (!maps.ContainsKey(data.SID)) {
					maps.Add(data.SID, new List<CollabMap>());
				}
			}
			// Get all the maps
			for (int i = 0; i < count; i++) {
				AreaData data = AreaData.Areas[i];
				string set = data.LevelSet;
				if (!CollabUtils2Integration.IsCollabLevelSet(set)) continue;  // Not a collab lobby
				string lobby = CollabUtils2Integration.GetLobbyForLevelSet(set);
				if (!maps.ContainsKey(lobby)) continue;
				maps[lobby].Add(CollabMap.Add(scene, i, YPosBase + maps[lobby].Count * YPosStep));
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
			if (maps.ContainsKey(ILSelector.LastArea.SID)) {
				foreach (CollabMap map in maps[ILSelector.LastArea.SID]) {
					map.Show = true;
					yield return 0.02f;
				}
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
			if (maps.ContainsKey(ILSelector.LastArea.SID)) {
				foreach (CollabMap map in maps[ILSelector.LastArea.SID]) {
					map.Show = false;
					yield return 0.01f;
				}
			}
		}

		public override void Update() {
			base.Update();
			if (Focused) {
				if (Input.MenuCancel.Pressed) {
					Audio.Play("event:/ui/world_map/chapter/back");
					//ILSelector.LastArea = new GlobalAreaKey(CollabUtils2Integration.GetLobbyForLevelSet(ILSelector.LastArea.Data.LevelSet), AreaMode.Normal);
					Overworld.Goto<OuiRunSelectILChapterSelect>();
				}
				else if (Input.MenuConfirm.Pressed) {
					Audio.Play("event:/ui/world_map/icon/select");
					ILSelector.LastArea = new GlobalAreaKey(maps[ILSelector.LastArea.SID][0].LocalID, AreaMode.Normal);
					Overworld.Goto<OuiRunSelectILChapterPanel>();
				}
			}
		}
	}
}
