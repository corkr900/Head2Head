using Celeste.Mod.CelesteNet;
using Celeste.Mod.Head2Head.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.Data {
    public class DataH2HMisc : DataH2HBase<DataH2HMisc> {
        static DataH2HMisc() {
            DataID = "Head2HeadMisc_" + Head2HeadModule.ProtocolVersion;
        }

        public string message;
        public PlayerID targetPlayer;
        public string details;
        public string requestorCPToken;
        public PlayerID? respondingPlayerID;

        protected override void Read(CelesteNetBinaryReader reader) {
            base.Read(reader);
            message = reader.ReadString();
            targetPlayer = reader.ReadPlayerID();
			details = reader.ReadString();
			requestorCPToken = reader.ReadString();
            respondingPlayerID = reader.ReadBoolean() ? reader.ReadPlayerID() : null;
		}

        protected override void Write(CelesteNetBinaryWriter writer) {
            base.Write(writer);
            writer.Write(message ?? "");
            writer.Write(targetPlayer);
            writer.Write(details ?? "");
			writer.Write(requestorCPToken ?? "");
            if (respondingPlayerID == null) {
                writer.Write(false);
            }
            else {
                writer.Write(true);
                writer.Write(respondingPlayerID.Value);
            }
		}
    }
}
