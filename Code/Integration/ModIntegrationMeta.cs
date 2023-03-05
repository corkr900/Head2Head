using Celeste.Mod.Meta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.Integration {
	public class ModIntegrationMeta : IMeta {
		public FullgameMeta[] Fullgame;
		public ILMeta[] IndividualLevels;
	}

	public class FullgameMeta : IMeta {
		public string ID;
		public string Name;
		public string Icon;
		public string LevelSet;
		public string StartingMap;
		public bool? AllowCheatMode;
		public ObjectiveMeta[] Objectives;
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
	}

	public class PhaseMeta : IMeta {
		public string Map;
		public string Side;
		public ObjectiveMeta[] Objectives;
	}

	public class ObjectiveMeta : IMeta {
		public string Type;
		public string ID;
		public int Count;
		public string TimeLimit;
		public string Description;
	}
}
