using Celeste.Mod.Head2Head.Shared;
using Celeste.Mod.CelesteNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.Data {
	public class DataH2HPlayerStatus : DataH2HBase<DataH2HPlayerStatus> {
		static DataH2HPlayerStatus() {
			DataID = "Head2HeadPlayerStatus_" + Head2HeadModule.ProtocolVersion;
		}

		protected override bool UseBoundRef { get { return true; } }

		public PlayerStatus Status;

		protected override void Read(CelesteNetBinaryReader reader) {
			base.Read(reader);
			Status = reader.ReadPlayerState();
		}

		protected override void Write(CelesteNetBinaryWriter writer) {
			base.Write(writer);
			writer.Write(Status);
		}
	}
}
