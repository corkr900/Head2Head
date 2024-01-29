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
		internal static Ruleset UsingRuleset = null;

		public override bool IsStart(Overworld overworld, Overworld.StartMode start) {
			if (Start) {
				Start = false;
				Add(new Coroutine(Enter(null)));
				return true;
			}

			return false;
		}

		public override IEnumerator Enter(Oui from) {
			Ruleset newRuleset = Ruleset.Current;
			if (newRuleset != UsingRuleset) {
				UsingRuleset = newRuleset;
				OuiRunSelectILChapterSelect.UsingLevelSet = UsingRuleset.LevelSets[0];
			}
			Audio.Play("event:/ui/world_map/icon/select");
			Overworld.Goto<OuiRunSelectILChapterSelect>();
			yield break;
		}

		public override IEnumerator Leave(Oui next) {
			yield break;
		}

		public static string LevelSetIdxToSet(int levelSetIndex) {
			return levelSetIndex < 0 ? null : levelSetIndex >= UsingRuleset.LevelSets.Count ? null : UsingRuleset.LevelSets[levelSetIndex].LevelSet;
		}

		public static int LevelSetToIdx(string setID) {
			for (int i = 0; i < UsingRuleset.LevelSets.Count; i++) {
				if (UsingRuleset.LevelSets[i].LevelSet == setID) return i;
			}
			return 0;
		}

		public static int GetNumOptionsInSet(int levelSetIdx) {
			if (UsingRuleset == null) return 0;
			if (levelSetIdx < 0 || levelSetIdx >= UsingRuleset.LevelSets.Count) {
				return 0;
			}
			return UsingRuleset.LevelSets[levelSetIdx].Chapters.Count;
		}

		internal static RunOptionsLevelSet PreviousLevelSet(int levelSetIn, ref int levelSetOut, bool includeHidden = false) {
            if (UsingRuleset == null || UsingRuleset.LevelSets.Count == 0) {
				levelSetOut = 0;
				return null;
			}
			levelSetOut = levelSetIn;
			while (--levelSetOut != levelSetIn) {
				if (levelSetOut < 0) {
					levelSetOut = UsingRuleset.LevelSets.Count - 1;
				}
				RunOptionsLevelSet set = UsingRuleset.LevelSets[levelSetOut];
				if (includeHidden || !set.Hidden) return set;
			}
			return UsingRuleset.LevelSets[levelSetOut];
		}

		internal static RunOptionsLevelSet NextLevelSet(int levelSetIn, ref int levelSetOut, bool includeHidden = false) {
			if (UsingRuleset == null || UsingRuleset.LevelSets.Count == 0) {
				levelSetOut = 0;
				return null;
			}
			levelSetOut = levelSetIn;
			while (++levelSetOut != levelSetIn) {
				if (levelSetOut >= UsingRuleset.LevelSets.Count) {
					levelSetOut = 0;
				}
				RunOptionsLevelSet set = UsingRuleset.LevelSets[levelSetOut];
				if (includeHidden || !set.Hidden) return set;
			}
			return UsingRuleset.LevelSets[levelSetOut];
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
		public readonly int InternalID = Ruleset.NewInternalID();
		public string CollabLobby {
			get {
				return Data == null ? null : CollabUtils2Integration.GetLobbyForMap?.Invoke(Data.SID);
			}
		}
		public string CollabLevelSetForLobby {
			get {
				return CollabUtils2Integration.GetLobbyLevelSet?.Invoke(Data?.SID);
			}
		}

		public string DisplayName => Title?.DialogCleanOrNull()
			?? Title?.SpacedPascalCase()
			?? Data?.Name?.DialogCleanOrNull()
			?? Data?.Name?.SpacedPascalCase()
			?? "UNNAMED_CHAPTER";

		public string IconSafe => !string.IsNullOrEmpty(Icon) && GFX.Gui.Has(Icon) ? Icon : "areas/null";

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
		internal MatchTemplate Template;
	}

}
