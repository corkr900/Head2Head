using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.Shared {

	public class CustomMatchDefinition {
		internal static Dictionary<string, CustomMatchDefinition> templates = new Dictionary<string, CustomMatchDefinition>();

		internal void RegisterAsTemplate() {
			if (templates.ContainsKey(Key)) templates.Remove(Key);
			templates.Add(Key, this);
		}

		public string Key;
		public string IconPath;
		public string DisplayName;
	}
}
