using Celeste.Mod.CelesteNet;
using Celeste.Mod.Head2Head.IO;
using Celeste.Mod.Head2Head.Shared;
using Celeste.Mod.Head2Head.UI;
using Monocle;
using MonoMod.ModInterop;
using MonoMod.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using static Celeste.Mod.Head2Head.Integration.RandomizerIntegration;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Celeste.Mod.Head2Head.Integration {

	[ModImportName("Randomizer.SettingsInterop")]
	public static class RandomizerSettingsInterop {
		public static Func<object> GetSettingsObject;
		public static Action<object, AreaKey> EnableMap;
		public static Action<object, IEnumerable<AreaKey>> EnableMaps;
		public static Action<object> EnableVanillaMaps;
		public static Action<object, string> SetSeed;
		public static Action<object, string> SetRules;
		public static Func<object, string, bool> SetSeedType;
		public static Func<object, string, bool> SetAlgorithm;
		public static Func<object, string, bool> SetDashes;
		public static Func<object, string, bool> SetDifficulty;
		public static Func<object, string, bool> SetDifficultyEagerness;
		public static Func<object, string, bool> SetLength;
		public static Func<object, string, bool> SetLights;
		public static Func<object, string, bool> SetDarkness;
		public static Func<object, string, bool> SetStrawberries;
	}

	[ModImportName("Randomizer.GenerationInterop")]
	public static class RandomizerGenerationInterop {
		public static Func<object, bool> Generate;
		public static Func<bool> GenerationInProgress;
		public static Func<bool> ReadyToLaunch;
		public static Func<AreaKey?> GetGeneratedArea;
		public static Func<bool> EnterGeneratedArea;
	}

	public class RandomizerIntegration {

		public static bool RandomizerLoaded {
			get {
				return RandomizerGenerationInterop.Generate != null
					&& RandomizerSettingsInterop.GetSettingsObject != null;
			}
		}

		public static void Load() {
			typeof(RandomizerGenerationInterop).ModInterop();
			typeof(RandomizerSettingsInterop).ModInterop();
		}

		/// <summary>
		/// Class implemented to build out a RandoSettings object
		/// </summary>
		public class SettingsBuilder {

			public string Seed { get; set; }
			public string LogicType { get; set; }
			public string Difficulty { get; set; }
			public string NumDashes { get; set; }
			public string DifficultyEagerness { get; set; }
			public string MapLength { get; set; }
			public string ShineLights { get; set; }
			public string Darkness { get; set; }
			public string StrawberryDensity { get; set; }

			public SettingsBuilder() {
				
			}

			/// <summary>
			/// Builds the actual settings object
			/// </summary>
			/// <returns></returns>
			public object Build() {
				if (!RandomizerLoaded) return null;

				object settings = RandomizerSettingsInterop.GetSettingsObject();
				RandomizerSettingsInterop.EnableVanillaMaps(settings);
				RandomizerSettingsInterop.SetSeedType(settings, "Custom");
				RandomizerSettingsInterop.SetRules(settings, "");

				RandomizerSettingsInterop.SetSeed(settings, Seed);
				RandomizerSettingsInterop.SetAlgorithm(settings, LogicType);
				RandomizerSettingsInterop.SetDifficulty(settings, Difficulty);
				RandomizerSettingsInterop.SetDashes(settings, NumDashes);
				RandomizerSettingsInterop.SetDifficultyEagerness(settings, DifficultyEagerness);
				RandomizerSettingsInterop.SetLength(settings, MapLength);
				RandomizerSettingsInterop.SetLights(settings, ShineLights);
				RandomizerSettingsInterop.SetDarkness(settings, Darkness);
				RandomizerSettingsInterop.SetStrawberries(settings, StrawberryDensity);

				return settings;
			}

			internal void RandomizeSeed(int? seedseed = null) {
				Seed = "";
				var ra = seedseed == null ? new Random() : new Random(seedseed.Value);
				for (int i = 0; i < 6; i++) {
					var val = ra.Next(36);
					if (val < 10) {
						Seed += ((char)('0' + val)).ToString();
					}
					else {
						Seed += ((char)('a' + val - 10)).ToString();
					}
				}
			}
		}

		public static bool BeginGeneration(object settings) {
			return RandomizerGenerationInterop.Generate(settings);
		}

		public static IEnumerator Begin() {
			while (RandomizerGenerationInterop.GenerationInProgress()) {
				yield return null;
			}
			if (RandomizerGenerationInterop.ReadyToLaunch()) {
				PlayerStatus.Current.RandomizerArea = new GlobalAreaKey(RandomizerGenerationInterop.GetGeneratedArea() ?? AreaKey.Default);
				RandomizerGenerationInterop.EnterGeneratedArea();
			}
			else {
				Logger.Log(LogLevel.Error, "Head2Head", "H2H + Randomizer: Randomizer area generation via ModInterop failed.");
			}
		}
	}

	public static class RandoIntegrationExtensions {
		public static SettingsBuilder ReadRandoSettings(this CelesteNetBinaryReader r) {
			SettingsBuilder sb = new SettingsBuilder();
			sb.Seed = r.ReadString();
			sb.LogicType = r.ReadString();
			sb.Difficulty = r.ReadString();
			sb.NumDashes = r.ReadString();
			sb.DifficultyEagerness = r.ReadString();
			sb.MapLength = r.ReadString();
			sb.ShineLights = r.ReadString();
			sb.Darkness = r.ReadString();
			sb.StrawberryDensity = r.ReadString();
			return sb;
		}

		public static void Write(this CelesteNetBinaryWriter w, SettingsBuilder settings) {
			w.Write(settings.Seed ?? "");
			w.Write(settings.LogicType ?? "");
			w.Write(settings.Difficulty ?? "");
			w.Write(settings.NumDashes ?? "");
			w.Write(settings.DifficultyEagerness ?? "");
			w.Write(settings.MapLength ?? "");
			w.Write(settings.ShineLights ?? "");
			w.Write(settings.Darkness ?? "");
			w.Write(settings.StrawberryDensity ?? "");
		}
	}

	public static class RandoStrings {
		public static string Title => Dialog.Clean("HEAD2HEAD_RANDOMIZER");
		public static string TypePathway => Dialog.Clean("MODOPTIONS_RANDOMIZER_LOGIC_PATHWAY");
		public static string TypeLabyrinth => Dialog.Clean("MODOPTIONS_RANDOMIZER_LOGIC_LABYRINTH");
		public static string TypeEndless => Dialog.Clean("MODOPTIONS_RANDOMIZER_LOGIC_ENDLESS");
		public static object DiffEasy => Dialog.Clean("MODOPTIONS_RANDOMIZER_DIFFICULTY_EASY");
		public static object DiffNormal => Dialog.Clean("MODOPTIONS_RANDOMIZER_DIFFICULTY_NORMAL");
		public static object DiffHard => Dialog.Clean("MODOPTIONS_RANDOMIZER_DIFFICULTY_HARD");
		public static object DiffExpert => Dialog.Clean("MODOPTIONS_RANDOMIZER_DIFFICULTY_EXPERT");
		public static object DiffMaster => Dialog.Clean("MODOPTIONS_RANDOMIZER_DIFFICULTY_MASTER");
		public static object DiffPerfect => Dialog.Clean("MODOPTIONS_RANDOMIZER_DIFFICULTY_PERFECT");
	}

	public static class RandomizerCategories {

		internal static void AddRandomizerCategories(List<RunOptionsLevelSet> selectableLevelSets) {
			if (!RandomizerLoaded) return;

			RunOptionsLevelSet setOption = new RunOptionsLevelSet();
			setOption.LevelSet = "Randomizer";
			selectableLevelSets.Add(setOption);

			// Pathway "chapter"
			RunOptionsILChapter pathway = new RunOptionsILChapter();
			pathway.Title = string.Format(Dialog.Get(""), RandoStrings.TypePathway, RandoStrings.Title);
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
				Template = RandomizerTemplate(
					string.Format(Dialog.Get("Head2Head_Rando_WeeklyCatName"), RandoStrings.TypePathway, RandoStrings.DiffNormal),
					"Pathway", "Normal", "Custom"),
			});
			side.Categories.Add(new RunOptionsILCategory() {
				Title = "MODOPTIONS_RANDOMIZER_DIFFICULTY_EXPERT",
				IconPath = "menu/skulls/skullGold",
				Template = RandomizerTemplate(
					string.Format(Dialog.Get("Head2Head_Rando_WeeklyCatName"), RandoStrings.TypePathway, RandoStrings.DiffExpert),
					"Pathway", "Expert", "Custom"),
			});
			side.Categories.Add(new RunOptionsILCategory() {
				Title = Dialog.Clean("Head2Head_Rando_Dashcount_Zero"),
				IconPath = "menu/skulls/skullRed",
				Template = RandomizerTemplate(
					string.Format(Dialog.Get("Head2Head_Rando_WeeklyCatName"), RandoStrings.TypePathway, Dialog.Clean("Head2Head_Rando_Dashcount_Zero")),
					"Pathway", "Normal", "Custom", "None"),
			});

			//////////////////////////////////////////////////////////////////////

			// Labyrinth "chapter"
			RunOptionsILChapter labyrinth = new RunOptionsILChapter();
			labyrinth.Title = string.Format(Dialog.Get(""), RandoStrings.TypeLabyrinth, RandoStrings.Title);
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
				Template = RandomizerTemplate(
					string.Format(Dialog.Get("Head2Head_Rando_WeeklyCatName"), RandoStrings.TypeLabyrinth, RandoStrings.DiffNormal),
					"Weekly Labyrinth - Normal",
					"Labyrinth", "Normal", "Custom"),
			});
			side.Categories.Add(new RunOptionsILCategory() {
				Title = "MODOPTIONS_RANDOMIZER_DIFFICULTY_EXPERT",
				IconPath = "menu/skulls/skullGold",
				Template = RandomizerTemplate(
					string.Format(Dialog.Get("Head2Head_Rando_WeeklyCatName"), RandoStrings.TypeLabyrinth, RandoStrings.DiffExpert),
					"Weekly Labyrinth - Expert",
					"Labyrinth", "Expert", "Custom"),
			});
			side.Categories.Add(new RunOptionsILCategory() {
				Title = Dialog.Clean("Head2Head_Rando_Dashcount_Zero"),
				IconPath = "menu/skulls/skullRed",
				Template = RandomizerTemplate(
					string.Format(Dialog.Get("Head2Head_Rando_WeeklyCatName"), RandoStrings.TypeLabyrinth, Dialog.Clean("Head2Head_Rando_Dashcount_Zero")),
					"Labyrinth", "Normal", "Custom", "None"),
			});


			//////////////////////////////////////////////////////////////////////

			// Custom Randomizer Categories
			RunOptionsILChapter customs = new RunOptionsILChapter();
			customs.Title = "TODO - CUSTOM CATEGORIES TITLE (1)";
			customs.Icon = "Head2Head/Categories/Custom";
			// only 1 side (?)
			side = new RunOptionsILSide() {
				Label = "TODO - CUSTOM CATEGORIES TITLE (2)",
				Icon = GFX.Gui["menu/play"],
				ID = "A",
				Mode = AreaMode.Normal,
			};
			customs.Sides.Add(side);
			bool customCategoryAdded = false;
			foreach (MatchTemplate cat in GetCustomRandoCategories()) {
				if (cat == null) continue;
				side.Categories.Add(new RunOptionsILCategory() {
					Title = cat.DisplayName,
					IconPath = cat.IconPath,
					Template = cat,
				});
				customCategoryAdded = true;
			}
			if (customCategoryAdded) setOption.Chapters.Add(customs);
		}

		private static IEnumerable<MatchTemplate> GetCustomRandoCategories() {
			RandomizerCustomOptionsFile data = RandomizerCustomOptionsFile.Load();
			foreach (var category in data.Categories) {
				yield return RandomizerTemplate(category.Name, category.Options);
			}
		}

		private static void AddAllDifficulties(RunOptionsILSide side, string type, string dashes) {
			side.Categories.Add(new RunOptionsILCategory() {
				Title = "MODOPTIONS_RANDOMIZER_DIFFICULTY_EASY",
				IconPath = "menu/skulls/strawberry",
				Template = RandomizerTemplate(RandoCatDispName("Easy", type, dashes), type, "Easy", numDashes: dashes),
			});
			side.Categories.Add(new RunOptionsILCategory() {
				Title = "MODOPTIONS_RANDOMIZER_DIFFICULTY_NORMAL",
				IconPath = "menu/skulls/skullBlue",
				Template = RandomizerTemplate(RandoCatDispName("Normal", type, dashes), type, "Normal", numDashes: dashes),
			});
			side.Categories.Add(new RunOptionsILCategory() {
				Title = "MODOPTIONS_RANDOMIZER_DIFFICULTY_HARD",
				IconPath = "menu/skulls/skullRed",
				Template = RandomizerTemplate(RandoCatDispName("Hard", type, dashes), type, "Hard", numDashes: dashes),
			});
			side.Categories.Add(new RunOptionsILCategory() {
				Title = "MODOPTIONS_RANDOMIZER_DIFFICULTY_EXPERT",
				IconPath = "menu/skulls/skullGold",
				Template = RandomizerTemplate(RandoCatDispName("Expert", type, dashes), type, "Expert", numDashes: dashes),
			});
			side.Categories.Add(new RunOptionsILCategory() {
				Title = "MODOPTIONS_RANDOMIZER_DIFFICULTY_MASTER",
				IconPath = "menu/skulls/skullOrange",
				Template = RandomizerTemplate(RandoCatDispName("Master", type, dashes), type, "Master", numDashes: dashes),
			});
			side.Categories.Add(new RunOptionsILCategory() {
				Title = "MODOPTIONS_RANDOMIZER_DIFFICULTY_PERFECT",
				IconPath = "menu/skulls/skullPurple",
				Template = RandomizerTemplate(RandoCatDispName("Perfect", type, dashes), type, "Perfect", numDashes: dashes),
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

		internal static MatchTemplate RandomizerTemplate(string name, RandomizerOptionsTemplate opts) {
			MatchTemplate template = new MatchTemplate() {
				DisplayName = name,
				RandoOptions = opts,
			};
			MatchPhaseTemplate phase = new MatchPhaseTemplate() {
				LevelSet = "Randomizer",
				Area = GlobalAreaKey.Randomizer,
			};
			phase.Objectives.Add(new MatchObjectiveTemplate() {
				ObjectiveType = MatchObjectiveType.RandomizerClear,
				Side = AreaMode.Normal,
				Description = Dialog.Clean("Head2Head_ObjectiveDescription_RandomizerClear"),
			});
			template.Phases.Add(phase);
			return template;
		}

		internal static MatchTemplate RandomizerTemplate(string name, string logicType, string difficulty, string seedType = "Random", string numDashes = "One") {
			return RandomizerTemplate(name, new RandomizerOptionsTemplate {
				SeedType = seedType,
				LogicType = logicType,
				Difficulty = difficulty,
				NumDashes = numDashes,
				MapLength = "Short",
				DifficultyEagerness = "Medium",
				ShineLights = "On",
				Darkness = "Never",
				StrawberryDensity = "None",
			});
		}

	}

	[Serializable]
	public class RandomizerCustomOptionsFile {

		private static string GetCustomRandoCatsFileName() {
			// Use DynamicData to access SavePath because the implementation of it is different between FNA/XNA
			DynamicData dd = new DynamicData(typeof(UserIO));
			string dirpath = dd.Get<string>("SavePath");
			return Path.Combine(dirpath, "Head2Head_CustomRandomizerCategories.celeste");
		}

		public static RandomizerCustomOptionsFile Load() {
			string path = GetCustomRandoCatsFileName();
			if (string.IsNullOrEmpty(path) || !File.Exists(path)) return new();
			try {
				using FileStream fs = new FileStream(path, FileMode.Open);
				XmlSerializer ser = new XmlSerializer(typeof(RandomizerCustomOptionsFile));
				object ob = ser.Deserialize(fs);
				return ob is RandomizerCustomOptionsFile log ? log : new();
			}
			catch (Exception e) {
				Logger.Log(LogLevel.Error, "Head2Head", "Failed to load custom randomizer options file: " + e.Message);
			}
			return new();
		}

		public bool Save() {
			try {
				using (FileStream fs = new FileStream(GetCustomRandoCatsFileName(), FileMode.Create)) {
					XmlSerializer ser = new XmlSerializer(typeof(RandomizerCustomOptionsFile));
					ser.Serialize(fs, this);
				}
				return true;
			}
			catch (Exception e) {
				Logger.Log(LogLevel.Warn, "Head2Head", "Failed to write custom randomizer options file: " + e.Message);
			}
			return false;
		}

		public List<RandomizerCustomOptionsCategory> Categories = new();

	}

	[Serializable]
	public class RandomizerCustomOptionsCategory {
		public string Name { get; set; }
		public RandomizerOptionsTemplate Options { get; set; }
	}
}
