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
		public OuiRunSelectILChapterIcon(int area, MTexture front, MTexture back) : base(area, front, back) { }

		public Vector2 IdlePositionOverride {
			get {
				float num = 960f + (float)(Area - ILSelector.LastArea.Local_Safe.ID) * 132f;
				if (Area < ILSelector.LastArea.Local_Safe.ID) {
					num -= 80f;
				}
				else if (Area > ILSelector.LastArea.Local_Safe.ID) {
					num += 80f;
				}
				float y = 130f;
				if (Area == ILSelector.LastArea.Local_Safe.ID) {
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
				sizeEase = Calc.Approach(sizeEase, (ILSelector.LastArea.Local_Safe.ID == Area) ? 1f : 0f, Engine.DeltaTime * 4f);
				if (SaveData.Instance.LastArea_Safe.ID == Area) {
					base.Depth = +50;
				}
				else {
					base.Depth = +45;
				}
				if (ILSelector.LastArea.Local_Safe.ID == Area) {
					base.Depth = -50;
				}
				else {
					base.Depth = -45;
				}
			}
		}
	}
}
