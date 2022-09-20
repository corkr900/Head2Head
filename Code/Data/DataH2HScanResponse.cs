using Celeste.Mod.CelesteNet;
using Celeste.Mod.Head2Head.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.Data {
	public class DataH2HScanResponse : DataH2HBase<DataH2HScanResponse> {
        static DataH2HScanResponse() {
            DataID = "DataH2HScanResponse_" + Head2HeadModule.ProtocolVersion;
        }

        protected override bool UseBoundRef { get { return false; } }

        public PlayerID Requestor;
        public PlayerStatus SenderStatus;
        public PlayerStatus RequestorStatus;
        public MatchDefinition MatchDef;

        protected override void Read(CelesteNetBinaryReader reader) {
            base.Read(reader);
            Requestor = reader.ReadPlayerID();
            SenderStatus = reader.ReadPlayerState();
            RequestorStatus = reader.ReadPlayerState();
            MatchDef = reader.ReadMatch();
        }

        protected override void Write(CelesteNetBinaryWriter writer) {
            base.Write(writer);
            writer.Write(Requestor);
            writer.Write(SenderStatus);
            writer.Write(RequestorStatus);
            writer.Write(MatchDef);
        }
    }
}
