using Celeste.Mod.Head2Head.Entities;
using Celeste.Mod.Head2Head.Shared;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.Head2Head.UI {
	public class H2HHudRenderer : HiresRenderer {

		private static Vector2 canvasSize = new Vector2(1920f, 1080f);
		public static float hudScale = 1f;  // TODO make HUD scale a setting
		public static float lineOffset = 52f;
		public static float bannerOpacity_beforematch = 1.0f;
		public static float bannerOpacity_inmatch = 0.02f;

		private float bannerOpacity {
			get {
				if (!ShouldRenderBanner) return 0f;
				if (PlayerStatus.Current.CurrentMatch?.State == MatchState.InProgress) return bannerOpacity_inmatch;
				return bannerOpacity_beforematch;
			}
		}

		public override void Render(Scene scene) {
			HiresRenderer.BeginRender();
			if (ShouldRenderBanner) RenderBanner(scene);
			HiresRenderer.EndRender();
			base.Render(scene);
		}

		private bool ShouldRenderBanner { get { return PlayerStatus.Current.CurrentMatch != null && ILSelector.ActiveSelector == null; } }

		private void RenderBanner(Scene scene) {
			MatchDefinition def = PlayerStatus.Current.CurrentMatch;
			bool showCreator = ShouldShowMatchCreatorOnBanner(def, scene);
			float opacity = bannerOpacity;
			Vector2 justify = new Vector2(0.5f, 0f);

			MTexture bannerBG = GFX.Gui["Head2Head/HUD/banner_bg"];
			Vector2 bannerPosition = new Vector2(canvasSize.X / 2f, -1f);
			if (!showCreator) bannerPosition -= Vector2.UnitY * (lineOffset - 16);
			Color bannerColor = Color.White;
			bannerColor = bannerColor * opacity;
			bannerBG.DrawJustified(bannerPosition, justify, bannerColor, hudScale);

			string matchCaption = def.Phases.Count == 0 ? "Unnamed Match" : def.Phases[0].Title;
			Vector2 captionPos = new Vector2(canvasSize.X / 2, 0f);
			Vector2 captionScale = Vector2.One * hudScale;
			Color captionColor = Color.Black;
			captionColor.A = (byte)(captionColor.A * opacity);
			ActiveFont.Draw(matchCaption, captionPos, justify, captionScale, captionColor);

			if (showCreator) {
				string matchowner = string.Format(Dialog.Get("Head2Head_hud_createdby"), def.Owner.Name);
				Vector2 matchPos = new Vector2(canvasSize.X / 2, lineOffset * hudScale);
				Vector2 matchScale = Vector2.One * hudScale * 0.6f;
				Color matchColor = Color.DarkCyan;
				matchColor.A = (byte)(matchColor.A * opacity);
				ActiveFont.Draw(matchowner, matchPos, justify, matchScale, matchColor);
			}
		}

		private bool ShouldShowMatchCreatorOnBanner(MatchDefinition def, Scene scene) {
			return def.State == MatchState.Staged && !(scene is Overworld);
		}
	}
}
