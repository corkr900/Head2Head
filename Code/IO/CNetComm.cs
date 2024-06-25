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
	public class CNetComm : GameComponent {
		public static CNetComm Instance { get; private set; }

		public CelesteNetClientContext CnetContext { get { return CelesteNetClientModule.Instance?.Context; } }
		public CelesteNetClient CnetClient { get { return CelesteNetClientModule.Instance?.Client; } }
		public bool IsConnected { get { return CnetClient?.Con?.IsConnected ?? false; } }
		public uint? CnetID { get { return IsConnected ? (uint?)CnetClient?.PlayerInfo?.ID : null; } }

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
		}

		public override void Update(GameTime gameTime) {
			ConcurrentQueue<Action> queue = updateQueue;
			updateQueue = new ConcurrentQueue<Action>();
			foreach (Action act in queue) act();

			base.Update(gameTime);
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

		internal void SendScanRequest(bool autoRejoin) {
			if (!CanSendMessages) {
				return;
			}
			CnetClient.SendAndHandle(new DataH2HScanRequest() {
				AutoRejoin = autoRejoin,
			});
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
			// TODO match log chunking
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

		public void Handle(CelesteNetConnection con, DataH2HTest data) {
			if (data.player == null) data.player = CnetClient.PlayerInfo;  // It's null when handling our own messages
			updateQueue.Enqueue(() => OnReceiveTest?.Invoke(data));
		}
		public void Handle(CelesteNetConnection con, DataChannelMove data) {
			if (data.Player == null) data.Player = CnetClient.PlayerInfo;  // It's null when handling our own messages
			updateQueue.Enqueue(() => OnReceiveChannelMove?.Invoke(data));
		}
		public void Handle(CelesteNetConnection con, DataH2HPlayerStatus data) {
			if (data.player == null) data.player = CnetClient.PlayerInfo;  // It's null when handling our own messages
			updateQueue.Enqueue(() => OnReceivePlayerStatus?.Invoke(data));
		}
		public void Handle(CelesteNetConnection con, DataH2HMatchReset data) {
			if (data.player == null) data.player = CnetClient.PlayerInfo;  // It's null when handling our own messages
			updateQueue.Enqueue(() => OnReceiveMatchReset?.Invoke(data));
		}
		public void Handle(CelesteNetConnection con, DataH2HMatchUpdate data) {
			if (data.player == null) data.player = CnetClient.PlayerInfo;  // It's null when handling our own messages
			updateQueue.Enqueue(() => OnReceiveMatchUpdate?.Invoke(data));
		}
		public void Handle(CelesteNetConnection con, DataH2HScanRequest data) {
			if (data.player == null) data.player = CnetClient.PlayerInfo;  // It's null when handling our own messages
			updateQueue.Enqueue(() => OnReceiveScanRequest?.Invoke(data));
		}
		public void Handle(CelesteNetConnection con, DataH2HMisc data) {
			if (data.player == null) data.player = CnetClient.PlayerInfo;  // It's null when handling our own messages
			updateQueue.Enqueue(() => OnReceiveMisc?.Invoke(data));
		}
		public void Handle(CelesteNetConnection con, DataH2HMatchLog data) {
			if (data.player == null) data.player = CnetClient.PlayerInfo;  // It's null when handling our own messages
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
