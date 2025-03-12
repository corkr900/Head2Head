using Celeste.Mod.Head2Head.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.ControlPanel
{
    struct SerializePlayerList {
		public List<SerializePlayerAction> Players {
			get {
				var players = new List<SerializePlayerAction>();
				PlayerID? myID = PlayerID.MyID;
				if (myID != null) AddPlayerData(myID.Value, players);
				foreach (var playerKvp in Head2HeadModule.knownPlayers) {
					AddPlayerData(playerKvp.Key, players);
				}
				return players;
			}
		}

		private static void AddPlayerData(PlayerID id, List<SerializePlayerAction> players) {
			var commands = new List<string>();

			if (RoleLogic.CanGrantMatchPass()) {
				commands.Add("GIVE_MATCH_PASS");
			}
			if (RoleLogic.CanGetEnabledMods()) {
				commands.Add("GET_ENABLED_MODS");
			}

			players.Add(new SerializePlayerAction {
				Id = id,
				Commands = commands
			});
		}
	}
}
