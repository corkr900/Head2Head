using Monocle;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Celeste.Mod.Head2Head.ControlPanel {

	public struct SerializeImage {

		private static readonly Dictionary<string, string> _guiCache = new();
		private static readonly string RepositoryURI = "https://github.com/corkr900/Head2Head";
		private static SerializeImage Default => new SerializeImage {
			atlas = "gui",
			path = "Head2Head/Categories/Custom",
		};

		public static void ClearCache() {
			_guiCache.Clear();
		}

		public static SerializeImage FromGui(string path, bool alwaysSendSource = false)
			=> string.IsNullOrEmpty(path) || !GFX.Gui.Has(path) ? Default
				: new SerializeImage {
					atlas = "gui",
					path = path,
					alwaysSendSource = alwaysSendSource,
				};

		public static SerializeImage FromGame(string path, bool alwaysSendSource = false)
			=> string.IsNullOrEmpty(path) || !GFX.Game.Has(path) ? Default
				: new SerializeImage {
					atlas = "game",
					path = path,
					alwaysSendSource = alwaysSendSource,
				};

		public static SerializeImage FromUrl(string url) => new SerializeImage {
			atlas = "url",
			path = url,
			alwaysSendSource = true,
		};

		private string atlas;
		private string path;
		private bool alwaysSendSource;

		/// <summary>
		/// A string that uniquely identifies the image
		/// </summary>
		public string Id => $"{atlas}:{path}";

		/// <summary>
		/// Flag indicating whether this image's content should always be sent or
		/// if only the ID should be sent unless the client requests the full data.
		/// </summary>
		[JsonInclude]
		public bool NeedRequestContent {
			get {
				if (alwaysSendSource) return false;
				switch (atlas) {
					case "gui":
					case "game":
						return !path.StartsWith("Head2Head");
					case "url":
					default:
						return false;
				}

			}
		}

		/// <summary>
		/// The string to be used in the src attribute in an HTML img element
		/// </summary>
		public string ImgSrc {
			get {
				if (NeedRequestContent && !alwaysSendSource) return "";
				if (path.StartsWith("Head2Head")) {
					if (atlas == "gui") {
						return $"{RepositoryURI}/blob/main/Graphics/Atlases/Gui/{path}.png?raw=true";
					}
					if (atlas == "game") {
						return $"{RepositoryURI}/blob/main/Graphics/Atlases/Gameplay/{path}.png?raw=true";
					}
				}
				return atlas == "url" ? path
					: $"data:image/jpeg;base64,${Base64Image()}";
			}
		}

		private string Base64Image() {
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
