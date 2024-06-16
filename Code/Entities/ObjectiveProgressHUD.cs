using Celeste.Mod.Head2Head.Shared;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.Entities {
	public class ObjectiveProgressHUD : Entity {

		private MTexture bg;
		public float DrawLerp = 0;

		public bool ShowHUD {
			get {
				if (!PlayerStatus.Current.IsInMatch(true)) return false;
				return Head2HeadModule.Settings.ShowObjectives switch {
					Head2HeadModuleSettings.ShowObjectivesHUD.Always
						=> true,
					Head2HeadModuleSettings.ShowObjectivesHUD.OnlyInLobby
						=> PlayerStatus.Current.CurrentArea.IsH2HLobby,
					Head2HeadModuleSettings.ShowObjectivesHUD.InLobbyOrPause
						=> SceneAs<Level>()?.Paused == true || PlayerStatus.Current.CurrentArea.IsH2HLobby,
					_ => false
				};
			}
		}

		public float TargetPosY {
			get {
				float yTargetPos = 96f;  // Base position
				yTargetPos += 78f;  // Berries counter
				if (SceneAs<Level>()?.TimerHidden == false) {
					if (Settings.Instance.SpeedrunClock == SpeedrunType.Chapter) {
						yTargetPos += 58f;
					}
					else if (Settings.Instance.SpeedrunClock == SpeedrunType.File) {
						yTargetPos += 78f;
					}
				}
				return yTargetPos;
			}
		}

		public ObjectiveProgressHUD() {
			Y = 96f;
			Depth = -101;
			Tag = (int)Tags.HUD | (int)Tags.Global | (int)Tags.PauseUpdate | (int)Tags.TransitionUpdate;
			bg = GFX.Gui["Head2Head/HUD/objectiveBG"];
		}

		public override void Update() {
			base.Update();
			if (ShowHUD) {
				DrawLerp = Calc.Approach(DrawLerp, 1f, 2f * Engine.RawDeltaTime);
			}
			else {
				DrawLerp = Calc.Approach(DrawLerp, 0f, 1.2f * Engine.RawDeltaTime);
			}
			if (Visible) {
				Y = Calc.Approach(Y, TargetPosY, Engine.DeltaTime * 800f);
			}
			Visible = DrawLerp > 0f;
		}

		public override void Render() {
			if (!PlayerStatus.Current.IsInMatch(true)) return;

			float bgposY = (float)Math.Round(Y) + 12f;
			foreach (var rule in PlayerStatus.Current.CurrentMatch.Rules) {
				string text = Util.TranslatedRuleLabel(rule);
				RenderOneWidget(null, text, bgposY, false);
				bgposY += 58f;
			}
			foreach (MatchObjective obj in PlayerStatus.Current.CurrentObjectives()) {
				H2HMatchObjectiveState state = PlayerStatus.Current.objectives.FirstOrDefault((H2HMatchObjectiveState s) => s.ObjectiveID == obj.ID );
				string text = obj.CollectableGoal > 0 ?
					$"{obj.Label} {state.CountCollectables()}/{obj.CollectableGoal}" :
					$"{obj.Label}";
				MTexture icon = GetIcon(obj);
				RenderOneWidget(icon, text, bgposY, state.Completed);
				bgposY += 58f;
			}
		}

		private void RenderOneWidget(MTexture icon, string text, float bgposY, bool complete) {
			const float TextScale = 0.5f;
			const float iconSize = 80f;

			float XSize = Calc.Max(32f, ActiveFont.Measure(text).X * TextScale + iconSize + 16f);
			Vector2 bgpos = new Vector2(Calc.LerpClamp(0, XSize, Ease.CubeOut(DrawLerp)), bgposY);
			bg.DrawJustified(bgpos, new Vector2(1f, 0.5f));
			Vector2 pos = new Vector2(Calc.LerpClamp(-XSize, 5f, Ease.CubeOut(DrawLerp)), bgpos.Y);
			if (icon != null) {
				float iconScale = iconSize / icon.Height;
				icon.DrawJustified(pos, Vector2.UnitY * 0.5f, Color.White, iconScale);
			}
			pos.X += iconSize;
			ActiveFont.DrawOutline(text, pos, Vector2.UnitY * 0.5f, Vector2.One * TextScale, Color.White, 2f, Color.Black);
			if (complete) {
				Draw.Line(pos, new Vector2(XSize, pos.Y), Color.DarkRed, 4f);
			}
		}

		private MTexture GetIcon(MatchObjective obj) {
			string path = obj.ObjectiveType switch {
				MatchObjectiveType.ChapterComplete => "Head2Head/Categories/Clear",
				MatchObjectiveType.HeartCollect => "collectables/heartgem/0/spin00",
				MatchObjectiveType.CassetteCollect => "collectables/cassette",
				MatchObjectiveType.Strawberries => "collectables/strawberry",
				//MatchObjectiveType.Keys => "Head2Head/Categories/Custom",
				MatchObjectiveType.MoonBerry => "Head2Head/Categories/MoonBerry",
				MatchObjectiveType.GoldenStrawberry => "Head2Head/Categories/Golden",
				MatchObjectiveType.WingedGoldenStrawberry => "Head2Head/Categories/WingedGolden",
				//MatchObjectiveType.Flag => "Head2Head/Categories/Custom",
				//MatchObjectiveType.EnterRoom => "Head2Head/Categories/Custom",
				MatchObjectiveType.TimeLimit => "Head2Head/Categories/TimeLimit",
				//MatchObjectiveType.CustomCollectable => "Head2Head/Categories/Custom",
				//MatchObjectiveType.CustomObjective => "Head2Head/Categories/Custom",
				//MatchObjectiveType.UnlockChapter => "Head2Head/Categories/Custom",
				MatchObjectiveType.RandomizerClear => "Head2Head/Categories/Clear",
				_ => null,
			};
			return string.IsNullOrEmpty(path) ? null : GFX.Gui.GetOrDefault(path, null);
		}
	}
}
