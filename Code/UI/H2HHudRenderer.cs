using Celeste.Mod.Head2Head.Entities;
using Celeste.Mod.Head2Head.Shared;
using Microsoft.Xna.Framework;
using Monocle;
using System.Collections.Generic;

namespace Celeste.Mod.Head2Head.UI {
	public class H2HHudRenderer : HiresRenderer {

		private static Vector2 canvasSize = new Vector2(1920f, 1080f);
		public static float lineOffset = 52f;

		private float Opacity(Scene scene, MatchDefinition def) {
			if (def == null) return 0.0f;
			if (scene is Overworld) return Head2HeadModule.Settings.HudOpacityInOverworld;
			if (def.State == MatchState.Completed) return Head2HeadModule.Settings.HudOpacityNotInMatch;
			if (def.State == MatchState.InProgress && def.BeginInstant <= System.DateTime.Now) {
				return Head2HeadModule.Settings.HudOpacityInMatch;
			}
			return Head2HeadModule.Settings.HudOpacityNotInMatch;
		}

		public override void Render(Scene scene) {
			MatchDefinition def = PlayerStatus.Current.CurrentMatch;
			HiresRenderer.BeginRender();
			if (ShouldRenderBanner(scene, def)) RenderBanner(scene, def);
			if (ShouldShowPlayerList(scene, def)) RenderPlayerList(scene, def);
			HiresRenderer.EndRender();
			base.Render(scene);
		}

		#region Banner

		private bool ShouldRenderBanner(Scene scene, MatchDefinition def) {
			return def != null && ILSelector.ActiveSelector == null;
		}

		private void RenderBanner(Scene scene, MatchDefinition def) {
			// TODO (!!!) stretch banner width to fit the title
			bool showCreator = ShouldShowMatchCreatorOnBanner(scene, def);
			float _bannerOpacity = Opacity(scene, def);
			Vector2 justify = new Vector2(0.5f, 0f);
			float hudScale = Head2HeadModule.Settings.HudScale;

			if (_bannerOpacity > 0.001f) {
				MTexture bannerBG = GFX.Gui["Head2Head/HUD/banner_bg"];
				Vector2 bannerPosition = new Vector2(canvasSize.X / 2f, -1f);
				if (!showCreator) bannerPosition -= Vector2.UnitY * (lineOffset - 16);
				Color bannerColor = Color.White;
				bannerBG.DrawJustified(bannerPosition, justify, bannerColor * _bannerOpacity, hudScale);

				string matchCaption = def.Phases.Count == 0 ? "Unnamed Match" : def.Phases[0].Title;
				Vector2 captionPos = new Vector2(canvasSize.X / 2, 0f);
				Vector2 captionScale = Vector2.One * hudScale;
				Color captionColor = Color.Black;
				ActiveFont.Draw(matchCaption, captionPos, justify, captionScale, captionColor * _bannerOpacity);

				if (showCreator) {
					string matchowner = string.Format(Dialog.Get("Head2Head_hud_createdby"), def.Owner.Name);
					Vector2 matchPos = new Vector2(canvasSize.X / 2, lineOffset * hudScale);
					Vector2 matchScale = Vector2.One * hudScale * 0.6f;
					Color matchColor = Color.DarkCyan;
					ActiveFont.Draw(matchowner, matchPos, justify, matchScale, matchColor * _bannerOpacity);
				}
			}
		}

		private bool ShouldShowMatchCreatorOnBanner(Scene scene, MatchDefinition def) {
			return def.State == MatchState.Staged && !(scene is Overworld);
		}

		#endregion

		#region Player List

		private bool ShouldShowPlayerList(Scene scene, MatchDefinition def) {
			return def != null && ILSelector.ActiveSelector == null;
		}

		private void RenderPlayerList(Scene scene, MatchDefinition def) {
			float opacity = Opacity(scene, def);
			float hudScale = Head2HeadModule.Settings.HudScale;

			Vector2 listTitleScale = Vector2.One * 0.7f;
			Vector2 listPlayerScale = Vector2.One * 0.5f;
			Color listTitleColor = Color.Black;
			Color listPlayerColor = Color.DarkCyan;
			const float lineHeight = 24f;
			Vector2 margin = new Vector2(-8f, 8f);

			if (opacity > 0.001f) {

				// TODO (!!!) player list background

				ActiveFont.Draw(string.Format(Dialog.Get("Head2Head_hud_playerlist_title"), def.Players.Count),
					new Vector2(canvasSize.X, 0) + margin, Vector2.UnitX, listTitleScale * hudScale, listTitleColor * opacity);

				float ypos = 42;
				foreach (PlayerID id in def.Players) {
					string name = id.Name;
					ResultCategory cat = def.GetPlayerResultCat(id);
					long timer = def.Result == null || cat != ResultCategory.Completed ? 0 : def.Result[id]?.FileTimeTotal ?? 0;
					string statusstr = cat == ResultCategory.Completed ? Dialog.FileTime(timer) : Util.TranslatedMatchResult(cat);
					ActiveFont.Draw(string.Format("{0} | {1}", name, statusstr),
						new Vector2(canvasSize.X, ypos) + margin, Vector2.UnitX, listPlayerScale * hudScale, listPlayerColor * opacity);
					ypos += lineHeight;
				}
			}
		}

		#endregion
	}
}
