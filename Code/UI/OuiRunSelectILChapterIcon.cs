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
		public int SpecialID { get; private set; }

		public OuiRunSelectILChapterIcon(int area, MTexture front, MTexture back, int specialID = -1)
			: base(area, front, back)
		{
			SpecialID = specialID;
		}

		public Vector2 IdlePositionOverride {
			get {
				RunOptionsILChapter option = OuiRunSelectIL.GetChapterOption(ILSelector.LastLevelSetIndex, ILSelector.LastChapterIndex);
				int lastID = option.IsSpecial ? option.SpecialID : option.Data.ID;
				int nextID = option.IsSpecial ? SpecialID : Area;
				float num = 960f + (nextID - lastID) * 132f;
				if (nextID < lastID) {
					num -= 80f;
				}
				else if (nextID > lastID) {
					num += 80f;
				}
				float y = 130f;
				if (nextID == lastID) {
					y = 140f;
				}
				return new Vector2(num, y);
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
				RunOptionsILChapter option = OuiRunSelectIL.GetChapterOption(ILSelector.LastLevelSetIndex, ILSelector.LastChapterIndex);
				if (option == null) return;
				sizeEase = Calc.Approach(sizeEase, (option.Data?.ID == Area) ? 1f : 0f, Engine.DeltaTime * 4f);
				if (SaveData.Instance.LastArea_Safe.ID == Area) {
					Depth = +50;
				}
				else {
					Depth = +45;
				}
				if (option.Data?.ID == Area) {
					Depth = -50;
				}
				else {
					Depth = -45;
				}
			}
		}
	}
}
