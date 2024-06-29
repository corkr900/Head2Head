using Celeste.Mod.CelesteNet;
using Celeste.Mod.Head2Head.IO;
using Celeste.Mod.Head2Head.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.Data {
	public class DataH2HMatchLog : DataH2HBase<DataH2HMatchLog> {
		static DataH2HMatchLog() {
			DataID = "H2HMatchLog_" + Head2HeadModule.ProtocolVersion;
		}

		public bool IsRequest => Log == null;
		public MatchLog Log;
		public string MatchID;
		public PlayerID LogPlayer;
		public PlayerID RequestingPlayer;
		public bool IsControlPanelRequest;
		public string Client;
		public int ChunkNumber;
		public int ChunksTotal;

		/// <summary>
		/// This field does not get serialized
		/// </summary>
		public int ActionsPerChunk;

		protected override void Read(CelesteNetBinaryReader reader) {
			base.Read(reader);
			MatchID = reader.ReadString();
			LogPlayer = reader.ReadPlayerID();
			RequestingPlayer = reader.ReadPlayerID();
			Client = reader.ReadString();
			ChunkNumber = reader.ReadInt32();
			ChunksTotal = reader.ReadInt32();
			bool isRequest = reader.ReadBoolean();
			if (!isRequest) {
				Log = reader.ReadMatchLog();
			}
		}

		protected override void Write(CelesteNetBinaryWriter writer) {
			base.Write(writer);
			writer.Write(MatchID);
			writer.Write(LogPlayer);
			writer.Write(RequestingPlayer);
			writer.Write(Client);
			writer.Write(ChunkNumber);
			writer.Write(ChunksTotal);
			writer.Write(IsRequest);
			if (!IsRequest) {
				writer.Write(Log, ChunkNumber * ActionsPerChunk, ActionsPerChunk);
			}
		}
	}
}
