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
	public class ActionLogger {
		private static MatchLog Current;

		private static bool TrackActions(bool skipCurrentCheck = false) {
			return (skipCurrentCheck || (Current != null && !Current.completedMatchWritten)) && Role.LogMatchActions();
		}

		public static bool WriteLog() {
			if (Current == null) return false;
			return Current.Write();
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
				Logger.Log(LogLevel.Error, "Head2Head", "Failed to load action log: " + e.Message);
			}
			return null;
		}

		/// <summary>
		/// Searches for an existing log with the given ID
		/// </summary>
		/// <param name="matchID"></param>
		/// <returns>Returns "" if there is no matching file</returns>
		private static string GetLogFileName(string matchID) {
			// Use DynamicData to access SavePath because the implementation of it is different between FNA/XNA
			DynamicData dd = new DynamicData(typeof(UserIO));
			string dirpath = dd.Get<string>("SavePath");
			foreach (string file in Directory.GetFiles(dirpath, "Head2Head*")) {
				if (string.IsNullOrEmpty(file)) continue;
				string filename = Path.GetFileNameWithoutExtension(file);
				string[] parts = filename.Split(' ');
				if (parts.Length < 3) continue;
				if (parts[2] == matchID) return file;
			}
			return "";
		}

		public static bool LogFileExists(string matchID) {
			return File.Exists(GetLogFileName(matchID));
		}

		public static void PurgeOldLogs() {
			// Delete logs older than today or yesterday
			// Use DynamicData to access SavePath because the implementation of it is different between FNA/XNA
			DynamicData dd = new DynamicData(typeof(UserIO));
			string dirpath = dd.Get<string>("SavePath");
			DateTime basedate = DateTime.Today;
			foreach (string file in Directory.GetFiles(dirpath, "Head2Head*")) {
				if (string.IsNullOrEmpty(file)) continue;
				string filename = Path.GetFileName(file);
				string[] parts = filename.Split(' ');
				if (parts.Length < 2) continue;
				DateTime date;
				if (DateTime.TryParse(parts[1], out date)){
					TimeSpan span = basedate - date;
					if (span.TotalHours > 40) {
						try {
							File.Delete(file);
						}
						catch(Exception e) {
							Logger.Log(LogLevel.Error, "Head2Head", "Failed to delete old log: " + e.Message);
						}
					}
				}
			}
		}

		public static void Export(string matchID) {
			try {
				string source = GetLogFileName(matchID);
				if (string.IsNullOrEmpty(source)) {
					Logger.Log(LogLevel.Error, "Head2Head", "Export failed: could not get source file path for match ID " + matchID);
					return;
				}
				if (!File.Exists(source)) {
					Logger.Log(LogLevel.Error, "Head2Head", "Export failed: source does not exist: " + source);
					return;
				}
				string dir = Head2HeadModule.Settings.RealExportLocation;
				if (!Directory.Exists(dir)) {
					Logger.Log(LogLevel.Error, "Head2Head", "Export failed: target directory does not exist: " + dir);
					return;
				}
				string target = Path.Combine(dir, Path.GetFileName(source));
				File.Copy(source, target, true);
			}
			catch(Exception e) {
				Logger.Log(LogLevel.Error, "Head2Head", "Export failed: " + e.Message);
			}
		}

		// ######################################

		public static void StartingMatch(MatchDefinition def) {
			if (!TrackActions(true)) return;
			if (Current == null || Current.matchID != def.MatchID) {
				WriteLog();
				Current = new MatchLog() {
					matchBeginDate = def.BeginInstant.ToShortDateString().Replace('/', '-'),
					matchID = def.MatchID,
					matchDispName = def.CategoryDisplayName,
					matchCreator = def.Owner.Name,
				};
			}
			Current.Log(new LoggableAction(LoggableAction.ActionType.MatchStart));
			WriteLog();
		}

		public static void EnteringArea() {
			if (!TrackActions()) return;
			Current.Log(new LoggableAction(LoggableAction.ActionType.AreaEnter));
			Current.Write();
		}

		public static void ExitingArea(LevelExit.Mode _mode) {
			if (!TrackActions()) return;
			Current.Log(new LoggableAction(LoggableAction.ActionType.AreaExit) {
				levelExitMode = _mode.ToString(),
			});
		}

		public static void AreaCompleted() {
			if (!TrackActions()) return;
			Current.Log(new LoggableAction(LoggableAction.ActionType.AreaComplete));
		}

		public static void EnteringRoom() {
			if (!TrackActions()) return;
			LoggableAction la = new LoggableAction(LoggableAction.ActionType.EnterRoom);
			Current.Log(la);
			if (!string.IsNullOrEmpty(la.checkpoint)) {
				WriteLog();
			}
		}

		public static void DebugView() {
			if (!TrackActions()) return;
			Current.Log(new LoggableAction(LoggableAction.ActionType.DebugView));
		}

		public static void DebugTeleport() {
			if (!TrackActions()) return;
			Current.Log(new LoggableAction(LoggableAction.ActionType.DebugTeleport));
		}

		public static void ClosingApplication() {
			if (!TrackActions()) return;
			Current.Log(new LoggableAction(LoggableAction.ActionType.IntentionalCloseApplication));
			WriteLog();
		}

		public static void CompletedObjective() {
			if (!TrackActions()) return;
			Current.Log(new LoggableAction(LoggableAction.ActionType.ObjectiveComplete));
		}

		public static void CompletedPhase() {
			if (!TrackActions()) return;
			Current.Log(new LoggableAction(LoggableAction.ActionType.PhaseComplete));
			WriteLog();
		}

		public static void CompletedMatch() {
			if (!TrackActions()) return;
			Current.Log(new LoggableAction(LoggableAction.ActionType.MatchComplete));
			Current.completedMatchWritten = true;
			WriteLog();
		}

		public static void EnteredSavefile() {
			if (!TrackActions()) return;
			Current.Log(new LoggableAction(LoggableAction.ActionType.EnterSavefile));
		}

		public static void DeletedSavefile() {
			if (!TrackActions()) return;
			Current.Log(new LoggableAction(LoggableAction.ActionType.DeletedSavefile));
		}

		public static void RejoinMatch(string matchID) {
			if (Current != null) {
				WriteLog();
				Current = null;
			}
			MatchLog log = LoadLog(matchID);
			if (log != null) {
				Current = log;
				Current.Log(new LoggableAction(LoggableAction.ActionType.MatchRejoin));
				WriteLog();
			}
		}
	}

	[Serializable]
	public class MatchLog {
		public string matchBeginDate;
		public string matchID;
		public string matchDispName;
		public string matchCreator;
		public List<LoggableAction> log = new List<LoggableAction>();

		private bool dirty = true;
		public bool completedMatchWritten = false;

		public void Log(LoggableAction a) {
			log.Add(a);
			dirty = true;
		}

		/// <summary>
		/// Writes this MatchLog to disk
		/// </summary>
		public bool Write() {
			if (!dirty) return false;
			try {
				using (FileStream fs = new FileStream(GetLogPath(), FileMode.Create)) {
					XmlSerializer ser = new XmlSerializer(typeof(MatchLog));
					ser.Serialize(fs, this);
				}
				dirty = false;
				return true;
			}
			catch (Exception e) {
				Logger.Log(LogLevel.Warn, "Head2Head", "Failed to write action log: " + e.Message);
			}
			return false;
		}

		/// <summary>
		/// Gets the path to write the current log to
		/// </summary>
		/// <returns>Returns "" if there is no appropriate path</returns>
		private string GetLogPath() {
			string handle = UserIO.GetHandle(string.Format("Head2Head {0} {1}", matchBeginDate, matchID));
			return handle ?? "";
		}
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
			EnterSavefile = 9,
			DeletedSavefile = 10,
			ObjectiveComplete = 11,
			PhaseComplete = 12,

			MatchRejoin = 98,
			IntentionalCloseApplication = 99,

			ErrorType = 999,
		}

		public LoggableAction()
			: this(ActionType.ErrorType)
		{

		}

		public LoggableAction(ActionType _type) {
			GlobalAreaKey area = PlayerStatus.Current.CurrentArea;
			type = _type;
			instant = SyncedClock.Now.ToString();
			fileTimer = Dialog.FileTime(SaveData.Instance.Time);
			areaSID = PlayerStatus.Current.CurrentArea.SID;
			room = PlayerStatus.Current.CurrentRoom;
			checkpoint = area.IsOverworld ? "Overworld"
				: area.IsValidInstalledMap ? AreaData.GetCheckpointName(area.Local_Safe, room)
				: "[unknown area]";
			matchID = PlayerStatus.Current.CurrentMatchID;
			saveDataIndex = SaveData.Instance?.FileSlot ?? -99;
			levelExitMode = "";
		}

		public ActionType type;
		public string instant;
		public string fileTimer;
		public string areaSID;
		public string room;
		public string checkpoint;
		public string matchID;
		public int saveDataIndex;

		public string levelExitMode;
	}
}
