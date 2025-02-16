using Celeste.Mod.Head2Head.Entities;
using Celeste.Mod.Head2Head.IO;
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
		public static readonly int listMargin = 32;

		MTexture Banner;
		MTexture BannerLeft;
		MTexture BannerMid;
		MTexture BannerRight;

		MTexture ListTitleBG;
		MTexture ListBG;

		public H2HHudRenderer() : base() {
			Banner = GFX.Gui["Head2Head/HUD/banner_bg"];
			int w = Banner.Width / 3;
			BannerLeft = new MTexture(Banner, 0, 0, w, Banner.Height);
			BannerMid = new MTexture(Banner, w, 0, w, Banner.Height);
			BannerRight = new MTexture(Banner, Banner.Width - w, 0, w, Banner.Height);

			ListTitleBG = GFX.Gui["Head2Head/HUD/playerlist_title_bg"];
			ListBG = GFX.Gui["Head2Head/HUD/playerlist_bg"];
		}

		public static float Opacity(Scene scene, MatchDefinition def) {
			if (def == null) return 0.0f;
			if (scene is Overworld) return Head2HeadModule.Settings.HudOpacityInOverworld;
			GlobalAreaKey area = PlayerStatus.Current.CurrentArea;
			if (area.Equals(GlobalAreaKey.Head2HeadLobby)) return Head2HeadModule.Settings.HudOpacityNotInMatch;
			ResultCategory cat = def.GetPlayerResultCat(PlayerID.MyIDSafe);
			if (cat == ResultCategory.Completed || cat == ResultCategory.DNF) return Head2HeadModule.Settings.HudOpacityNotInMatch;
			return Head2HeadModule.Settings.HudOpacityInMatch;
		}

		public override void BeforeRender(Scene scene) {
			if (DrawToBuffer) {
				Engine.Graphics.GraphicsDevice.SetRenderTarget(Buffer);
				RenderContent(scene);
			}
		}

		public override void Render(Scene scene) {
			if (!DrawToBuffer) {
				RenderContent(scene);
			}
		}

		public override void RenderContent(Scene scene) {
			MatchDefinition def = PlayerStatus.Current.CurrentMatch;
			HiresRenderer.BeginRender();
			if (ShouldRenderBanner(scene, def)) RenderBanner(scene, def);
			if (ShouldShowPlayerList(scene, def)) RenderPlayerList(scene, def);
			if (ShouldShowCountdown(scene, def)) RenderCountdown(scene, def);
			if (ShouldShowMatchPass(scene, def)) RenderMatchPass(scene, def);
			if (ShouldShowDebugInfo(scene, def)) RenderDebugInfo(scene, def);
			if (ShouldShowLobbyRaceInfo(scene, def)) RenderLobbyRaceInfo(scene, def);
			HiresRenderer.EndRender();
		}

		#region Banner

		private bool ShouldRenderBanner(Scene scene, MatchDefinition def) {
			// TODO handle other selectors
			return def != null && ILSelector.ActiveSelector == null && FullgameSelector.ActiveSelector == null;
		}

		private void RenderBanner(Scene scene, MatchDefinition def) {
			bool showCreator = ShouldShowMatchCreatorOnBanner(scene, def);
			float _bannerOpacity = Opacity(scene, def);
			Vector2 justify = new Vector2(0.5f, 0f);
			float hudScale = Head2HeadModule.Settings.HudScale;

			if (_bannerOpacity > 0.001f) {
				string matchCaption = def.MatchDisplayName;
				Vector2 captionPos = new Vector2(CanvasSize.X / 2, 0f);
				Vector2 captionScale = Vector2.One * hudScale;
				Color captionColor = Color.Black;

				float midWidth = Math.Max(ActiveFont.Measure(matchCaption).X - (BannerLeft.Width * 2) + titleMarginX * 2, 0) * hudScale;
				float anchorY = 0;
				if (!showCreator) {
					//float offset = Vector2.UnitY * (lineOffset - 16);
					anchorY = (lineOffset - 16) / (float)BannerMid.Height;
				}
				Vector2 bannerPositionL = new Vector2((CanvasSize.X - midWidth) / 2f, -1f);
				Vector2 bannerPositionR = new Vector2((CanvasSize.X + midWidth) / 2f, -1f);
				BannerLeft.DrawJustified(bannerPositionL, new Vector2(1, anchorY), Color.White * _bannerOpacity, hudScale);
				BannerMid.DrawJustified(bannerPositionL, new Vector2(0, anchorY), Color.White * _bannerOpacity, new Vector2(midWidth / (BannerMid.Width * hudScale), 1) * hudScale);
				BannerRight.DrawJustified(bannerPositionR, new Vector2(0, anchorY), Color.White * _bannerOpacity, hudScale);

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
				float listWidth = 0;
				foreach (PlayerID id in def.Players) {
					ResultCategory cat = def.GetPlayerResultCat(id);
					string name = id.Name;
					string text = string.Format("{0} | {1}", name, GetPlayerDetail(id, cat, def));
					players.Add(new Tuple<string, ResultCategory, long>(
						text,
						cat,
						cat == ResultCategory.Completed ? def.Result[id]?.FileTimeTotal ?? long.MaxValue : long.MaxValue
					));
					float width = ActiveFont.Measure(text).X;
					listWidth = Math.Max(width, listWidth);
				}
				listWidth *= listPlayerScale.X;
				string title = string.Format(Dialog.Get("Head2Head_hud_playerlist_title"), def.Players.Count);
				Vector2 titleSize = ActiveFont.Measure(title) * hudScale;
				listWidth *= hudScale;


				if (players.Count == 0) {
					// figure out the title bg position / scale
					Vector2 titleBgPos = new Vector2(CanvasSize.X - titleSize.X, titleSize.Y) + cornerMargin;
					Vector2 titlebgSize = new Vector2(CanvasSize.X - titleBgPos.X, titleBgPos.Y);
					MTexture titleBGSubtex = GetSubtexLowerLeft(ListTitleBG, titlebgSize, hudScale);
					Vector2 titleBGScale = new Vector2(titlebgSize.X / titleBGSubtex.Width, titlebgSize.Y / titleBGSubtex.Height);
					// Draw the BG and title
					ListTitleBG.DrawJustified(titleBgPos, new Vector2(0, 1), Color.White * opacity, titleBGScale);
					ActiveFont.Draw(title, new Vector2(CanvasSize.X, 0) + cornerMargin,
						Vector2.UnitX, listTitleScale * hudScale, listTitleColor * opacity);
				}
				else {
					// initial title bg position
					Vector2 titleBgPos = new Vector2(CanvasSize.X - titleSize.X, titleSize.Y) + cornerMargin;

					// figure out the list bg position / scale
					Vector2 bgPos = new Vector2(CanvasSize.X, 0);
					bgPos.X -= listWidth + listMargin * hudScale;
					bgPos.Y += (players.Count * lineHeight + listMargin) * hudScale + titleBgPos.Y;
					bgPos += cornerMargin;

					Vector2 bgSize = new Vector2(CanvasSize.X - bgPos.X, bgPos.Y - titleBgPos.Y);
					MTexture listBGSubtex = GetSubtexLowerLeft(ListBG, bgSize, hudScale);
					Vector2 listBGScale = new Vector2(bgSize.X / listBGSubtex.Width, bgSize.Y / listBGSubtex.Height);

					// draw title bg, ensuring it's at least a little wider than the list
					titleBgPos.X = Calc.Min(titleBgPos.X, bgPos.X - (8 * hudScale));
					Vector2 titlebgSize = new Vector2(CanvasSize.X - titleBgPos.X, titleBgPos.Y);
					MTexture titleBGSubtex = GetSubtexLowerLeft(ListTitleBG, titlebgSize, hudScale);
					Vector2 titleBGScale = new Vector2(titlebgSize.X / titleBGSubtex.Width, titlebgSize.Y / titleBGSubtex.Height);

					// draw bgs and title
					ListTitleBG.DrawJustified(titleBgPos, new Vector2(0, 1), Color.White * opacity, titleBGScale);
					ActiveFont.Draw(title, new Vector2(CanvasSize.X, 0) + cornerMargin,
						Vector2.UnitX, listTitleScale * hudScale, listTitleColor * opacity);
					listBGSubtex.DrawJustified(bgPos, new Vector2(0, 1), Color.White * opacity, listBGScale);

					// Sort DNF to the bottom and finished to the top, sorting by finish time
					players.Sort((Tuple<string, ResultCategory, long> t1, Tuple<string, ResultCategory, long> t2) => {
						if (t1.Item2 == ResultCategory.DNF) return t2.Item2 == ResultCategory.DNF ? 0 : 1;
						if (t2.Item2 == ResultCategory.DNF) return -1;
						if (t1.Item2 != ResultCategory.Completed) return t2.Item2 == ResultCategory.Completed ? -1 : 0;
						if (t2.Item2 != ResultCategory.Completed) return 1;
						return t1.Item3 > t2.Item3 ? 1 : t1.Item3 < t2.Item3 ? -1 : 0;
					});

					// Draw the list text
					float ypos = titleBgPos.Y;
					foreach (Tuple<string, ResultCategory, long> t in players) {
						Color color = t.Item2 == ResultCategory.Completed ? Color.DarkGreen
							: t.Item2 == ResultCategory.DNF ? Color.DarkRed : Color.DarkCyan;
						ActiveFont.Draw(t.Item1, new Vector2(CanvasSize.X, ypos) + cornerMargin,
							Vector2.UnitX, listPlayerScale * hudScale, color * opacity);
						ypos += lineHeight * hudScale;
					}
				}
			}
		}

		private MTexture GetSubtexLowerLeft(MTexture source, Vector2 targetSize, float scale) {
			int actualWidth = (int)Calc.Min(source.Width, targetSize.X / scale);
			int actualHeight = (int)Calc.Min(source.Height, targetSize.Y / scale);
			return source.GetSubtexture(0, source.Height - actualHeight, actualWidth, actualHeight);
		}

		private string GetPlayerDetail(PlayerID id, ResultCategory cat, MatchDefinition def) {
			if (cat == ResultCategory.Completed) {
				if (def.Phases[0].HasTimeLimit) {
					MatchResultPlayer res = def.Result[id];
					if (res.FinalRoom != "h2h_chapter_completed") {
						return res.FinalRoom;
					}
				}
				long timer = def.Result == null ? 0 : def.Result[id]?.FileTimeTotal ?? 0;
				return Dialog.FileTime(timer);
			}
			if (cat == ResultCategory.InMatch) {
				PlayerStatus stat = id.Equals(PlayerID.MyID) ? PlayerStatus.Current
					: Head2HeadModule.knownPlayers.ContainsKey(id) ? Head2HeadModule.knownPlayers[id]
					: null;
				if (stat != null) {
					if (def.Phases[0].HasTimeLimit) {
						MatchObjective ob = def.Phases[0].Objectives[0];
						long timeRemaining = Math.Max(stat.FileTimerAtMatchBegin + ob.AdjustedTimeLimit(id) - stat.CurrentFileTimer, 0);
						return string.Format(Dialog.Get("Head2Head_hud_playerlist_timeremain"), Util.ReadableTimeSpanTitle(timeRemaining));
					}
					int strawbsTotal = 0;
					int strawbsCollected = 0;
					int curPhase = stat.CurrentPhase(id);
					if (curPhase >= 0) {
						foreach (MatchPhase ph in def.Phases) {
							if (ph.Order != curPhase) continue;
							foreach (MatchObjective obj in ph.Objectives) {
								if (obj.CollectableGoal > 0) {
									strawbsTotal += obj.CollectableGoal;
									int idx = stat.Objectives.FindIndex((H2HMatchObjectiveState s) => s.ObjectiveID == obj.ID);
									if (idx < 0) continue;
									strawbsCollected += stat.Objectives[idx].CountCollectables();
								}
							}
						}
						if (strawbsTotal > 0) {
							return string.Format("{0}/{1}", strawbsCollected, strawbsTotal);
						}
					}
				}
			}
			if (cat == ResultCategory.DNF) {
				DNFReason reason = DNFReason.MatchCancel;
				if (def.Result?.players?.ContainsKey(id) == true) {
					reason = def.Result[id].DNFreason;
				}
				return string.Format("{0} ({1})", Util.TranslatedMatchResult(cat), Util.TranslatedDNFReason(reason));
			}
			return Util.TranslatedMatchResult(cat);
		}

		#endregion

		#region Countdown

		private bool ShouldShowCountdown(Scene scene, MatchDefinition def) {
			if (def == null) return false;
			if (def.State != MatchState.InProgress) return false;
			if (def.GetPlayerResultCat(PlayerID.MyIDSafe) == ResultCategory.NotJoined) return false;
			DateTime now = SyncedClock.Now;
			if (def.BeginInstant < now) return false;
			TimeSpan remain = def.BeginInstant - now;
			int cdIdx = (int)Math.Ceiling(remain.TotalSeconds);
			if (cdIdx <= 0 || cdIdx > 5) return false;
			GlobalAreaKey area = PlayerStatus.Current.CurrentArea;
			return area.IsOverworld || area.Equals(GlobalAreaKey.Head2HeadLobby);
		}

		public void RenderCountdown(Scene scene, MatchDefinition def) {
			if (def == null) return;
			float _bannerOpacity = Opacity(scene, PlayerStatus.Current.CurrentMatch);
			Vector2 justify = new Vector2(0.5f, 0.5f);
			float hudScale = Head2HeadModule.Settings.HudScale;
			DateTime now = SyncedClock.Now;
			if (def.BeginInstant < now) return;
			TimeSpan remain = def.BeginInstant - now;
			int displaynum = (int)Math.Ceiling(remain.TotalSeconds);
			if (displaynum <= 0 || displaynum > 15) return;

			ActiveFont.DrawOutline(displaynum.ToString(), CanvasSize / 2f, new Vector2(.5f, .5f), Vector2.One * 5f, Color.White, 5f, Color.Black);
		}

		private bool ShouldShowMatchPass(Scene scene, MatchDefinition def) {
			return RoleLogic.HasBTAMatchPass && PlayerStatus.Current.CurrentArea.Equals(GlobalAreaKey.Head2HeadLobby);
		}

		private void RenderMatchPass(Scene scene, MatchDefinition def) {
			float hudScale = Head2HeadModule.Settings.HudScale;
			Vector2 justify = new Vector2(0, 1);
			MTexture img = GFX.Gui["Head2Head/HUD/MatchTicket"];
			Vector2 pos = new Vector2(0, CanvasSize.Y);
			Color color = Color.White;
			img.DrawJustified(pos, justify, color, hudScale);
		}

		#endregion

		#region Debug Info

		private bool ShouldShowDebugInfo(Scene scene, MatchDefinition def) {
			return def != null && RoleLogic.IsDebug;
		}

		private void RenderDebugInfo(Scene scene, MatchDefinition def) {
			const float scale = 0.5f;

			List<string> deftext = new List<string>();
			List<string> ststext = new List<string>();
			deftext.Add(string.Format("{0} ({1})", def.MatchDisplayName, def.MatchID));
			ststext.Add(string.Format("{0}", def.GetPlayerResultCat(PlayerID.MyIDSafe)));
			foreach (MatchPhase ph in def.Phases) {
				deftext.Add(string.Format("> {0}; cat={1}; area={2}; fg={3}, ord={4}", ph.Title, ph.category, ph.Area.SID, ph.Fullgame, ph.Order));
				List<H2HMatchPhaseState> statelist = PlayerStatus.Current.Phases.FindAll(
					(H2HMatchPhaseState s) => s.PhaseID == ph.ID);
				if (statelist.Count > 0) {
					H2HMatchPhaseState state = statelist[0];
					ststext.Add(string.Format("complete: {0}", state.Completed));
				}
				else ststext.Add("-");
				foreach (MatchObjective obj in ph.Objectives) {
					deftext.Add(string.Format(">> {0}; ct={1}; key={2}; side={3}; tl={4}", obj.ObjectiveType, obj.CollectableGoal, obj.CustomTypeKey, obj.Side, obj.TimeLimit));
					List<H2HMatchObjectiveState> oblist = PlayerStatus.Current.Objectives.FindAll(
						(H2HMatchObjectiveState s) => s.ObjectiveID == obj.ID);
					if (oblist.Count > 0) {
						H2HMatchObjectiveState state = oblist[0];
						ststext.Add(string.Format("done={0}; ct={1}; frm={2}", state.Completed, state.CountCollectables(), state.FinalRoom));
					}
					else ststext.Add("-");
				}
			}

			Vector2 defSize = Vector2.Zero;
			deftext.ForEach((string s) => {
				Vector2 size = ActiveFont.Measure(s);
				if (defSize.X < size.X) defSize.X = size.X;
				defSize.Y += size.Y;
			});
			Vector2 anchor = new Vector2(1920 - (defSize.X * scale), 1080 - (defSize.Y  * scale));
			float yPos = anchor.Y;
			for (int i = 0; i < deftext.Count; i++) {
				ActiveFont.DrawOutline(deftext[i], new Vector2(anchor.X, yPos), Vector2.Zero, Vector2.One * scale, Color.White, 1f, Color.Black);
				ActiveFont.DrawOutline(ststext[i], new Vector2(anchor.X - 15, yPos), Vector2.UnitX, Vector2.One * scale, Color.Cyan, 1f, Color.Black);
				yPos += ActiveFont.Measure(deftext[i]).Y * scale;
			}
		}

		#endregion

		#region Lobby Race

		private bool ShouldShowLobbyRaceInfo(Scene scene, MatchDefinition def) {
			PlayerStatus st = PlayerStatus.Current;
			if (!st.CurrentArea.Equals(GlobalAreaKey.Head2HeadLobby)) return false;
			if (PlayerStatus.Current.RecordLobbyTime > 0) return true;
			foreach (PlayerStatus stat in Head2HeadModule.knownPlayers.Values) {
				if (stat.RecordLobbyTime > 0) return true;
			}
			return false;
		}

		private void RenderLobbyRaceInfo(Scene scene, MatchDefinition def) {
			List<Tuple<string, long, Vector2>> rows = new List<Tuple<string, long, Vector2>>();
			float hudScale = Head2HeadModule.Settings.HudScale * 0.5f;

			string row = "";
			Vector2 sum = Vector2.Zero;
			if (PlayerStatus.Current.RecordLobbyTime > 0) {
				row = Util.MyDisplayName() + " - " + Dialog.FileTime(PlayerStatus.Current.RecordLobbyTime);
				sum = ActiveFont.Measure(row);
				rows.Add(new Tuple<string, long, Vector2>(row, PlayerStatus.Current.RecordLobbyTime, sum));
			}
			foreach (KeyValuePair<PlayerID, PlayerStatus> kvp in Head2HeadModule.knownPlayers) {
				if (kvp.Value.RecordLobbyTime > 0) {
					bool isLast = false;
					if (rows.Count >= 10) {
						row = string.Format(Dialog.Get("Head2Head_CountMore"), Head2HeadModule.knownPlayers.Count - rows.Count);
						isLast = true;
					}
					else {
						row = kvp.Key.Name + " - " + Dialog.FileTime(kvp.Value.RecordLobbyTime);
					}
					Vector2 size = ActiveFont.Measure(row);
					sum.X = Calc.Max(sum.X, size.X);
					sum.Y += size.Y;
					rows.Add(new Tuple<string, long, Vector2>(row, kvp.Value.RecordLobbyTime, size));
					if (isLast) {
						break;
					}
				}
			}

			string title = Dialog.Clean("Head2Head_LobbyRecords");
			Vector2 titleSize = ActiveFont.Measure(title) * hudScale + Vector2.One * listMargin / 2f;

			Vector2 listSize = sum * hudScale + Vector2.One * listMargin;
			MTexture bgTex = GetSubtexLowerLeft(ListBG, listSize, hudScale);
			Vector2 bGScale = new Vector2(listSize.X / bgTex.Width, listSize.Y / bgTex.Height);
			bGScale.Y *= -1;
			Vector2 position = new Vector2(1920, 1080 - titleSize.Y);
			bgTex.DrawJustified(position, Vector2.UnitX, Color.White, bGScale);

			if (titleSize.X < listSize.X + 8 * hudScale) {
				titleSize.X = listSize.X + 8 * hudScale;
			}
			MTexture titleTex = GetSubtexLowerLeft(ListTitleBG, titleSize, hudScale);
			Vector2 titleScale = new Vector2(titleSize.X / titleTex.Width, titleSize.Y / titleTex.Height);
			titleScale.Y *= -1;
			Vector2 titlePosition = new Vector2(1920, 1080);
			titleTex.DrawJustified(titlePosition, Vector2.UnitX, Color.White, titleScale);
			ActiveFont.Draw(title, titlePosition - Vector2.One * listMargin / 4f, Vector2.One, Vector2.One * hudScale, Color.Black);

			rows.Sort((Tuple<string, long, Vector2> left, Tuple<string, long, Vector2> right) => {
				return right.Item2.CompareTo(left.Item2);
			});
			Vector2 pos = new Vector2(1920 - listMargin / 2f, 1080 - titleSize.Y - listMargin / 2f);
			for (int i = 0; i < rows.Count; i++) {
				ActiveFont.Draw(rows[i].Item1, pos, Vector2.One, Vector2.One * hudScale, Color.Black);
				pos.Y -= rows[i].Item3.Y * hudScale;
			}
		}

		#endregion
	}
}
