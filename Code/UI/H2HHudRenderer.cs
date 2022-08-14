using Celeste.Mod.Head2Head.Entities;
using Celeste.Mod.Head2Head.Shared;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;

namespace Celeste.Mod.Head2Head.UI {
	public class H2HHudRenderer : HiresRenderer {

		public static Vector2 CanvasSize { get { return new Vector2(1920f, 1080f); } }
		public static readonly int lineOffset = 52;
		public static readonly int titleMarginX = 80;

		MTexture Banner;
		MTexture BannerLeft;
		MTexture BannerMid;
		MTexture BannerRight;

		public H2HHudRenderer() : base() {
			Banner = GFX.Gui["Head2Head/HUD/banner_bg"];
			int w = Banner.Width / 3;
			BannerLeft = new MTexture(Banner, 0, 0, w, Banner.Height);
			BannerMid = new MTexture(Banner, w, 0, w, Banner.Height);
			BannerRight = new MTexture(Banner, Banner.Width - w, 0, w, Banner.Height);
		}

		public static float Opacity(Scene scene, MatchDefinition def) {
			if (def == null) return 0.0f;
			if (scene is Overworld) return Head2HeadModule.Settings.HudOpacityInOverworld;
			if (def.State == MatchState.Completed) return Head2HeadModule.Settings.HudOpacityNotInMatch;
			if (def.State == MatchState.InProgress && def.BeginInstant <= System.DateTime.Now) {
				return Head2HeadModule.Settings.HudOpacityInMatch;
			}
			return Head2HeadModule.Settings.HudOpacityNotInMatch;
		}

		public override void BeforeRender(Scene scene) {
			if (DrawToBuffer) {
				Engine.Graphics.GraphicsDevice.SetRenderTarget(Buffer);
				RenderContent(scene);
			}
		}

		public override void RenderContent(Scene scene) {
			MatchDefinition def = PlayerStatus.Current.CurrentMatch;
			HiresRenderer.BeginRender();
			if (ShouldRenderBanner(scene, def)) RenderBanner(scene, def);
			if (ShouldShowPlayerList(scene, def)) RenderPlayerList(scene, def);
			if (ShouldShowCountdown(scene, def)) RenderCountdown(scene, def);
			HiresRenderer.EndRender();
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
				string matchCaption = def.Phases.Count == 0 ? "Unnamed Match" : def.Phases[0].Title;
				Vector2 captionPos = new Vector2(CanvasSize.X / 2, 0f);
				Vector2 captionScale = Vector2.One * hudScale;
				Color captionColor = Color.Black;

				float midWidth = Math.Max(ActiveFont.Measure(matchCaption).X - (BannerLeft.Width * 2) + titleMarginX * 2, 0);
				Vector2 bannerPositionL = new Vector2((CanvasSize.X - midWidth) / 2f, -1f);
				Vector2 bannerPositionR = new Vector2((CanvasSize.X + midWidth) / 2f, -1f);
				if (!showCreator) {
					bannerPositionL -= Vector2.UnitY * (lineOffset - 16);
					bannerPositionR -= Vector2.UnitY * (lineOffset - 16);
				}
				BannerLeft.DrawJustified(bannerPositionL, new Vector2(1, 0));
				BannerMid.DrawJustified(bannerPositionL, new Vector2(0, 0), Color.White, new Vector2(midWidth / BannerMid.Width, 1));
				BannerRight.DrawJustified(bannerPositionR, new Vector2(0, 0));

				ActiveFont.Draw(matchCaption, captionPos, justify, captionScale, captionColor * _bannerOpacity);

				if (showCreator) {
					string matchowner = string.Format(Dialog.Get("Head2Head_hud_createdby"), def.Owner.Name);
					Vector2 matchPos = new Vector2(CanvasSize.X / 2, lineOffset * hudScale);
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
					new Vector2(CanvasSize.X, 0) + margin, Vector2.UnitX, listTitleScale * hudScale, listTitleColor * opacity);

				float ypos = 42;
				foreach (PlayerID id in def.Players) {
					string name = id.Name;
					ResultCategory cat = def.GetPlayerResultCat(id);
					long timer = def.Result == null || cat != ResultCategory.Completed ? 0 : def.Result[id]?.FileTimeTotal ?? 0;
					string statusstr = cat == ResultCategory.Completed ? Dialog.FileTime(timer) : Util.TranslatedMatchResult(cat);
					ActiveFont.Draw(string.Format("{0} | {1}", name, statusstr),
						new Vector2(CanvasSize.X, ypos) + margin, Vector2.UnitX, listPlayerScale * hudScale, listPlayerColor * opacity);
					ypos += lineHeight;
				}
			}
		}

		#endregion

		#region Countdown

		private bool ShouldShowCountdown(Scene scene, MatchDefinition def) {
			if (def == null) return false;
			if (def.State != MatchState.InProgress) return false;
			DateTime now = DateTime.Now;
			if (def.BeginInstant < now) return false;
			TimeSpan remain = def.BeginInstant - now;
			int cdIdx = (int)Math.Ceiling(remain.TotalSeconds);
			if (cdIdx <= 0 || cdIdx > 5) return false;

			return true;
		}

		public void RenderCountdown(Scene scene, MatchDefinition def) {
			if (def == null) return;
			float _bannerOpacity = Opacity(scene, PlayerStatus.Current.CurrentMatch);
			Vector2 justify = new Vector2(0.5f, 0.5f);
			float hudScale = Head2HeadModule.Settings.HudScale;
			DateTime now = DateTime.Now;
			if (def.BeginInstant < now) return;
			TimeSpan remain = def.BeginInstant - now;
			int cdIdx = (int)Math.Ceiling(remain.TotalSeconds);
			if (cdIdx <= 0 || cdIdx > 5) return;

			MTexture cdBG = GFX.Gui["Head2Head/Countdown/" + cdIdx.ToString()];
			Vector2 cdPos = CanvasSize / 2f;
			Color cdColor = Color.White;
			cdBG.DrawJustified(cdPos, justify, cdColor * _bannerOpacity, hudScale);
		}

		#endregion
	}
}
