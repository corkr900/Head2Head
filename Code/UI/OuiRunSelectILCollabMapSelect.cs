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
			internal static float EnterEaseTime { get { return 0.3f; } }
			internal static float SelectEaseTime { get { return 0.15f; } }
			internal static float SelectedXOffset { get { return 60f; } }
			internal static float IconSize { get { return 90f; } }
			internal static float Margin { get { return 15f; } }

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
			public float Scroll { get; internal set; } = 0;

			private float EnterEase = 0;
			private float SelectEase = 0;
			private Wiggler IconWiggler;
			private float iconRotation = 0;

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

				IconWiggler?.Update();
				EnterEase = Calc.Approach(EnterEase, Show ? 1 : 0, Engine.DeltaTime / EnterEaseTime);
				SelectEase = Calc.Approach(SelectEase, Hovered ? 1 : 0, Engine.DeltaTime / SelectEaseTime);
			}

			public override void Render() {
				base.Render();
				if (!Show && EnterEase <= 0) return;

				Vector2 size = Size;
				float xmin = -size.X;
				float xmax = Margin;
				float xpos = Calc.LerpClamp(xmin, xmax, Ease.CubeIn(EnterEase));
				xpos += Calc.LerpClamp(0, SelectedXOffset + Margin, Ease.CubeInOut(SelectEase));

				if (Icon != null) {
					Icon.DrawJustified(new Vector2(xpos + IconSize/2f, YPosition - Scroll), new Vector2(0.5f, 0.5f), Color.White, 0.5f, iconRotation);
					xpos += IconSize + Margin;
				}
				ActiveFont.DrawOutline(Title, new Vector2(xpos, YPosition - Scroll), new Vector2(0, 0.5f), Vector2.One, Color.White, 5f, Color.Black);
			}

			public void Wiggle() {
				if (IconWiggler == null) {
					IconWiggler = Wiggler.Create(0.4f, 4f, (float f) => {
						iconRotation = f * 0.3f;
					});
				}
				IconWiggler.StopAndClear();
				IconWiggler.Start();
			}
		}

		public class Pointer : Entity {
			public bool Shown { get; set; } = false;
			public float Scroll { get; internal set; } = 0;

			private static MTexture Texture { get { return MTN.Journal["poemArrow"]; } }

			public Vector2 PosBase { get; private set; }
			public Vector2 PosTarget { get; private set; }
			private float poslerp;
			public Vector2 PosActual { get; private set; }

			public void SetInitialTarget(Vector2 target) {
				PosActual = PosBase = PosTarget = target;
				poslerp = 1;
			}

			public void SetNewTarget(Vector2 target) {

				PosBase = PosActual;
				PosTarget = target;
				poslerp = 0;
			}

			public override void Update() {
				base.Update();

				poslerp = Calc.Approach(poslerp, 1, Engine.DeltaTime / CollabMap.SelectEaseTime);
				PosActual = new Vector2(
					Calc.LerpClamp(PosBase.X, PosTarget.X, Ease.CubeInOut(poslerp)),
					Calc.LerpClamp(PosBase.Y, PosTarget.Y, Ease.CubeInOut(poslerp)));
			}

			public override void Render(){
				base.Render();

				if (Shown) {
					Texture.DrawJustified(PosActual + new Vector2(0, -Scroll), new Vector2(0, 0.5f));
				}
			}
		}

		private static float YPosBase { get { return 135f; } }
		private static float YPosStep { get { return 90f; } }
		private static Dictionary<string, List<CollabMap>> maps { get; set; } = new Dictionary<string, List<CollabMap>>();

		private Pointer pointer;
		private int hovered = 0;
		private float scrollBase;
		private float scrollTarget;
		private float scrollLerp;
		private float scrollCurrent;

		public override bool IsStart(Overworld overworld, Overworld.StartMode start) => false;

		public override void Added(Scene scene) {
			base.Added(scene);

			// Misc
			pointer = new Pointer();
			pointer.Shown = false;
			pointer.Tag = Tags.HUD;
			scene.Add(pointer);
			scrollBase = scrollTarget = scrollTarget = 0;
			scrollLerp = 1;

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
				if (string.IsNullOrEmpty(lobby) || !maps.ContainsKey(lobby)) continue;
				maps[lobby].Add(CollabMap.Add(scene, i, YPosBase + maps[lobby].Count * YPosStep));
			}
		}

		public override IEnumerator Enter(Oui from) {
			foreach (KeyValuePair<string, List<CollabMap>> kvp in maps) {
				foreach (CollabMap map in kvp.Value) {
					map.Show = false;
					map.Hovered = false;
					map.Enabled = kvp.Key == kvp.Key;
				}
			}
			if (maps.ContainsKey(ILSelector.LastArea.SID)) {
				hovered = Calc.Clamp(hovered, 0, maps[ILSelector.LastArea.SID].Count - 1);
				foreach (CollabMap map in maps[ILSelector.LastArea.SID]) {
					map.Enabled = true;
					map.Show = true;
					map.Scroll = scrollCurrent;
					if (maps[ILSelector.LastArea.SID].IndexOf(map) == hovered) {  // TODO this makes an n^2 runtime (bad)
						SetInitialHover(map);
					}
					yield return 0.02f;
				}
			}
		}

		public override IEnumerator Leave(Oui next) {
			pointer.Shown = false;
			foreach (KeyValuePair<string, List<CollabMap>> kvp in maps) {
				foreach (CollabMap map in kvp.Value) {
					map.Enabled = false;
				}
			}
			foreach (KeyValuePair<string, List<CollabMap>> kvp in maps) {
				foreach (CollabMap map in kvp.Value) {
					if (!map.Show) continue;
					map.Show = false;
					yield return 0.01f;
				}
			}
		}

		public override void Update() {
			base.Update();

			if (Focused) {
				scrollLerp = Calc.Approach(scrollLerp, 1, Engine.DeltaTime / CollabMap.SelectEaseTime);
				scrollCurrent = Calc.LerpClamp(scrollBase, scrollTarget, Ease.CubeInOut(scrollLerp));
				pointer.Scroll = scrollCurrent;
				foreach (CollabMap map in maps[ILSelector.LastArea.SID]) {
					map.Scroll = scrollCurrent;
				}

				if (Input.MenuUp.Pressed && hovered > 0) {
					Audio.Play("event:/ui/world_map/chapter/tab_roll_left");
					SetHover(hovered - 1);
				}
				else if (Input.MenuDown.Pressed && hovered < maps[ILSelector.LastArea.SID].Count - 1) {
					Audio.Play("event:/ui/world_map/chapter/tab_roll_right");
					SetHover(hovered + 1);
				}
				else if (Input.MenuCancel.Pressed) {
					Audio.Play("event:/ui/world_map/chapter/back");
					Overworld.Goto<OuiRunSelectILChapterSelect>();
				}
				else if (Input.MenuConfirm.Pressed) {
					Audio.Play("event:/ui/world_map/icon/select");
					ILSelector.LastArea = new GlobalAreaKey(maps[ILSelector.LastArea.SID][hovered].LocalID, AreaMode.Normal);
					Overworld.Goto<OuiRunSelectILChapterPanel>();
				}
			}
		}

		private void SetInitialHover(CollabMap map) {
			map.Hovered = true;
			pointer.SetInitialTarget(new Vector2(CollabMap.Margin, map.YPosition));
			pointer.Shown = true;
			scrollBase = scrollCurrent = scrollTarget = 0;
			scrollBase = scrollCurrent = scrollTarget = GetScrollTarget(map);
			scrollLerp = 1;
		}

		private void SetHover(int newHover) {
			List<CollabMap> list = maps[ILSelector.LastArea.SID];
			list[hovered].Hovered = false;
			hovered = newHover;
			list[hovered].Hovered = true;
			pointer.SetNewTarget(new Vector2(CollabMap.Margin, list[hovered].YPosition));
			scrollBase = scrollCurrent;
			scrollTarget = GetScrollTarget(list[hovered]);
			scrollLerp = 0;
			list[hovered].Wiggle();
		}

		private float GetScrollTarget(CollabMap map) {
			const float screenHeight = 1080f;
			const float deadZoneMin = screenHeight / 3f;
			const float deadZoneMax = screenHeight * 2f / 3f;

			float currentPtrPos = pointer.PosTarget.Y - scrollTarget;
			if (currentPtrPos >= deadZoneMin && currentPtrPos <= deadZoneMax) {  // Current targets are with deadzone; this is fine.
				return scrollTarget;
			}

			float regionHeight = YPosBase * 2 + YPosStep * maps[ILSelector.LastArea.SID].Count;
			float minScroll = 0;
			float maxScroll = Calc.Clamp(regionHeight - screenHeight, 0, float.MaxValue);

			float scrollChange = currentPtrPos <= deadZoneMin ? currentPtrPos - deadZoneMin : currentPtrPos - deadZoneMax;
			return Calc.Clamp(scrollTarget + scrollChange, minScroll, maxScroll);
		}
	}
}
