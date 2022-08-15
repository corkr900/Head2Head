﻿using Celeste.Mod.Head2Head.Entities;
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
		public static readonly int listMarginX = 32;
		public static readonly int listMarginY = 150;
		public static readonly float listBgScale = 0.5f;

		MTexture Banner;
		MTexture BannerLeft;
		MTexture BannerMid;
		MTexture BannerRight;

		MTexture ListBG;

		public H2HHudRenderer() : base() {
			Banner = GFX.Gui["Head2Head/HUD/banner_bg"];
			int w = Banner.Width / 3;
			BannerLeft = new MTexture(Banner, 0, 0, w, Banner.Height);
			BannerMid = new MTexture(Banner, w, 0, w, Banner.Height);
			BannerRight = new MTexture(Banner, Banner.Width - w, 0, w, Banner.Height);

			ListBG = GFX.Gui["areaselect/card"];
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
			bool showCreator = ShouldShowMatchCreatorOnBanner(scene, def);
			float _bannerOpacity = Opacity(scene, def);
			Vector2 justify = new Vector2(0.5f, 0f);
			float hudScale = Head2HeadModule.Settings.HudScale;

			if (_bannerOpacity > 0.001f) {
				string matchCaption = def.Phases.Count == 0 ? "Unnamed Match" : def.Phases[0].Title;
				Vector2 captionPos = new Vector2(CanvasSize.X / 2, 0f);
				Vector2 captionScale = Vector2.One * hudScale;
				Color captionColor = Color.Black;

				float midWidth = Math.Max(ActiveFont.Measure(matchCaption).X - (BannerLeft.Width * 2) + titleMarginX * 2, 0) * hudScale;
				Vector2 bannerPositionL = new Vector2((CanvasSize.X - midWidth) / 2f, -1f);
				Vector2 bannerPositionR = new Vector2((CanvasSize.X + midWidth) / 2f, -1f);
				if (!showCreator) {
					bannerPositionL -= Vector2.UnitY * (lineOffset - 16);
					bannerPositionR -= Vector2.UnitY * (lineOffset - 16);
				}
				BannerLeft.DrawJustified(bannerPositionL, new Vector2(1, 0), Color.White * _bannerOpacity, hudScale);
				BannerMid.DrawJustified(bannerPositionL, new Vector2(0, 0), Color.White * _bannerOpacity, new Vector2(midWidth / (BannerMid.Width * hudScale), 1) * hudScale);
				BannerRight.DrawJustified(bannerPositionR, new Vector2(0, 0), Color.White * _bannerOpacity, hudScale);

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
			Color listBGColor = Color.White;
			listBGColor.A = (byte)(opacity * 127);
			const float lineHeight = 24f;
			Vector2 cornerMargin = new Vector2(-8f, 8f);

			if (opacity > 0.001f) {
				List<Tuple<string, ResultCategory, long>> players = new List<Tuple<string, ResultCategory, long>>();
				float maxWidth = 0;
				foreach (PlayerID id in def.Players) {
					string name = id.Name;
					ResultCategory cat = def.GetPlayerResultCat(id);
					long timer = def.Result == null || cat != ResultCategory.Completed ? 0 : def.Result[id]?.FileTimeTotal ?? 0;
					string statusstr = cat == ResultCategory.Completed ? Dialog.FileTime(timer) : Util.TranslatedMatchResult(cat);
					string text = string.Format("{0} | {1}", name, statusstr);
					players.Add(new Tuple<string, ResultCategory, long>(
						text,
						cat,
						cat == ResultCategory.Completed ? def.Result[id]?.FileTimeTotal ?? long.MaxValue : long.MaxValue
					));
					float width = ActiveFont.Measure(text).X;
					maxWidth = Math.Max(width, maxWidth);
				}
				maxWidth *= listPlayerScale.X;
				string title = string.Format(Dialog.Get("Head2Head_hud_playerlist_title"), def.Players.Count);
				maxWidth = Math.Max(maxWidth, ActiveFont.Measure(title).X * listTitleScale.X);
				maxWidth *= hudScale;

				Vector2 bgPos = new Vector2(CanvasSize.X, 0);
				bgPos.X -= maxWidth + listMarginX;
				bgPos.Y += (players.Count * lineHeight + listMarginY) * hudScale;
				bgPos += cornerMargin;

				float bgscale = listBgScale * hudScale;
				bgscale = Math.Max(bgscale, (CanvasSize.X - bgPos.X) / ListBG.Width);
				bgscale = Math.Max(bgscale, bgPos.Y / ListBG.Height);

				ListBG.DrawJustified(bgPos, new Vector2(0, 1), Color.White * opacity, bgscale);

				ActiveFont.Draw(title, new Vector2(CanvasSize.X, 0) + cornerMargin,
					Vector2.UnitX, listTitleScale * hudScale, listTitleColor * opacity);

				// Sort DNF to the bottom and finished to the top, sorting by finish time
				players.Sort((Tuple<string, ResultCategory, long> t1, Tuple<string, ResultCategory, long> t2) => {
					if (t1.Item2 == ResultCategory.DNF) return t2.Item2 == ResultCategory.DNF ? 0 : 1;
					if (t2.Item2 == ResultCategory.DNF) return -1;
					if (t1.Item2 != ResultCategory.Completed) return t2.Item2 == ResultCategory.Completed ? -1 : 0;
					if (t2.Item2 != ResultCategory.Completed) return 1;
					return t1.Item3 > t2.Item3 ? 1 : t1.Item3 < t2.Item3 ? -1 : 0;
				});

				float ypos = 42 * hudScale;
				foreach (Tuple<string, ResultCategory, long> t in players) {
					Color color = t.Item2 == ResultCategory.Completed ? Color.DarkGreen
						: t.Item2 == ResultCategory.DNF ? Color.DarkRed : Color.DarkCyan;
					ActiveFont.Draw(t.Item1, new Vector2(CanvasSize.X, ypos) + cornerMargin,
						Vector2.UnitX, listPlayerScale * hudScale, color * opacity);
					ypos += lineHeight * hudScale;
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
