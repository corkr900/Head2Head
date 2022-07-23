using Celeste.Mod.CelesteNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.Shared {
    public class MatchResult {
        public Dictionary<PlayerID, MatchResultPlayer> players = new Dictionary<PlayerID, MatchResultPlayer>();
        public ResultCategory this[PlayerID key] {
            get {
                if (!players.ContainsKey(key)) return ResultCategory.NotJoined;
                return players[key].Result;
            }
            set {
                if (players.ContainsKey(key)) {
                    players[key].Result = value;
                }
                else {
                    players.Add(key, new MatchResultPlayer() {
                        Result = value,
                    });
                }
            }
        }
    }

    public class MatchResultPlayer {
        public ResultCategory Result = ResultCategory.NotJoined;
    }

    public enum ResultCategory {
        NotJoined,
        Joined,
        InMatch,
        Completed,
        DNF,
    }

    public static class MatchResultExtensions {
        public static MatchResult ReadMatchResult(this CelesteNetBinaryReader r) {
            MatchResult result = new MatchResult();
            int numPlayers = r.ReadInt32();
            for (int i = 0; i < numPlayers; i++) {
                PlayerID id = r.ReadPlayerID();
                MatchResultPlayer res = r.ReadMatchResultPlayer();
                result.players.Add(id, res);
			}
            return result;
        }

        public static void Write(this CelesteNetBinaryWriter w, MatchResult m) {
            w.Write(m.players.Count);
            foreach (var kvp in m.players) {
                w.Write(kvp.Key);
                w.Write(kvp.Value);
			}
        }

        public static MatchResultPlayer ReadMatchResultPlayer(this CelesteNetBinaryReader r) {
            MatchResultPlayer res = new MatchResultPlayer();
            res.Result = (ResultCategory)Enum.Parse(typeof(ResultCategory), r.ReadString());
            return res;
        }

        public static void Write(this CelesteNetBinaryWriter w, MatchResultPlayer m) {
            w.Write(m.Result.ToString());
        }
    }
}
