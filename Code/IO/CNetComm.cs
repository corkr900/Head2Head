using Celeste.Mod.Head2Head.Data;
using Celeste.Mod.Head2Head.Shared;
using Celeste.Mod.CelesteNet;
using Celeste.Mod.CelesteNet.Client;
using Celeste.Mod.CelesteNet.DataTypes;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Celeste.Mod.CelesteNet.Client.Components;

namespace Celeste.Mod.Head2Head.IO {
	public class H2HGameComponent : CelesteNetGameComponent {
		public H2HGameComponent(CelesteNetClientContext context, Game game)
			: base(context, game) {

		}

		private ulong tickCounter = 0;

		public override void Tick() {
			CNetComm.Instance?.Tick(++tickCounter);
		}
	}

	public class CNetComm : GameComponent {
		public static CNetComm Instance { get; private set; }

		public CelesteNetClientContext CnetContext { get { return CelesteNetClientModule.Instance?.Context; } }
		public CelesteNetClient CnetClient { get { return CelesteNetClientModule.Instance?.Client; } }
		public bool IsConnected { get { return CnetClient?.Con?.IsConnected ?? false; } }
		public uint? CnetID { get { return IsConnected ? (uint?)CnetClient?.PlayerInfo?.ID : null; } }
		public long MaxPacketSize 
			=> CnetClient?.Con is CelesteNetTCPUDPConnection connection
			? (connection.ConnectionSettings?.MaxPacketSize ?? 2048)
			: 2048;
		/// <summary>
		/// IDK exactly how much overhead i need to leave, but 256 bytes should be plenty for whatever headers cnet adds.
		/// The H2H format's header is at most 25 bytes plus the length of the sender's name
		/// </summary>
		public long MaxPacketChunkSize => MaxPacketSize - 25L - (PlayerID.LastKnownName?.Length ?? 100) - 256L;

		public DataChannelList.Channel CurrentChannel {
			get {
				KeyValuePair<Type, CelesteNetGameComponent> listComp = CnetContext.Components.FirstOrDefault((KeyValuePair<Type, CelesteNetGameComponent> kvp) => {
					return kvp.Key == typeof(CelesteNetPlayerListComponent);
				});
				if (listComp.Equals(default(KeyValuePair<Type, CelesteNetGameComponent>))) return null;
				CelesteNetPlayerListComponent comp = listComp.Value as CelesteNetPlayerListComponent;
				DataChannelList.Channel[] list = comp.Channels?.List;
				return list?.FirstOrDefault(c => c.Players.Contains(CnetClient.PlayerInfo.ID));
			}
		}
		public bool CurrentChannelIsMain {
			get {
				return CurrentChannel?.Name?.ToLower() == "main";
			}
		}

		public bool CanSendMessages {
			get {
				return IsConnected && !CurrentChannelIsMain;
			}
		}

		internal static long MessageCounter = 0;

		// #############################################

		public delegate void OnConnectedHandler(CelesteNetClientContext cxt);
		public static event OnConnectedHandler OnConnected;

		public delegate void OnDisonnectedHandler(CelesteNetConnection con);
		public static event OnDisonnectedHandler OnDisconnected;

		public delegate void OnReceiveTestHandler(DataH2HTest data);
		public static event OnReceiveTestHandler OnReceiveTest;

		public delegate void OnReceiveChannelMoveHandler(DataChannelMove data);
		public static event OnReceiveChannelMoveHandler OnReceiveChannelMove;

		public delegate void OnReceivePlayerStatusHandler(DataH2HPlayerStatus data);
		public static event OnReceivePlayerStatusHandler OnReceivePlayerStatus;

		public delegate void OnReceiveMatchResetHandler(DataH2HMatchReset data);
		public static event OnReceiveMatchResetHandler OnReceiveMatchReset;

		public delegate void OnReceiveMatchUpdateHandler(DataH2HMatchUpdate data);
		public static event OnReceiveMatchUpdateHandler OnReceiveMatchUpdate;

		public delegate void OnReceiveScanRequestHandler(DataH2HScanRequest data);
		public static event OnReceiveScanRequestHandler OnReceiveScanRequest;

		public delegate void OnReceiveMiscHandler(DataH2HMisc data);
		public static event OnReceiveMiscHandler OnReceiveMisc;

		public delegate void OnReceiveMatchLogHandler(DataH2HMatchLog data);
		public static event OnReceiveMatchLogHandler OnReceiveMatchLog;

		// #############################################

		private ConcurrentQueue<Action> updateQueue = new ConcurrentQueue<Action>();

		private ConcurrentQueue<DataType> outgoingExtraPacketChunks = new();
		private List<DataType> incomingExtraPacketChunks = new();
		private static object incomingChunksLock = new();

		public CNetComm(Game game)
			: base (game)
		{
			Instance = this;
			Disposed += OnComponentDisposed;
			CelesteNetClientContext.OnStart += OnCNetClientContextStart;
			CelesteNetClientContext.OnDispose += OnCNetClientContextDispose;
			OnReceiveTest += OnTestMessage;
		}

		private void OnComponentDisposed(object sender, EventArgs args) {
			CelesteNetClientContext.OnStart -= OnCNetClientContextStart;
			CelesteNetClientContext.OnDispose -= OnCNetClientContextDispose;

			OnReceiveTest -= OnTestMessage;
		}

		private void OnCNetClientContextStart(CelesteNetClientContext cxt) {
			CnetClient.Data.RegisterHandlersIn(this);
			CnetClient.Con.OnDisconnect += OnDisconnect;
			updateQueue.Enqueue(() => OnConnected?.Invoke(cxt));
		}

		private void OnCNetClientContextDispose(CelesteNetClientContext cxt) {
			// CnetClient is null here
			;
		}

		private void OnDisconnect(CelesteNetConnection con) {
			updateQueue.Enqueue(() => OnDisconnected?.Invoke(con));
			outgoingExtraPacketChunks.Clear();
			incomingExtraPacketChunks.Clear();
		}

		public override void Update(GameTime gameTime) {
			ConcurrentQueue<Action> queue = updateQueue;
			updateQueue = new ConcurrentQueue<Action>();
			foreach (Action act in queue) act();

			base.Update(gameTime);
		}

		internal void Tick(ulong v) {
			if (outgoingExtraPacketChunks.TryDequeue(out DataType chunk)) {
				// Not calling SendAndHandle because the first one already has all the data when sending to ourself
				CnetClient.Send(chunk);
			}
		}

		// #############################################

		public void SendTestMessage() {
			if (!CanSendMessages) {
				Engine.Commands.Log("Cannot send test message: not connected to CelesteNet.");
				return;
			}
			CnetClient.SendAndHandle(new DataH2HTest() {
				Message = "This is a test message you absolute dingus."
			});
		}

		internal void SendPlayerStatus(PlayerStatus stat) {
			if (!CanSendMessages) {
				return;
			}
			CnetClient.SendAndHandle(new DataH2HPlayerStatus() {
				Status = stat,
			});
			MessageCounter++;
		}

		internal void SendMatchReset(string matchID) {
			if (!CanSendMessages) {
				return;
			}
			CnetClient.SendAndHandle(new DataH2HMatchReset() {
				MatchID = matchID,
			});
			MessageCounter++;
		}

		internal void SendMatchUpdate(MatchDefinition def) {
			if (!CanSendMessages) {
				return;
			}
			CnetClient.SendAndHandle(new DataH2HMatchUpdate() {
				NewDef = def,
			});
			MessageCounter++;
		}

		internal void SendScanRequest() {
			if (!CanSendMessages) {
				return;
			}
			CnetClient.SendAndHandle(new DataH2HScanRequest());
			MessageCounter++;
		}

		internal void SendMisc(string message, PlayerID targetPlayer) {
			if (!CanSendMessages) {
				return;
			}
			CnetClient.SendAndHandle(new DataH2HMisc() {
				message = message,
				targetPlayer = targetPlayer,
			});
			MessageCounter++;
		}

		internal void SendMatchLogRequest(PlayerID player, string matchID, bool isControlPanelRequest, string client) {
			if (!CanSendMessages) {
				return;
			}
			CnetClient.SendAndHandle(new DataH2HMatchLog() {
				Log = null,
				MatchID = matchID,
				LogPlayer = player,
				RequestingPlayer = PlayerID.MyIDSafe,
				IsControlPanelRequest = isControlPanelRequest,
				Client = client,
			});
			MessageCounter++;
		}

		internal void SendMatchLog(MatchLog log, PlayerID player, string matchID, PlayerID requestor, bool isControlPanelRequest, string client) {
			if (!CanSendMessages || log == null) {
				return;
			}
			CnetClient.SendAndHandle(new DataH2HMatchLog() {
				Log = log,
				MatchID = matchID,
				LogPlayer = player,
				RequestingPlayer = requestor,
				IsControlPanelRequest = isControlPanelRequest,
				Client = client,
			});
			MessageCounter++;
		}

		// #############################################

		internal void EnqueueSubsequentChunk<T>(DataH2HBase<T> data) where T : DataH2HBase<T>, new() {
			outgoingExtraPacketChunks.Enqueue(data);
		}

		// #############################################

		private T PreHandle<T>(T data) where T : DataH2HBase<T>, new() {
			if (data.player == null) data.player = CnetClient.PlayerInfo;  // It's null when handling our own messages
			if (data.chunksInPacket <= 1) {  // Packet does not have subsequent chunks and can be processed immediately
				return data;
			}
			
			// record the incoming chunk and then check if we have all of them
			lock (incomingChunksLock) {
				incomingExtraPacketChunks.Add(data);
				T[] arr = new T[data.chunksInPacket];
				Type t = data.GetType();
				// Sort the chunks for this packet into an array
				foreach (T chunk in incomingExtraPacketChunks) {
					if (chunk.playerID.Equals(data.playerID) && chunk.packetID == data.packetID) {
						if (!chunk.GetType().Equals(t)) {
							Logger.Log(LogLevel.Error, "Head2Head", $"Received packets of different types from the same sender with the same ID.");
							throw new InvalidOperationException($"Received packets of different types from the same sender with the same ID.");
						}
						arr[chunk.chunkNumber] = chunk;
					}
				}
				// Check whether we've received all the chunks
				for (int i = 0; i < arr.Length; i++) {
					if (arr[i] == null) return null;  // Don't have all chunks yet
				}
				// Compose the chunks into the same object and return it to be sent to the correct event
				for (int i = 0; i < arr.Length; i++) {
					incomingExtraPacketChunks.Remove(arr[i]);
				}
				arr[0].Compose(arr);
				return arr[0];
			}
		}

		public void Handle(CelesteNetConnection con, DataH2HTest data) {
			DataH2HTest packet = PreHandle(data);
			if (packet == null) return;  // Waiting on more chunks
			updateQueue.Enqueue(() => OnReceiveTest?.Invoke(data));
		}
		public void Handle(CelesteNetConnection con, DataChannelMove data) {
			if (data.Player == null) data.Player = CnetClient.PlayerInfo;  // It's null when handling our own messages
			updateQueue.Enqueue(() => OnReceiveChannelMove?.Invoke(data));
		}
		public void Handle(CelesteNetConnection con, DataH2HPlayerStatus data) {
			DataH2HPlayerStatus packet = PreHandle(data);
			if (packet == null) return;  // Waiting on more chunks
			updateQueue.Enqueue(() => OnReceivePlayerStatus?.Invoke(data));
		}
		public void Handle(CelesteNetConnection con, DataH2HMatchReset data) {
			DataH2HMatchReset packet = PreHandle(data);
			if (packet == null) return;  // Waiting on more chunks
			updateQueue.Enqueue(() => OnReceiveMatchReset?.Invoke(data));
		}
		public void Handle(CelesteNetConnection con, DataH2HMatchUpdate data) {
			DataH2HMatchUpdate packet = PreHandle(data);
			if (packet == null) return;  // Waiting on more chunks
			updateQueue.Enqueue(() => OnReceiveMatchUpdate?.Invoke(data));
		}
		public void Handle(CelesteNetConnection con, DataH2HScanRequest data) {
			DataH2HScanRequest packet = PreHandle(data);
			if (packet == null) return;  // Waiting on more chunks
			updateQueue.Enqueue(() => OnReceiveScanRequest?.Invoke(data));
		}
		public void Handle(CelesteNetConnection con, DataH2HMisc data) {
			DataH2HMisc packet = PreHandle(data);
			if (packet == null) return;  // Waiting on more chunks
			updateQueue.Enqueue(() => OnReceiveMisc?.Invoke(data));
		}
		public void Handle(CelesteNetConnection con, DataH2HMatchLog data) {
			DataH2HMatchLog packet = PreHandle(data);
			if (packet == null) return;  // Waiting on more chunks
			if (data.IsRequest && !data.LogPlayer.Equals(PlayerID.MyID)) return;
			if (!data.IsRequest && !data.RequestingPlayer.Equals(PlayerID.MyID)) return;
			updateQueue.Enqueue(() => OnReceiveMatchLog?.Invoke(data));
		}


		// #############################################

		private void OnTestMessage(DataH2HTest data) {
			Engine.Commands.Log("Received test message: " + data.Message);
			Engine.Commands.Log("ID: " + data.player.ID);
			Engine.Commands.Log("Name: " + data.player.Name);
			Engine.Commands.Log("Full Name: " + data.player.FullName);
			Engine.Commands.Log("Display Name: " + data.player.DisplayName);
			Logger.Log(LogLevel.Verbose, "Head2Head", "Received test message: " + data.Message);
			Logger.Log(LogLevel.Verbose, "Head2Head", "ID: " + data.player.ID);
			Logger.Log(LogLevel.Verbose, "Head2Head", "Name: " + data.player.Name);
			Logger.Log(LogLevel.Verbose, "Head2Head", "Full Name: " + data.player.FullName);
			Logger.Log(LogLevel.Verbose, "Head2Head", "Display Name: " + data.player.DisplayName);
		}

	}
}
