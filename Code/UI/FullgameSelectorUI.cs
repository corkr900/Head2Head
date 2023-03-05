using Celeste.Mod.Head2Head.Integration;
using Celeste.Mod.Head2Head.Shared;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.Entities {
	[Tracked]
	public class FullgameSelectorUI : Entity {
		private static readonly Vector2 CanvasSize = new Vector2(1920, 1080);

		public event Action OnRemove;
		private List<Tuple<string, List<Option>>> categories;
		private static int HoveredOption = 0;
		private MTexture scarf = GFX.Gui["Head2Head/scarf"];
		private MTexture[] scarfSegments;
		private MTexture arrow = GFX.Gui["textboxbutton"];
		private static readonly int scarfSegmentSize = 12;
		private float timer = 0f;
		private static int selectedSetIdx = 0;

		public FullgameSelectorUI() {
			Tag = Tags.HUD;
		}

		public override void Added(Scene scene) {
			base.Added(scene);
			categories = GetCategories();
			HoveredOption = Calc.Clamp(HoveredOption, 0, categories.Count);
			UpdateUITargetPositions();
			scarfSegments = new MTexture[scarf.Height / scarfSegmentSize];
			for (int j = 0; j < scarfSegments.Length; j++) {
				scarfSegments[j] = scarf.GetSubtexture(0, j * scarfSegmentSize, scarf.Width, scarfSegmentSize);
			}
		}

		public override void Removed(Scene scene) {
			base.Removed(scene);
			OnRemove?.Invoke();
		}

		private List<Tuple<string, List<Option>>> GetCategories() {
			// Standard Fullgame Categories
			List<Tuple<string, List<Option>>> ret = new List<Tuple<string, List<Option>>>();
			StandardCategory[] standardCats = new StandardCategory[] {
				StandardCategory.AnyPercent,
				StandardCategory.AllRedBerries,
				StandardCategory.TrueEnding,
				StandardCategory.AllCassettes,
				StandardCategory.BnyPercent,
				StandardCategory.AllHearts,
				StandardCategory.OneHundredPercent,
				StandardCategory.AllChapters,
				StandardCategory.AllASides,
				StandardCategory.AllBSides,
				StandardCategory.AllCSides,
			};
			List<Option> vanillaList = new List<Option>();
			foreach (StandardCategory cat in standardCats) {
				vanillaList.Add(new Option() {
					Cat = cat,
				}.AfterInitialize());
			}
			ret.Add(new Tuple<string, List<Option>>("Celeste", vanillaList));

			// Collab Lobbies
			if (CollabUtils2Integration.IsCollabUtils2Installed) {
				// Collect a list of all lobbies and their maps
				List<Tuple<string, AreaData, List<AreaData>>> collabSets = new List<Tuple<string, AreaData, List<AreaData>>>();
				foreach (AreaData data in AreaData.Areas) {
					// This pattern assumes the lobby will get hit before its levels in the iteration
					// Check whether this is a lobby
					string setForLobby = CollabUtils2Integration.GetLobbyLevelSet(data.SID);
					if (!string.IsNullOrEmpty(setForLobby)) {
						collabSets.Add(new Tuple<string, AreaData, List<AreaData>>(setForLobby, data, new List<AreaData>()));
						continue;
					}
					// Check whether this map is in a lobby's set
					int idx = collabSets.FindIndex((Tuple<string, AreaData, List<AreaData>> tup) => tup.Item1 == data.LevelSet);
					if (idx >= 0 && idx < collabSets.Count) {
						collabSets[idx].Item3.Add(data);
					}
				}
				// Generate the lobby run category for each
				foreach (Tuple<string, AreaData, List<AreaData>> tup in collabSets) {
					// Set up the collection
					string collabLevelSet = tup.Item2.LevelSet;
					int idx = ret.FindIndex((Tuple<string, List<Option>> rettup) => rettup.Item1 == collabLevelSet);
					List<Option> list;
					if (idx < 0 || idx >= ret.Count) {
						list = new List<Option>();
						ret.Add(new Tuple<string, List<Option>>(collabLevelSet, list));
					}
					else {
						list = ret[idx].Item2;
					}
					// Finally make the category
					list.Add(new Option() {
						Cat = StandardCategory.Custom,
						CustomIcon = GFX.Gui[tup.Item2.Icon],
						CustomTitle = Dialog.CleanLevelSet(tup.Item2.Name),
						Template = GetTemplateForCollabLobby(tup),
					}.AfterInitialize());
				}
			}

			// Custom Fullgame Categories
			foreach (KeyValuePair<string, List<CustomMatchTemplate>> set in CustomMatchTemplate.FullgameTemplates) {
				int idx = ret.FindIndex((Tuple<string, List<Option>> tup) => tup.Item1 == set.Key);
				List<Option> list;
				if (idx < 0 || idx >= ret.Count) {
					list = new List<Option>();
					ret.Add(new Tuple<string, List<Option>>(set.Key, list));
				}
				else {
					list = ret[idx].Item2;
				}
				
				foreach (CustomMatchTemplate template in set.Value) {
					list.Add(new Option() {
						Cat = StandardCategory.Custom,
						CustomIcon = (!string.IsNullOrEmpty(template.IconPath) && GFX.Gui.Has(template.IconPath)) ? GFX.Gui[template.IconPath] : null,
						CustomTitle = template.DisplayName,
						Template = template,
					}.AfterInitialize());
				}
			}

			return ret;
		}

		private CustomMatchTemplate GetTemplateForCollabLobby(Tuple<string, AreaData, List<AreaData>> tup) {
			GlobalAreaKey area = new GlobalAreaKey(tup.Item2.SID);
			CustomMatchTemplate ret = new CustomMatchTemplate();
			ret.Key = "Autogen_CollabLobby_" + tup.Item2.SID;
			ret.Area = area;
			ret.IconPath = tup.Item2.Icon;
			ret.DisplayName = tup.Item2.CompleteScreenName;
			ret.AllowCheatMode = false;
			CustomMatchPhaseTemplate ph = new CustomMatchPhaseTemplate() {
				Area = area,
				LevelSet = tup.Item1,
			};
			foreach (AreaData data in tup.Item3) {
				ph.Objectives.Add(new CustomMatchObjectiveTemplate() {
					ObjectiveType = MatchObjectiveType.ChapterComplete,
					CustomTypeKey = data.SID,
				});
			}
			ret.Phases.Add(ph);
			return ret;
		}

		public override void Update() {
			base.Update();

			timer += Engine.DeltaTime;

			foreach (Tuple<string, List<Option>> set in categories) {
				foreach (Option o in set.Item2) {
					o.IconComponent?.Update();
					o.TitleComponent?.Update();
				}
			}

			if (Input.MenuCancel.Pressed) {
				Audio.Play("event:/ui/world_map/chapter/back");
				Add(new Coroutine(CloseCoroutine()));
				return;
			}

			if (categories == null || categories.Count == 0) return;
			string levelSet = categories[selectedSetIdx].Item1;
			List<Option> currentOptions = categories[selectedSetIdx].Item2;

			if (Input.MenuDown.Pressed && categories.Count > 1) {
				Audio.Play("event:/ui/main/rollover_down");
				HoveredOption = 0;
				selectedSetIdx = (selectedSetIdx + 1) % categories.Count;
				levelSet = categories[selectedSetIdx].Item1;
				currentOptions = categories[selectedSetIdx].Item2;
				UpdateUITargetPositions();

			}
			else if (Input.MenuUp.Pressed && categories.Count > 1) {
				Audio.Play("event:/ui/main/rollover_up");
				HoveredOption = 0;
				selectedSetIdx = selectedSetIdx > 0 ? selectedSetIdx - 1 : categories.Count - 1;
				levelSet = categories[selectedSetIdx].Item1;
				currentOptions = categories[selectedSetIdx].Item2;
				UpdateUITargetPositions();
			}

			if (Input.MenuRight.Pressed && HoveredOption < currentOptions.Count - 1) {
				Audio.Play("event:/ui/world_map/icon/roll_right");
				HoveredOption++;
				UpdateUITargetPositions();
			}
			else if (Input.MenuLeft.Pressed && HoveredOption > 0) {
				Audio.Play("event:/ui/world_map/icon/roll_left");
				HoveredOption--;
				UpdateUITargetPositions();
			}

			if (Input.MenuConfirm.Pressed) {
				Confirm();
			}
		}

		public override void Render() {
			base.Render();
			Vector2 AnchorPosition = new Vector2(CanvasSize.X / 2f, -50f);
			for (int i = 0; i < scarfSegments.Length; i++) {
				float wave = 8f * (float)Math.Sin(2f * (i / (float)scarfSegments.Length) - timer);
				scarfSegments[i].DrawJustified(
					AnchorPosition + Vector2.UnitY * (scarfSegments[i].Height * i) + Vector2.UnitX * wave,
					Vector2.UnitX / 2f);
			}
			if (categories != null) {
				foreach (Tuple<string, List<Option>> tup in categories) {
					foreach (Option o in tup.Item2) {
						o.IconComponent?.Render();
						o.TitleComponent?.Render();
					}
				}
			}

			string setTitle = Util.TranslatedIfAvailable(categories[selectedSetIdx].Item1);
			Vector2 size = ActiveFont.Measure(setTitle);
			Vector2 setTitlePos = AnchorPosition + Vector2.UnitY * 170f;
			ActiveFont.DrawOutline(setTitle, setTitlePos, Vector2.One / 2f, Vector2.One, Color.White, 2f, Color.Black);
			if (categories.Count > 1) {
				arrow.DrawCentered(setTitlePos + size.YComp() * 0.7f, Color.White, 1f, 0f);
				arrow.DrawCentered(setTitlePos - size.YComp() * 0.7f, Color.White, 1f, (float)Math.PI);
			}
		}

		private void UpdateUITargetPositions() {
			Vector2 AnchorPosition = new Vector2(CanvasSize.X / 2f, 0);
			for (int setI = 0; setI < categories.Count; setI++) {
				Tuple<string, List<Option>> tup = categories[setI];
				if (setI == selectedSetIdx) {
					for (int optI = 0; optI < tup.Item2.Count; optI++) {
						Vector2 basePosition = Vector2.UnitX * (optI - HoveredOption) * 300f;
						basePosition += Vector2.UnitY * (100f * (float)Math.Cos(basePosition.X / CanvasSize.X * 2f * Math.PI) + 100f);
						basePosition += AnchorPosition;

						tup.Item2[optI].IconComponent.Position = basePosition + (Vector2.UnitY * 50f);
						tup.Item2[optI].TitleComponent.Position = basePosition + (Vector2.UnitY * 150f);
					}
				}
				else {
					foreach (Option o in tup.Item2) {
						o.IconComponent.Position = AnchorPosition - Vector2.UnitY * 200f;
						o.TitleComponent.Position = AnchorPosition - Vector2.UnitY * 200f;
					}
				}
			}
			
		}

		private void Confirm() {
			if (Head2HeadModule.Instance.CanBuildFullgameMatch()) {
				Audio.Play("event:/ui/world_map/chapter/checkpoint_start");
				if (categories?.Count > 0) {
					Option o = categories[selectedSetIdx].Item2[HoveredOption];
					MatchDefinition def;
					if (o.Cat == StandardCategory.Custom) {
						def = o.Template.BuildFullgame();
					}
					else {
						def = StandardMatches.GetFullgameCategoryDefinition(o.Cat);
					}
					if (def == null) {
						Logger.Log(LogLevel.Error, "Head2Head", "Fullgame category build failed: " + o.Cat.ToString());
						return;
					}
					Head2HeadModule.Instance.buildingMatch = def;
					Head2HeadModule.Instance.StageMatch();
				}
			}
			else {
				Audio.Play("event:/ui/world_map/chapter/back");
			}
			Add(new Coroutine(CloseCoroutine()));
		}

		private IEnumerator CloseCoroutine() {
			foreach (Tuple<string, List<Option>> tup in categories) {
				foreach (Option o in tup.Item2) {
					o.IconComponent.Position = new Vector2(o.IconComponent.Position.X, -200f);
					o.TitleComponent.Position = new Vector2(o.IconComponent.Position.X, -50f);
				}
			}
			yield return 0.25f;
			RemoveSelf();
		}

		private class Option {
			public StandardCategory Cat;

			public string CustomTitle;
			public MTexture CustomIcon;
			public CustomMatchTemplate Template;

			public string Title {
				get {
					if (!string.IsNullOrEmpty(CustomTitle)) return CustomTitle;
					return Util.TranslatedCategoryName(Cat);
				}
			}
			public MTexture Icon {
				get {
					if (CustomIcon != null) return CustomIcon;
					switch (Cat) {
						default:
							return null;
						case StandardCategory.AnyPercent:
							return GFX.Gui[Util.CategoryToIcon(StandardCategory.Clear)];
						case StandardCategory.AllRedBerries:
							return GFX.Gui[Util.CategoryToIcon(StandardCategory.ARB)];
						case StandardCategory.BnyPercent:
							return GFX.Gui[Util.CategoryToIcon(StandardCategory.BnyPercent)];
						case StandardCategory.OneHundredPercent:
							return GFX.Gui[Util.CategoryToIcon(StandardCategory.OneHundredPercent)];
						case StandardCategory.AllASides:
							return GFX.Gui[Util.CategoryToIcon(StandardCategory.AllASides)];
						case StandardCategory.AllBSides:
							return GFX.Gui[Util.CategoryToIcon(StandardCategory.AllBSides)];
						case StandardCategory.AllCSides:
							return GFX.Gui[Util.CategoryToIcon(StandardCategory.AllCSides)];
						case StandardCategory.TrueEnding:
							return GFX.Gui[Util.CategoryToIcon(StandardCategory.TrueEnding)];
						case StandardCategory.AllCassettes:
							return GFX.Gui[Util.CategoryToIcon(StandardCategory.CassetteGrab)];
						case StandardCategory.AllHearts:
							return GFX.Gui[Util.CategoryToIcon(StandardCategory.AllHearts)];
						case StandardCategory.AllChapters:
							return GFX.Gui[Util.CategoryToIcon(StandardCategory.AllChapters)];
					}
				}
			}


			public FGSComponent IconComponent;
			public FGSComponent TitleComponent;

			public Option AfterInitialize() {
				TitleComponent = new FGSText() {
					Text = Title,
				};
				MTexture tex = Icon ?? GFX.Gui[Util.CategoryToIcon(StandardCategory.Custom)];
				IconComponent = new FGSIcon() {
					Icon = tex,
				};
				return this;
			}
		}

		public abstract class FGSComponent {
			public bool Shown = true;
			public float EaseTime = 0.2f;
			private Vector2 EaseBase = new Vector2(CanvasSize.X / 2f, -200);
			private Vector2 EaseTarget = new Vector2(CanvasSize.X / 2f, -200);
			private float EaseProgress = 1;
			public Vector2 Position {
				get {
					float eased = Ease.CubeInOut(Calc.Clamp(EaseProgress, 0, 1));
					return new Vector2(Calc.LerpClamp(EaseBase.X, EaseTarget.X, eased), Calc.LerpClamp(EaseBase.Y, EaseTarget.Y, eased));
				}
				set {
					if (value.Equals(EaseTarget)) return;
					EaseBase = Position;
					EaseTarget = value;
					EaseProgress = 0;
				}
			}

			public Vector2 PositionImmediate {
				set {
					EaseBase = value;
					EaseTarget = value;
					EaseProgress = 1;
				}
				get { return Position; }
			}

			public void Update() {
				EaseProgress = Calc.Approach(EaseProgress, 1, Engine.DeltaTime / EaseTime);
			}

			public abstract void Render();
		}

		public class FGSIcon : FGSComponent {
			public MTexture Icon;
			public override void Render() {
				if (Shown) {
					Icon.DrawJustified(Position, new Vector2(0.5f, 0.5f));
				}
			}
		}

		public class FGSText : FGSComponent {
			public string Text;
			public override void Render() {
				if (Shown) {
					ActiveFont.DrawOutline(Util.TranslatedIfAvailable(Text), Position, new Vector2(0.5f, 0.5f), Vector2.One, Color.White, 2f, Color.Black);
				}
			}
		}
	}
}
