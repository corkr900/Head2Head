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
using Celeste.Mod.Head2Head.Integration;
using Celeste.Mod.CelesteNet;

namespace Celeste.Mod.Head2Head.Entities {
	[CustomEntity("Head2Head/ILSelector")]
	public class ILSelector : Entity {

		public static RunOptionsILCategory ChosenCategory = null;

		public static Dictionary<GlobalAreaKey, List<StandardCategory>> SuppressedCategories = new Dictionary<GlobalAreaKey, List<StandardCategory>>();
		public static void SuppressCategory(GlobalAreaKey area, params StandardCategory[] cats) {
			if (!SuppressedCategories.ContainsKey(area)) SuppressedCategories.Add(area, new List<StandardCategory>());
			foreach (StandardCategory cat in cats) {
				if (!SuppressedCategories[area].Contains(cat)) {
					SuppressedCategories[area].Add(cat);
				}
			}
		}
		public static bool IsSuppressed(GlobalAreaKey area, StandardCategory cat) {
			return SuppressedCategories.ContainsKey(area) && SuppressedCategories[area].Contains(cat);
		}

		private SceneWrappingEntity<Overworld> overworldWrapper;

		public static ILSelector ActiveSelector { get; private set; } = null;

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
			ChosenCategory = null;

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
				overworld.RendererList.Remove(overworld.RendererList.Renderers.Find(r => r is H2HHudRenderer));
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

			if (ChosenCategory != null) {
				MatchTemplate tempate = ChosenCategory.Template;
				foreach (MatchPhase ph in tempate.Build()) {
					Head2HeadModule.Instance.AddMatchPhase(ph);
				}
				Head2HeadModule.Instance.NameBuildingMatch(tempate.DisplayName);

				if (tempate.RandoOptions != null) {  // TODO (!) move this into the integration file
					RandomizerIntegration.SettingsBuilder bld = new RandomizerIntegration.SettingsBuilder();
					bld.LogicType = tempate.RandoOptions.LogicType;
					bld.Difficulty = tempate.RandoOptions.Difficulty;
					bld.NumDashes = tempate.RandoOptions.NumDashes;
					bld.DifficultyEagerness = tempate.RandoOptions.DifficultyEagerness;
					bld.MapLength = tempate.RandoOptions.MapLength;
					bld.ShineLights = tempate.RandoOptions.ShineLights;
					bld.Darkness = tempate.RandoOptions.Darkness;
					bld.StrawberryDensity = tempate.RandoOptions.StrawberryDensity;
					if (tempate.RandoOptions.SeedType == "Random") {
						int numDays = (int)(SyncedClock.Now - DateTime.MinValue).TotalDays;
						bld.RandomizeSeed();
					}
					else {
						// Seed changes weekly
						int numDays = (int)(SyncedClock.Now - DateTime.MinValue).TotalDays;
						bld.RandomizeSeed(numDays / 7);
					}
					Head2HeadModule.Instance.AddRandoToMatch(bld);
				}
				ChosenCategory = null;
				Head2HeadModule.Instance.StageMatch();
			}
		}

		internal static void OnPause(On.Celeste.Level.orig_Pause orig, Level self, int startIndex, bool minimal, bool quickReset) {
			if (ActiveSelector != null) {
				ChosenCategory = null;
				ActiveSelector.Close(self, true, true);
			}
		}

	}
}
