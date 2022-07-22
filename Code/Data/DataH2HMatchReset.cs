using Celeste.Mod.CelesteNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.Data {
    public class DataH2HMatchReset : DataH2HBase<DataH2HMatchReset> {
        static DataH2HMatchReset() {
            DataID = "Head2HeadMatchReset_" + Head2HeadModule.ProtocolVersion;
        }

        public string MatchID;

        protected override void Read(CelesteNetBinaryReader reader) {
            base.Read(reader);
            MatchID = reader.ReadString();
        }

        protected override void Write(CelesteNetBinaryWriter writer) {
            base.Write(writer);
            writer.Write(MatchID);
        }
    }
}
