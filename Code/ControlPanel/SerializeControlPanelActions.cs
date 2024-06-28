using Celeste.Mod.Head2Head.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.ControlPanel {
	public struct SerializeControlPanelActions {

		public List<string> AvailableActions => GetAvailableActions();
		// TODO "Why Can't I Create A Match?" field


		private List<string> GetAvailableActions() {
			List<string> ret = new();
			MatchDefinition def = PlayerStatus.Current.CurrentMatch;

			if (def?.PlayerCanLeaveFreely(PlayerID.MyIDSafe) == true) {
				ret.Add("UNSTAGE_MATCH");
			}
			if (RoleLogic.IsDebug) {
				ret.Add("DBG_PURGE_DATA");
				ret.Add("DBG_PULL_DATA");

			}
			if (RoleLogic.CanGrantMatchPass()) {
				ret.Add("GIVE_MATCH_PASS");
			}
			bool menuHasDropOutButton = (!def?.PlayerCanLeaveFreely(PlayerID.MyIDSafe)) ?? false;
			bool isNoSavefile = SaveData.Instance?.FileSlot == null;
			if (!menuHasDropOutButton && !isNoSavefile && !PlayerStatus.Current.CurrentArea.Equals(GlobalAreaKey.Head2HeadLobby)) {
				ret.Add("GO_TO_LOBBY");
			}
			return ret;
		}
	}
}
