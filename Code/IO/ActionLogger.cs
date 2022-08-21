using Celeste.Mod.Head2Head.Control;
using Celeste.Mod.Head2Head.Shared;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Celeste.Mod.Head2Head.IO {

	// TODO (!!!) put action logging behind a setting
	// TODO (!!!) system to export a match log
	public class ActionLogger {
		private static MatchLog Current;

		private static bool TrackActions(bool skipCurrentCheck = false) {
			return (skipCurrentCheck || Current != null) && Head2HeadModule.Settings.UseActionLog;
		}

		public static void WriteLog() {
			if (Current == null) return;
			try {
				using (FileStream fs = new FileStream(GetLogPath(), FileMode.Create)) {
					XmlSerializer ser = new XmlSerializer(typeof(MatchLog));
					ser.Serialize(fs, Current);
				}
			}
			catch (Exception e) {
				Engine.Commands.Log("Failed to write action log: " + e.Message);
			}
		}

		public static MatchLog LoadLog(string matchID) {
			string path = GetLogFileName(matchID);
			if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
			try {
				using (FileStream fs = new FileStream(path, FileMode.Open)) {
					XmlSerializer ser = new XmlSerializer(typeof(MatchLog));
					object ob = ser.Deserialize(fs);
					if (ob is MatchLog log) {
						return log;
					}
					else return null;
				}
			}
			catch (Exception e) {
				Engine.Commands.Log("Failed to write action log: " + e.Message);
			}
			return null;
		}

		/// <summary>
		/// Searches for an existing log with the given ID
		/// </summary>
		/// <param name="matchID"></param>
		/// <returns>Returns "" if there is no matching file</returns>
		private static string GetLogFileName(string matchID) {
			DynamicData dd = new DynamicData(typeof(UserIO));
			string dirpath = dd.Get<string>("SavePath");
			foreach (string file in Directory.GetFiles(dirpath, "Head2Head*")) {
				if (string.IsNullOrEmpty(file)) continue;
				string filename = Path.GetFileName(file);
				string[] parts = filename.Split(' ');
				if (parts.Length < 3) continue;
				if (parts[2] == matchID) return file;
			}
			return "";
		}

		/// <summary>
		/// Gets the path to write the current log to
		/// </summary>
		/// <returns>Returns "" if there is no appropriate path</returns>
		private static string GetLogPath() {
			if (Current == null) return "";
			DynamicData dd = new DynamicData(typeof(UserIO));
			string handle = dd.Invoke("GetHandle", string.Format("Head2Head {0} {1}", Current.matchBeginDate, Current.matchID)) as string;
			return handle ?? "";
		}

		public static void PurgeOldLogs() {
			// TODO (!!!) Delete logs older than today or yesterday
		}

		// ######################################

		public static void StartingMatch(MatchDefinition def) {
			WriteLog();
			if (!TrackActions(true)) return;
			Current = new MatchLog() {
				matchBeginDate = def.BeginInstant.ToShortDateString().Replace('/', '-'),
				matchID = def.MatchID,
				matchDispName = def.DisplayName,
				matchCreator = def.Owner.Name,
				dirty = false,
			};
			Current.log.Add(new LoggableAction(LoggableAction.ActionType.MatchStart));
			WriteLog();
		}

		public static void EnteringArea() {
			if (!TrackActions()) return;
			Current.log.Add(new LoggableAction(LoggableAction.ActionType.AreaEnter));
		}

		public static void ExitingArea(LevelExit.Mode _mode) {
			if (!TrackActions()) return;
			Current.log.Add(new LoggableAction(LoggableAction.ActionType.AreaExit) {
				levelExitMode = _mode.ToString(),
			});
		}

		public static void AreaCompleted() {
			if (!TrackActions()) return;
			Current.log.Add(new LoggableAction(LoggableAction.ActionType.AreaComplete));
		}

		public static void EnteringRoom() {
			if (!TrackActions()) return;
			Current.log.Add(new LoggableAction(LoggableAction.ActionType.EnterRoom));
		}

		public static void DebugView() {
			if (!TrackActions()) return;
			Current.log.Add(new LoggableAction(LoggableAction.ActionType.DebugView));
		}

		public static void DebugTeleport() {
			if (!TrackActions()) return;
			Current.log.Add(new LoggableAction(LoggableAction.ActionType.DebugTeleport));
		}

		public static void ClosingApplication() {
			if (!TrackActions()) return;
			Current.log.Add(new LoggableAction(LoggableAction.ActionType.IntentionalCloseApplication));
			WriteLog();
		}

		// TODO (!!!) event for completed match

		// TODO (!!!) event for opening savefile

		// TODO (!!!) event for rejoining match
	}

	[Serializable]
	public class MatchLog {
		public string matchBeginDate;
		public string matchID;
		public string matchDispName;
		public string matchCreator;
		public List<LoggableAction> log = new List<LoggableAction>();

		public bool dirty;
	}

	[Serializable]
	public class LoggableAction {
		public enum ActionType : int {
			MatchStart = 1,
			MatchComplete = 2,
			AreaEnter = 3,
			AreaComplete = 4,
			AreaExit = 5,
			EnterRoom = 6,
			DebugView = 7,
			DebugTeleport = 8,

			IntentionalCloseApplication = 99,

			ErrorType = 999,
		}

		public LoggableAction()
			: this(ActionType.ErrorType)
		{

		}

		public LoggableAction(ActionType _type) {
			type = _type;
			instant = DateTime.Now.ToString();
			fileTimer = SaveData.Instance.Time;
			areaSID = PlayerStatus.Current.CurrentArea.SID;
			room = PlayerStatus.Current.CurrentRoom;
			matchID = PlayerStatus.Current.CurrentMatchID;
			saveDataIndex = SaveData.Instance?.FileSlot ?? -99;
			levelExitMode = "";
		}

		public ActionType type;
		public string instant;
		public long fileTimer;
		public string areaSID;
		public string room;
		public string matchID;
		public int saveDataIndex;

		public string levelExitMode;
	}
}
