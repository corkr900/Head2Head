using Celeste.Mod.CelesteNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.Shared {
    public class MatchResult {
        public Dictionary<PlayerID, MatchResultPlayer> players = new Dictionary<PlayerID, MatchResultPlayer>();
        public MatchResultPlayer this[PlayerID key] {
            get {
                if (!players.ContainsKey(key)) return null;
                return players[key];
            }
            set {
                if (players.ContainsKey(key)) {
                    players[key] = value;
                }
                else {
                    players.Add(key, value);
                }
            }
        }
    }

    public class MatchResultPlayer {
        public PlayerID ID;
        public ResultCategory Result = ResultCategory.NotJoined;
        public long FileTimeStart;
        public long FileTimeEnd;
		public int SaveFile;
        public string FinalRoom;

		public long FileTimeTotal { get { return FileTimeEnd - FileTimeStart; } }
    }

    /// <summary>
    /// Discrete match outcomes (or states prior to end of match).
    /// Underlying value defines priority when merging (high value = higher priority)
    /// </summary>
    public enum ResultCategory {
        NotJoined = 0,
        Joined = 10,
        InMatch = 20,
        Completed = 30,
        DNF = 999,
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
            res.ID = r.ReadPlayerID();
            res.Result = (ResultCategory)Enum.Parse(typeof(ResultCategory), r.ReadString());
            res.FileTimeStart = r.ReadInt64();
            res.FileTimeEnd = r.ReadInt64();
            res.SaveFile = r.ReadInt32();
            res.FinalRoom = r.ReadString();
            return res;
        }

        public static void Write(this CelesteNetBinaryWriter w, MatchResultPlayer m) {
            w.Write(m.ID);
            w.Write(m.Result.ToString());
            w.Write(m.FileTimeStart);
            w.Write(m.FileTimeEnd);
            w.Write(m.SaveFile);
            w.Write(m.FinalRoom);
        }
    }
}
