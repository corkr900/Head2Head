﻿using Celeste.Mod.CelesteNet;
using Celeste.Mod.Head2Head.IO;
using Celeste.Mod.Head2Head.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.Data {
	public class DataH2HMatchLog : DataH2HBase<DataH2HMatchLog> {
		static DataH2HMatchLog() {
			DataID = "H2HMatchLog_" + Head2HeadModule.ProtocolVersion;
		}

		public bool IsRequest => Log == null;

		public string MatchID;
		public PlayerID LogPlayer;
		public PlayerID RequestingPlayer;
		public bool IsControlPanelRequest;
		public string Client;
		public MatchLog Log;

		protected override void Read(MemoryStream reader) {
			base.Read(reader);
			MatchID = reader.ReadString();
			LogPlayer = reader.ReadPlayerID();
			RequestingPlayer = reader.ReadPlayerID();
			IsControlPanelRequest = reader.ReadBoolean();
			Client = reader.ReadString();
			bool isRequest = reader.ReadBoolean();
			if (!isRequest) {
				Log = reader.ReadMatchLog();
			}
		}

		protected override void Write(MemoryStream writer) {
			base.Write(writer);
			writer.Write(MatchID);
			writer.Write(LogPlayer);
			writer.Write(RequestingPlayer);
			writer.Write(IsControlPanelRequest);
			writer.Write(Client);
			writer.Write(IsRequest);
			if (!IsRequest) {
				writer.Write(Log);
			}
		}
	}
}
