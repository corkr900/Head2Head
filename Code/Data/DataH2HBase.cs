using Celeste.Mod.Head2Head.IO;
using Celeste.Mod.Head2Head.Shared;
using Celeste.Mod.CelesteNet;
using Celeste.Mod.CelesteNet.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.Data {
	abstract public class DataH2HBase<T> : DataType<T> where T: DataH2HBase<T> {

        public DataPlayerInfo player;
        public PlayerID playerID;

        protected virtual bool UseBoundRef { get { return false; } }

        public DataH2HBase() {
            playerID = PlayerID.MyID ?? PlayerID.Default;
		}

        public override MetaType[] GenerateMeta(DataContext ctx) {
            if (UseBoundRef) {
                return new MetaType[] {
                    new MetaPlayerPrivateState(player),
                    new MetaBoundRef(DataType<DataPlayerInfo>.DataID, player?.ID ?? uint.MaxValue, true)
                };
            }
            else return new MetaType[] { new MetaPlayerPrivateState(player) };
        }

        public override void FixupMeta(DataContext ctx) {
            player = Get<MetaPlayerPrivateState>(ctx);
            if (UseBoundRef) Get<MetaBoundRef>(ctx).ID = player?.ID ?? uint.MaxValue;
        }

        protected override void Read(CelesteNetBinaryReader reader) {
            playerID = reader.ReadPlayerID();
        }

        protected override void Write(CelesteNetBinaryWriter writer) {
            writer.Write(playerID);
        }
	}
}
