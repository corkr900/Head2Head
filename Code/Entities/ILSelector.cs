using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.Utils;
using System.Collections;
using System.Reflection;
using Celeste.Mod.UI;
using Celeste.Mod.Head2Head.UI;
using Celeste.Mod.Head2Head.Shared;
using FMOD.Studio;
using Celeste.Mod.Head2Head.IO;

namespace Celeste.Mod.Head2Head.Entities {
	[CustomEntity("Head2Head/ILSelector")]
	public class ILSelector : Entity {

		public static GlobalAreaKey LastArea = GlobalAreaKey.VanillaPrologue;

		private SceneWrappingEntity<Overworld> overworldWrapper;

		public static ILSelector ActiveSelector { get; private set; } = null;
		public GlobalAreaKey Area;
		public StandardCategory Category;

		private Sprite sprite;
		private TalkComponent talkComponent;

		public ILSelector(EntityData data, Vector2 offset) {
			//map = data.Attr("map");
			Position = data.Position + offset;
			Add(sprite = GFX.SpriteBank.Create("Head2Head_ILSelector"));
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
			talkComponent.Enabled = Head2HeadModule.Instance.CanBuildMatch();
		}

		private void OpenUI(Player player) {
			if (ActiveSelector != null) return;
			Level l = player.Scene as Level;
			if (l != null) l.PauseLock = true;
			ActiveSelector = this;
			Area = GlobalAreaKey.Overworld;
			Category = StandardCategory.Clear;

			player.StateMachine.State = Player.StDummy;
			OuiRunSelectIL.Start = true;
			overworldWrapper = new SceneWrappingEntity<Overworld>(new Overworld(new OverworldLoader((Overworld.StartMode)(-1),
				new HiresSnow() {
					Alpha = 0f,
					ParticleAlpha = 0.25f,
				}
			)));
			overworldWrapper.OnBegin += (overworld) => {
				overworld.RendererList.Remove(overworld.RendererList.Renderers.Find(r => r is MountainRenderer));
				overworld.RendererList.Remove(overworld.RendererList.Renderers.Find(r => r is ScreenWipe));
				overworld.RendererList.UpdateLists();
			};
			overworldWrapper.OnEnd += (overworld) => {
				if (overworldWrapper?.WrappedScene == overworld) {
					overworldWrapper = null;
				}
			};
			Scene.Add(overworldWrapper);
			overworldWrapper.Add(new Coroutine(UpdateRoutine(overworldWrapper)));
		}

		private IEnumerator UpdateRoutine(SceneWrappingEntity<Overworld> wrapper) {
			Level level = wrapper.Scene as Level;
			Overworld overworld = wrapper.WrappedScene;

			while (overworldWrapper?.Scene == Engine.Scene) {
				if (overworld.Next is OuiRunSelectILExit) {
					overworld.Next.RemoveSelf();
					overworldWrapper.Add(new Coroutine(DelayedCloseRoutine(level)));
				}

				overworld.Snow.ParticleAlpha = 0.25f;

				if (overworld.Current != null || overworld.Next?.Scene != null) {
					overworld.Snow.Alpha = Calc.Approach(overworld.Snow.Alpha, 1f, Engine.DeltaTime * 2f);

				}
				else {
					overworld.Snow.Alpha = Calc.Approach(overworld.Snow.Alpha, 0, Engine.DeltaTime * 2f);
					if (overworld.Snow.Alpha <= 0.01f) {
						Close(level, true, true);
					}
				}

				yield return null;
			}

			if (wrapper.Scene != null) {
				wrapper.RemoveSelf();
			}
		}

		private IEnumerator DelayedCloseRoutine(Level level) {
			yield return null;
			Close(level, false, true);
		}

		public void Close(Level level, bool removeScene, bool resetPlayer) {
			ActiveSelector = null;
			level.PauseLock = false;
			if (removeScene) {
				overworldWrapper?.WrappedScene?.Entities.UpdateLists();
				overworldWrapper?.RemoveSelf();
			}

			if (resetPlayer) {
				Player player = level.Tracker.GetEntity<Player>();
				if (player != null && player.StateMachine.State == Player.StDummy) {
					Engine.Scene.OnEndOfFrame += () => {
						player.StateMachine.State = Player.StNormal;
					};
				}
			}

			if (!Area.IsOverworld) {
				Head2HeadModule.Instance.AddMatchPhase(Category, Area);
				Area = GlobalAreaKey.Overworld;
				Category = StandardCategory.Clear;
				Head2HeadModule.Instance.StageMatch();
			}
		}

		internal static void OnPause(On.Celeste.Level.orig_Pause orig, Level self, int startIndex, bool minimal, bool quickReset) {
			if (ActiveSelector != null) {
				ActiveSelector.Area = GlobalAreaKey.Overworld;
				ActiveSelector.Category = StandardCategory.Clear;
				ActiveSelector.Close(self, true, true);
			}
		}

		internal static void OnMapSearchCleanExit(On.Celeste.Mod.UI.OuiMapSearch.orig_cleanExit orig, OuiMapSearch self) {
			if (ActiveSelector == null) {
				orig(self);
			}
			else {
				DynamicData dd = new DynamicData(self);
				dd.Invoke("clearSearch");
				if (self.FromChapterSelect) {
					Audio.Play("event:/ui/main/button_back");
					self.Overworld.Goto<OuiRunSelectILChapterSelect>();
				}
				else {
					self.Overworld.Goto<OuiMapList>();
				}
			}
		}

		internal static void OnMapSearchInspect(On.Celeste.Mod.UI.OuiMapSearch.orig_Inspect orig, OuiMapSearch self, AreaData area, AreaMode mode) {
			if (ActiveSelector == null) {
				orig(self, area, mode);
			}
			else {
				self.Focused = false;
				Audio.Play("event:/ui/world_map/icon/select");
				LastArea = new GlobalAreaKey(area.ToKey(mode));
				if (self.OuiIcons != null && area.ID < self.OuiIcons.Count) {
					self.OuiIcons[area.ID].Select();
				}
				self.Overworld.Mountain.Model.EaseState(area.MountainState);
				self.Overworld.Goto<OuiRunSelectILChapterSelect>();
			}
		}

		internal static void OnMapListUpdate(On.Celeste.Mod.UI.OuiMapList.orig_Update orig, OuiMapList self) {
			if (ActiveSelector == null) {
				orig(self);
				return;
			}
			DynamicData dd = new DynamicData(self);
			TextMenu menu = dd.Get<TextMenu>("menu");
			bool goingToChapSelect = false;
			if (menu != null && menu.Focused && self.Selected) {
				self.Overworld.Maddy.Show = false;
				if (Input.MenuCancel.Pressed || Input.Pause.Pressed || Input.ESC.Pressed) {
					Audio.Play("event:/ui/main/button_back");
					self.Overworld.Goto<OuiRunSelectILChapterSelect>();
					goingToChapSelect = true;
				}
			}
			if (!goingToChapSelect) {
				orig(self);
			}
		}

		internal static void OnMapListInspect(On.Celeste.Mod.UI.OuiMapList.orig_Inspect orig, OuiMapList self, AreaData area, AreaMode mode) {
			if (ActiveSelector == null) {
				orig(self, area, mode);
				return;
			}
			DynamicData dd = new DynamicData(self);
			self.Focused = false;
			Audio.Play("event:/ui/world_map/icon/select");
			LastArea = new GlobalAreaKey(area.ToKey(mode));
			if (self.OuiIcons != null && area.ID < self.OuiIcons.Count) {
				self.OuiIcons[area.ID].Select();
			}
			self.Overworld.Mountain.Model.EaseState(area.MountainState);
			self.Overworld.Goto<OuiRunSelectILChapterPanel>();
		}
	}
}
