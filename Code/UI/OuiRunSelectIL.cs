using Celeste.Mod.Head2Head.Entities;
using Celeste.Mod.Head2Head.Integration;
using Celeste.Mod.Head2Head.Shared;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.UI {
	public class OuiRunSelectIL : Oui {

		public static bool Start;
		public static List<RunOptionsLevelSet> SelectableLevelSets = new List<RunOptionsLevelSet>();
		private static int _specialIDCounter = 0;

		public override bool IsStart(Overworld overworld, Overworld.StartMode start) {
			if (Start) {
				Start = false;
				Add(new Coroutine(Enter(null)));
				return true;
			}

			return false;
		}

		public override IEnumerator Enter(Oui from) {
			Audio.Play("event:/ui/world_map/icon/select");
			BuildOptions();
			Overworld.Goto<OuiRunSelectILChapterSelect>();
			yield break;
		}

		public override IEnumerator Leave(Oui next) {
			yield break;
		}

		public static string LevelSetIdxToSet(int levelSetIndex) {
			return levelSetIndex < 0 ? null : levelSetIndex >= SelectableLevelSets.Count ? null : SelectableLevelSets[levelSetIndex].LevelSet;
		}

		public static int LevelSetToIdx(string setID) {
			for (int i = 0; i < SelectableLevelSets.Count; i++) {
				if (SelectableLevelSets[i].LevelSet == setID) return i;
			}
			return 0;
		}

		public static RunOptionsILChapter GetChapterOption(int levelSetIdx, int chapterIdx) {
			if (levelSetIdx < 0 || levelSetIdx >= SelectableLevelSets.Count) {
				return null;
			}
			if (chapterIdx < 0 || chapterIdx >= SelectableLevelSets[levelSetIdx].Chapters.Count) {
				return null;
			}
			return SelectableLevelSets[levelSetIdx].Chapters[chapterIdx];
		}

		public static RunOptionsILChapter GetChapterOption(string SID, ref int levelSetIdx, ref int chapterIdx) {
			for (int i = 0; i < SelectableLevelSets.Count; i++) {
				for (int j = 0; j < SelectableLevelSets[i].Chapters.Count; j++) {
					if (SelectableLevelSets[i].Chapters[j].Data.SID == SID) {
						levelSetIdx = i;
						chapterIdx = j;
						return SelectableLevelSets[i].Chapters[j];
					}
				}
			}
			return null;
		}

		public static RunOptionsILChapter GetChapterOption(int ID, ref int levelSetIdx, ref int chapterIdx) {
			for (int i = 0; i < SelectableLevelSets.Count; i++) {
				for (int j = 0; j < SelectableLevelSets[i].Chapters.Count; j++) {
					if (SelectableLevelSets[i].Chapters[j].Data.ID == ID) {
						levelSetIdx = i;
						chapterIdx = j;
						return SelectableLevelSets[i].Chapters[j];
					}
				}
			}
			return null;
		}

		public static int GetNumOptionsInSet(int levelSetIdx) {
			if (levelSetIdx < 0 || levelSetIdx >= SelectableLevelSets.Count) {
				return 0;
			}
			return SelectableLevelSets[levelSetIdx].Chapters.Count;
		}

		internal static RunOptionsLevelSet PreviousLevelSet(int levelSetIn, ref int levelSetOut, bool includeHidden = false) {
			if (SelectableLevelSets.Count == 0) {
				levelSetOut = 0;
				return null;
			}
			levelSetOut = levelSetIn;
			while (--levelSetOut != levelSetIn) {
				if (levelSetOut < 0) {
					levelSetOut = SelectableLevelSets.Count - 1;
				}
				RunOptionsLevelSet set = SelectableLevelSets[levelSetOut];
				if (includeHidden || !set.Hidden) return set;
			}
			return SelectableLevelSets[levelSetOut];
		}

		internal static RunOptionsLevelSet NextLevelSet(int levelSetIn, ref int levelSetOut, bool includeHidden = false) {
			if (SelectableLevelSets.Count == 0) {
				levelSetOut = 0;
				return null;
			}
			levelSetOut = levelSetIn;
			while (++levelSetOut != levelSetIn) {
				if (levelSetOut >= SelectableLevelSets.Count) {
					levelSetOut = 0;
				}
				RunOptionsLevelSet set = SelectableLevelSets[levelSetOut];
				if (includeHidden || !set.Hidden) return set;
			}
			return SelectableLevelSets[levelSetOut];
		}

		private static void BuildOptions() {
			SelectableLevelSets.Clear();
			_specialIDCounter = 0;
			int count = AreaData.Areas.Count;
			string levelSet = "";
			RunOptionsLevelSet setOption = null;
			bool setAdded = false;

			// Loop through normal vanilla + modded maps & add valid options
			for (int id = 0; id < count; id++) {
				AreaData areaData = AreaData.Get(id);
				if (areaData == null) continue;
				if (CollabUtils2Integration.IsCollabGym?.Invoke(areaData.SID) ?? false) continue;
				string set = areaData.LevelSet;
				if (string.IsNullOrEmpty(set)) continue;
				if (set == "Head2Head") continue;

				// Add a new level set option if we need to
				if (set != levelSet || setOption == null) {
					levelSet = set;
					setOption = new RunOptionsLevelSet();
					setOption.LevelSet = levelSet;
					setAdded = false;
				}

				// Hide non-lobby collab maps
				if (CollabUtils2Integration.IsCollabMap?.Invoke(areaData.SID) ?? false) {
					setOption.Hidden = true;
				}
				else if (CollabUtils2Integration.IsCollabGym?.Invoke(areaData.SID) ?? false) {
					setOption.Hidden = true;
				}

				// Add stuff about the level
				RunOptionsILChapter chapter = new RunOptionsILChapter();
				chapter.Data = areaData;
				if (BuildOptions_NormalChapter(chapter, areaData, areaData.ToKey())) {
					setOption.Chapters.Add(chapter);
					if (!setAdded) {
						SelectableLevelSets.Add(setOption);
						setAdded = true;
					}
				}
			}
			
			// Add special options
			AddRandomizerCategories();
		}

		private static void AddRandomizerCategories() {
			if (!RandomizerIntegration.RandomizerLoaded) return;

			RunOptionsLevelSet setOption = new RunOptionsLevelSet();
			setOption.LevelSet = "Randomizer";
			SelectableLevelSets.Add(setOption);

			// Pathway "chapter"
			RunOptionsILChapter pathway = new RunOptionsILChapter();
			pathway.Title = "Pathway Randomizer";
			pathway.SpecialID = NewSpecialID();
			pathway.Icon = "menu/pathway_icon";
			setOption.Chapters.Add(pathway);

			// Pathway 1 dash
			RunOptionsILSide side = new RunOptionsILSide() {
				Label = Dialog.Clean("Head2Head_Rando_onedash"),
				Icon = GFX.Gui["menu/play"],
				ID = "A",
				Mode = AreaMode.Normal,
			};
			pathway.Sides.Add(side);
			AddAllDifficulties(side, "Pathway", "One");

			// Pathway 2 dash
			side = new RunOptionsILSide() {
				Label = Dialog.Clean("Head2Head_Rando_twodash"),
				Icon = GFX.Gui["menu/play"],
				ID = "A",
				Mode = AreaMode.Normal,
			};
			pathway.Sides.Add(side);
			AddAllDifficulties(side, "Pathway", "Two");

			// Pathway dashless
			side = new RunOptionsILSide() {
				Label = Dialog.Clean("Head2Head_Rando_nodash"),
				Icon = GFX.Gui["menu/play"],
				ID = "A",
				Mode = AreaMode.Normal,
			};
			pathway.Sides.Add(side);
			AddAllDifficulties(side, "Pathway", "Zero");

			// Pathway Weekly
			side = new RunOptionsILSide() {
				Label = Dialog.Clean("Head2Head_Rando_WeeklySeed"),
				Icon = GFX.Gui["menu/play"],
				ID = "A",
				Mode = AreaMode.Normal,
			};
			pathway.Sides.Add(side);
			side.Categories.Add(new RunOptionsILCategory() {
				Title = "MODOPTIONS_RANDOMIZER_DIFFICULTY_NORMAL",
				IconPath = "menu/skulls/skullBlue",
				CustomTemplate = RandomizerTemplate("Weekly Pathway - Normal", "Pathway", "Normal", "Custom"),  // TODO (!!) Tokenize
			});
			side.Categories.Add(new RunOptionsILCategory() {
				Title = "MODOPTIONS_RANDOMIZER_DIFFICULTY_EXPERT",
				IconPath = "menu/skulls/skullGold",
				CustomTemplate = RandomizerTemplate("Weekly Pathway - Expert", "Pathway", "Expert", "Custom"),  // TODO (!!) Tokenize
			});
			side.Categories.Add(new RunOptionsILCategory() {
				Title = "Dashless",  // TODO (!!) Tokenize
				IconPath = "menu/skulls/skullRed",
				CustomTemplate = RandomizerTemplate("Weekly Pathway - Dashless", "Pathway", "Normal", "Custom", "None"),  // TODO (!!) Tokenize
			});

			//////////////////////////////////////////////////////////////////////

			// Labyrinth "chapter"
			RunOptionsILChapter labyrinth = new RunOptionsILChapter();
			labyrinth.Title = "Labyrinth Randomizer";  // TODO (!!) tokenize
			labyrinth.SpecialID = NewSpecialID();
			labyrinth.Icon = "menu/labyrinth_icon";
			setOption.Chapters.Add(labyrinth);
			side = new RunOptionsILSide() {
				Label = Dialog.Clean("overworld_normal").ToUpper(),
				Icon = GFX.Gui["menu/play"],
				ID = "A",
				Mode = AreaMode.Normal,
			};
			labyrinth.Sides.Add(side);
			AddAllDifficulties(side, "Labyrinth", "One");

			// Labyrinth 2 dash
			side = new RunOptionsILSide() {
				Label = Dialog.Clean("Head2Head_Rando_twodash"),
				Icon = GFX.Gui["menu/play"],
				ID = "A",
				Mode = AreaMode.Normal,
			};
			labyrinth.Sides.Add(side);
			AddAllDifficulties(side, "Labyrinth", "Two");

			// Labyrinth dashless
			side = new RunOptionsILSide() {
				Label = Dialog.Clean("Head2Head_Rando_nodash"),
				Icon = GFX.Gui["menu/play"],
				ID = "A",
				Mode = AreaMode.Normal,
			};
			labyrinth.Sides.Add(side);
			AddAllDifficulties(side, "Labyrinth", "Zero");

			// Labyrinth Weekly
			side = new RunOptionsILSide() {
				Label = Dialog.Clean("Head2Head_Rando_WeeklySeed"),
				Icon = GFX.Gui["menu/play"],
				ID = "A",
				Mode = AreaMode.Normal,
			};
			labyrinth.Sides.Add(side);
			side.Categories.Add(new RunOptionsILCategory() {
				Title = "MODOPTIONS_RANDOMIZER_DIFFICULTY_NORMAL",
				IconPath = "menu/skulls/skullBlue",
				CustomTemplate = RandomizerTemplate("Weekly Labyrinth - Normal", "Labyrinth", "Normal", "Custom"),  // TODO (!!) Tokenize
			});
			side.Categories.Add(new RunOptionsILCategory() {
				Title = "MODOPTIONS_RANDOMIZER_DIFFICULTY_EXPERT",
				IconPath = "menu/skulls/skullGold",
				CustomTemplate = RandomizerTemplate("Weekly Labyrinth - Expert", "Labyrinth", "Expert", "Custom"),  // TODO (!!) Tokenize
			});
			side.Categories.Add(new RunOptionsILCategory() {
				Title = "Dashless",  // TODO (!!) Tokenize
				IconPath = "menu/skulls/skullRed",
				CustomTemplate = RandomizerTemplate("Weekly Labyrinth - Dashless", "Labyrinth", "Normal", "Custom", "None"),  // TODO (!!) Tokenize
			});
		}

		private static void AddAllDifficulties(RunOptionsILSide side, string type, string dashes) {
			side.Categories.Add(new RunOptionsILCategory() {
				Title = "MODOPTIONS_RANDOMIZER_DIFFICULTY_EASY",
				IconPath = "menu/skulls/strawberry",
				CustomTemplate = RandomizerTemplate(RandoCatDispName("Easy", type, dashes), type, "Easy", numDashes: dashes),
			});
			side.Categories.Add(new RunOptionsILCategory() {
				Title = "MODOPTIONS_RANDOMIZER_DIFFICULTY_NORMAL",
				IconPath = "menu/skulls/skullBlue",
				CustomTemplate = RandomizerTemplate(RandoCatDispName("Normal", type, dashes), type, "Normal", numDashes: dashes),
			});
			side.Categories.Add(new RunOptionsILCategory() {
				Title = "MODOPTIONS_RANDOMIZER_DIFFICULTY_HARD",
				IconPath = "menu/skulls/skullRed",
				CustomTemplate = RandomizerTemplate(RandoCatDispName("Hard", type, dashes), type, "Hard", numDashes: dashes),
			});
			side.Categories.Add(new RunOptionsILCategory() {
				Title = "MODOPTIONS_RANDOMIZER_DIFFICULTY_EXPERT",
				IconPath = "menu/skulls/skullGold",
				CustomTemplate = RandomizerTemplate(RandoCatDispName("Expert", type, dashes), type, "Expert", numDashes: dashes),
			});
			side.Categories.Add(new RunOptionsILCategory() {
				Title = "MODOPTIONS_RANDOMIZER_DIFFICULTY_MASTER",
				IconPath = "menu/skulls/skullOrange",
				CustomTemplate = RandomizerTemplate(RandoCatDispName("Master", type, dashes), type, "Master", numDashes: dashes),
			});
			side.Categories.Add(new RunOptionsILCategory() {
				Title = "MODOPTIONS_RANDOMIZER_DIFFICULTY_PERFECT",
				IconPath = "menu/skulls/skullPurple",
				CustomTemplate = RandomizerTemplate(RandoCatDispName("Perfect", type, dashes), type, "Perfect", numDashes: dashes),
			});
		}

		private static string RandoCatDispName(string diff, string logic, string numDashes) {
			string diffDialogKey = "MODOPTIONS_RANDOMIZER_DIFFICULTY_" + diff.ToUpper();
			string logicDialogKey = "MODOPTIONS_RANDOMIZER_LOGIC_" + logic.ToUpper();
			string numDashesDialogKey = "Head2Head_Rando_Dashcount_" + numDashes;
			return string.Format(
				Dialog.Get("Head2Head_Rando_CatNameFormat"),
				Dialog.Get(diffDialogKey),
				Dialog.Get(logicDialogKey),
				Dialog.Get(numDashesDialogKey));
		}

		private static CustomMatchTemplate RandomizerTemplate(string name, string logicType, string difficulty, string seedType = "Random", string numDashes = "One") {
			CustomMatchTemplate template = new CustomMatchTemplate() {
				DisplayName = name,
				RandoOptions = new RandomizerOptionsTemplate {
					SeedType = RandomizerIntegration.SeedTypeEnum.Valiate(seedType),
					LogicType = RandomizerIntegration.LogicTypeEnum.Valiate(logicType),
					Difficulty = RandomizerIntegration.DifficultyEnum.Valiate(difficulty),
					MapLength = RandomizerIntegration.MapLengthEnum.Valiate("Short"),
					NumDashes = RandomizerIntegration.NumDashesEnum.Valiate(numDashes),
					DifficultyEagerness = RandomizerIntegration.DifficultyEagernessEnum.Valiate("Medium"),
					ShineLights = RandomizerIntegration.ShineLightsEnum.Valiate("On"),
					Darkness = RandomizerIntegration.DarknessEnum.Valiate("Never"),
					StrawberryDensity = RandomizerIntegration.StrawberryDensityEnum.Valiate("None"),
				},
			};
			CustomMatchPhaseTemplate phase = new CustomMatchPhaseTemplate() {
				LevelSet = "Randomizer",
				Area = GlobalAreaKey.Randomizer,
			};
			phase.Objectives.Add(new CustomMatchObjectiveTemplate() {
				ObjectiveType = MatchObjectiveType.RandomizerClear,
				Side = AreaMode.Normal,
				Description = "Complete the randomized level",  // TODO (!!) Tokenize
			});
			template.Phases.Add(phase);
			return template;
		}

		private static int NewSpecialID() {
			return ++_specialIDCounter;
		}

		private static bool BuildOptions_NormalChapter(RunOptionsILChapter chapter, AreaData data, AreaKey area) {
			bool addedOptions = false;
			if (StandardMatches.HasAnyValidCategory(new GlobalAreaKey(area.ID, AreaMode.Normal))) {
				chapter.Sides.Add(new RunOptionsILSide {
					Label = Dialog.Clean(data.Interlude ? "FILE_BEGIN" : "overworld_normal").ToUpper(),
					Icon = GFX.Gui["menu/play"],
					ID = "A",
					Mode = AreaMode.Normal,
				});
				addedOptions = true;
			}
			if (StandardMatches.HasAnyValidCategory(new GlobalAreaKey(area.ID, AreaMode.BSide))) {
				chapter.Sides.Add(new RunOptionsILSide {
					Label = Dialog.Clean("overworld_remix"),
					Icon = GFX.Gui["menu/remix"],
					ID = "B",
					Mode = AreaMode.BSide,
				});
				addedOptions = true;
			}
			if (StandardMatches.HasAnyValidCategory(new GlobalAreaKey(area.ID, AreaMode.CSide))) {
				chapter.Sides.Add(new RunOptionsILSide {
					Label = Dialog.Clean("overworld_remix2"),
					Icon = GFX.Gui["menu/rmx2"],
					ID = "C",
					Mode = AreaMode.CSide,
				});
				addedOptions = true;
			}
			return addedOptions;
		}

	}

	public class RunOptionsLevelSet {
		public string LevelSet;
		public bool Hidden;

		public List<RunOptionsILChapter> Chapters = new List<RunOptionsILChapter>();
	}

	public class RunOptionsILChapter {
		public string Title;
		public string Icon;
		public AreaData Data;
		public bool IsSpecial { get { return Data == null; } }
		public int SpecialID = 0;
		public string CollabLobby {
			get {
				return Data == null ? null : CollabUtils2Integration.GetLobbyForMap?.Invoke(Data.SID);
			}
		}
		public string CollabLevelSetForLobby {
			get {
				return CollabUtils2Integration.GetLobbyLevelSet?.Invoke(Data.SID);
			}
		}
		public List<RunOptionsILSide> Sides = new List<RunOptionsILSide>();
	}

	public class RunOptionsILSide {
		public string Label;
		public string ID;
		public MTexture Icon;
		public AreaMode Mode = AreaMode.Normal;
		public List<RunOptionsILCategory> Categories = new List<RunOptionsILCategory>();
	}

	public class RunOptionsILCategory {
		public string Title;
		internal string IconPath;
		internal CustomMatchTemplate CustomTemplate;
	}

}
