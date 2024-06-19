using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.ControlPanel {
	internal struct SerializeImage {

		private static readonly Dictionary<string, string> _guiCache = new();

		public static void ClearCache() {
			_guiCache.Clear();
		}

		public static SerializeImage FromGui(string path) => new SerializeImage {
			atlas = "gui",
			path = path,
		};

		private string atlas;
		private string path;

		public string Id => $"{atlas}:{path}";

		public string Base64Image {
			get {
				if (_guiCache.TryGetValue(path, out string value)) {
					return value;
				}
				if (!GFX.Gui.Has(path)) {
					path = "Head2Head/Categories/Custom";
					if (_guiCache.TryGetValue(path, out value)) {
						return value;
					}
				}
				MTexture tex = GFX.Gui[path];
				string serialized = Convert.ToBase64String(tex.Texture.Metadata.Data);
				_guiCache[path] = serialized;
				return serialized;
			}
		}



	}
}
