using Celeste.Mod.CelesteNet;
using Celeste.Mod.Head2Head.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.Data {
    public class DataH2HMatchUpdate : DataH2HBase<DataH2HMatchUpdate> {
        static DataH2HMatchUpdate() {
            DataID = "Head2HeadMatchUpdate_" + Head2HeadModule.ProtocolVersion;
        }

		protected override bool UseBoundRef { get { return true; } }

        public MatchDefinition NewDef;

        protected override void Read(CelesteNetBinaryReader reader) {
            base.Read(reader);
            NewDef = reader.ReadMatch();
        }

        protected override void Write(CelesteNetBinaryWriter writer) {
            base.Write(writer);
            writer.Write(NewDef);
        }
    }
}
