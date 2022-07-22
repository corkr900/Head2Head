using Celeste.Mod.Head2Head.IO;
using Celeste.Mod.CelesteNet;
using Celeste.Mod.CelesteNet.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.Data {
	public class DataH2HTest : DataH2HBase<DataH2HTest> {
        static DataH2HTest() {
            DataID = "Head2HeadTest_" + Head2HeadModule.ProtocolVersion;
        }

		public string Message;

        protected override void Read(CelesteNetBinaryReader reader) {
            base.Read(reader);
            Message = reader.ReadString();
        }

        protected override void Write(CelesteNetBinaryWriter writer) {
            base.Write(writer);
            writer.Write(Message);
        }
    }
}
