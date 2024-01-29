using Celeste.Mod.Head2Head.Entities;
using Celeste.Mod.Head2Head.Shared;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.UI {
	public class OuiRunSelectILChapterIcon : OuiChapterSelectIcon {
		public RunOptionsILChapter ChapterOption { get; private set; }

		public OuiRunSelectILChapterIcon(int area, MTexture front, MTexture back, RunOptionsILChapter opt)
			: base(area, front, back)
		{
			ChapterOption = opt;
		}

		public Vector2 IdlePositionOverride {
			get {
				int lastID = ChapterOption.IsSpecial ? ChapterOption.InternalID : ChapterOption.Data?.ID ?? -1;
				int nextID = ChapterOption.IsSpecial ? ChapterOption.InternalID : Area;
				float xpos = 960f + (nextID - lastID) * 132f;
				if (nextID < lastID) {
					xpos -= 80f;
				}
				else if (nextID > lastID) {
					xpos += 80f;
				}
				float ypos = 130f;
				if (nextID == lastID) {
					ypos = 140f;
				}
				return new Vector2(xpos, ypos);
			}
		}

		public void OnAfterShow() {
			New = false;
			AssistModeUnlockable = false;
		}

		public override void Update() {
			base.Update();
			// Undo changes made based on SaveData and replace them based on the actual thing we care about
			if (SaveData.Instance != null) {
				sizeEase = Calc.Approach(sizeEase, (ChapterOption.Data?.ID == Area) ? 1f : 0f, Engine.DeltaTime * 4f);
				if (SaveData.Instance.LastArea_Safe.ID == Area) {
					Depth = +50;
				}
				else {
					Depth = +45;
				}
				if (ChapterOption.Data?.ID == Area) {
					Depth = -50;
				}
				else {
					Depth = -45;
				}
			}
		}
	}
}
