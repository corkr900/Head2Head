using Celeste.Mod.CelesteNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.Data {
	public class DataH2HScanRequest : DataH2HBase<DataH2HScanRequest> {
        static DataH2HScanRequest() {
            DataID = "DataH2HScanRequest_" + Head2HeadModule.ProtocolVersion;
        }

        protected override bool UseBoundRef { get { return false; } }

        protected override void Read(CelesteNetBinaryReader reader) {
            base.Read(reader);
        }

        protected override void Write(CelesteNetBinaryWriter writer) {
            base.Write(writer);
        }
    }
}
