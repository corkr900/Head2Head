using Celeste.Mod.Meta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.Integration {
	public class ModIntegrationMeta : IMeta {
		public FullgameCategoryMeta[] Fullgame;
		public ILMeta[] IndividualLevels;
		public RulesetMeta[] Rulesets;
	}

	public class RulesetMeta : IMeta {
		public string ID;
		public string Name;
		public string Role;
		public RulesetChapterMeta[] Chapters;
	}

	public class RulesetChapterMeta : IMeta {
		public string Name;
		public string Icon;
		public string MapSID;
		public RulesetSideMeta[] Sides;
	}

	public class RulesetSideMeta : IMeta {
		public string Name;
		public string Icon;
		public CategoryMeta[] Categories;
	}

	public class LevelListMeta : IMeta {
		public string SID;
		public string Side;
	}

	public class FullgameCategoryMeta : IMeta {
		public string ID;
		public string Name;
		public string Icon;
		public string LevelSet;
		public string StartingMap;
		public bool? AllowCheatMode;
		public ObjectiveMeta[] Objectives;
		public string[] Rules;
	}

	public class ILMeta : IMeta {
		public string Map;
		public string Side;
		public string[] RemoveCategories;
		public CategoryMeta[] AddCategories;
	}

	public class CategoryMeta : IMeta {
		public string ID;
		public string Name;
		public string Icon;
		public PhaseMeta[] Phases;
		public string[] Rules;
	}

	public class PhaseMeta : IMeta {
		public string Map;
		public string Side;
		public ObjectiveMeta[] Objectives;
	}

	public class ObjectiveMeta : IMeta {
		public string Type;
		public string ID;
		public string Side;
		public int Count;
		public string TimeLimit;
		public string Description;
	}
}
