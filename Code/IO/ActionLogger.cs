using Celeste.Mod.CelesteNet;
using Celeste.Mod.Head2Head.Control;
using Celeste.Mod.Head2Head.Integration;
using Celeste.Mod.Head2Head.Shared;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Serialization;
using static Celeste.Mod.Head2Head.IO.LoggableAction;


namespace Celeste.Mod.Head2Head.IO {
	public class ActionLogger {
		public static MatchLog Current { get; private set; }
		private static Dictionary<string, MatchLog> allLogs = new();

		private static bool TrackActions(bool skipCurrentCheck = false) {
			return (skipCurrentCheck || Current != null) && RoleLogic.LogMatchActions();
		}

		public static bool WriteLog() {
			if (Current == null) return false;
			allLogs[Current.MatchID] = Current;
			return Current.Write();
		}

		public static MatchLog LoadLog(string matchID) {
			if (allLogs.TryGetValue(matchID, out MatchLog ret)) {
				return ret;
			}
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
			if (allLogs.ContainsKey(matchID)) return true;
			return File.Exists(GetLogFileName(matchID));
		}

		public static void PurgeOldLogs() {
			// Clean up logs for matches completed more than 20 minutes ago
			List<string> logsToRemove = new();
			foreach (MatchLog log in allLogs.Values) {
				if (log.CompletionInstant != null && (SyncedClock.Now - log.CompletionInstant.Value).TotalMinutes > 20) {
					log.Write();
					logsToRemove.Add(log.MatchID);
				}
			}
			foreach (string s in logsToRemove) {
				allLogs.Remove(s);
			}
			// Delete log files older than today or yesterday
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
			if (Current == null || Current.MatchID != def.MatchID) {
				WriteLog();
				Current = new MatchLog() {
					MatchBeginDate = def.BeginInstant.ToShortDateString().Replace('/', '-'),
					MatchID = def.MatchID,
					MatchDispName = def.CategoryDisplayName,
					MatchCreator = def.Owner.DisplayName,
					SerializedRunnerID = PlayerID.MyIDSafe.SerializedID,
				};
				allLogs[def.MatchID] = Current;
			}
			Current.Log(new LoggableAction(ActionType.MatchStart));
			WriteLog();
		}

		public static void EnteringArea() {
			if (!TrackActions()) return;
			Current.Log(new LoggableAction(ActionType.AreaEnter));
			Current.Write();
		}

		public static void ExitingArea(LevelExit.Mode _mode) {
			if (!TrackActions()) return;
			Current.Log(new LoggableAction(ActionType.AreaExit) {
				LevelExitMode = _mode.ToString(),
			});
		}

		public static void AreaCompleted() {
			if (!TrackActions()) return;
			Current.Log(new LoggableAction(ActionType.AreaComplete));
		}

		public static void EnteringRoom() {
			if (!TrackActions()) return;
			LoggableAction la = new LoggableAction(ActionType.EnterRoom);
			Current.Log(la);
			if (!string.IsNullOrEmpty(la.Checkpoint)) {
				WriteLog();
			}
		}

		public static void DebugView() {
			if (!TrackActions()) return;
			Current.Log(new LoggableAction(ActionType.DebugView));
		}

		public static void DebugTeleport() {
			if (!TrackActions()) return;
			Current.Log(new LoggableAction(ActionType.DebugTeleport));
		}

		public static void ClosingApplication() {
			if (!TrackActions()) return;
			Current.Log(new LoggableAction(ActionType.IntentionalCloseApplication));
			WriteLog();
		}

		public static void CompletedObjective() {
			if (!TrackActions()) return;
			Current.Log(new LoggableAction(ActionType.ObjectiveComplete));
		}

		public static void CompletedPhase() {
			if (!TrackActions()) return;
			Current.Log(new LoggableAction(ActionType.PhaseComplete));
			WriteLog();
		}

		public static void CompletedMatch() {
			if (!TrackActions()) return;
			Current.Log(new LoggableAction(ActionType.MatchComplete));
			WriteLog();
			Current = null;
		}

		public static void EnteredSavefile() {
			if (!TrackActions()) return;
			Current.Log(new LoggableAction(ActionType.EnterSavefile));
		}

		public static void DeletedSavefile() {
			if (!TrackActions()) return;
			Current.Log(new LoggableAction(ActionType.DeletedSavefile));
		}

		public static void RejoinMatch(string matchID) {
			if (Current != null) {
				WriteLog();
				Current = null;
			}
			MatchLog log = LoadLog(matchID);
			if (log != null) {
				Current = log;
				allLogs[log.MatchID] = log;
				Current.Log(new LoggableAction(ActionType.MatchRejoin));
				WriteLog();
			}
		}
	}

	[Serializable]
	public class MatchLog {
		public string MatchBeginDate { get; set; }
		public string MatchID { get; set; }
		public string MatchDispName { get; set; }
		public string MatchCreator { get; set; }
		[JsonIgnore]
		public string SerializedRunnerID { get; set; }
		public PlayerID RunnerID => PlayerID.FromSerialized(SerializedRunnerID);
		public List<LoggableAction> Events { get; set; } = new();
		[JsonIgnore]
		public DateTime? CompletionInstant { get; set; } = null;
		public string CompletionTime => CompletionInstant?.ToString() ?? "";

		[NonSerialized]
		private bool dirty = true;

		public void Log(LoggableAction a) {
			Events.Add(a);
			if (a.Type == ActionType.MatchComplete) {
				CompletionInstant = SyncedClock.Now;
			}
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
			string handle = UserIO.GetHandle(string.Format("Head2Head {0} {1}", MatchBeginDate, MatchID));
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

		public LoggableAction() : this(ActionType.ErrorType) { }

		public LoggableAction(ActionType _type) {
			GlobalAreaKey area = PlayerStatus.Current.CurrentArea;
			Type = _type;
			Instant = SyncedClock.Now.ToString();
			long saveTime = SaveData.Instance?.Time ?? 0;
			FileTimer = Dialog.FileTime(saveTime);
			MatchTimer = Dialog.FileTime(saveTime - PlayerStatus.Current.FileTimerAtMatchBegin);
			AreaSID = PlayerStatus.Current.CurrentArea.SID;
			Room = PlayerStatus.Current.CurrentRoom;
			Checkpoint = area.IsOverworld ? ""
				: !area.IsValidInstalledMap ? ""
				: AreaData.GetCheckpointName(area.Local_Safe, Room) ?? "";
			SaveDataIndex = SaveData.Instance?.FileSlot ?? -99;
			LevelExitMode = "";
		}

		public ActionType Type { get; set; }
		public string Label => Type.ToString();
		public string Instant { get; set; }
		public string FileTimer { get; set; }
		public string MatchTimer { get; set; }
		public string AreaSID { get; set; }
		public string Room { get; set; }
		public string Checkpoint { get; set; }
		public int SaveDataIndex { get; set; }
		public string LevelExitMode { get; set; }
	}

	public static class ActionLogExtensions {
		public static void Write(this CelesteNetBinaryWriter w, MatchLog log, int actionStartIdx = 0, int numActions = 9999999) {
			w.Write(log.MatchBeginDate ?? "");
			w.Write(log.MatchID ?? "");
			w.Write(log.MatchDispName ?? "");
			w.Write(log.MatchCreator ?? "");
			w.Write(log.SerializedRunnerID ?? "");
			int actualActionsWritten = Math.Min(log.Events.Count, actionStartIdx + numActions) - actionStartIdx;
			w.Write(actualActionsWritten);
			for (int i = actionStartIdx; i < log.Events.Count && i < actionStartIdx + numActions; i++) {
				LoggableAction action = log.Events[i];
				w.Write(action);
			}
		}

		public static MatchLog ReadMatchLog(this CelesteNetBinaryReader r) {
			MatchLog log = new();
			log.MatchBeginDate = r.ReadString();
			log.MatchID = r.ReadString();
			log.MatchDispName = r.ReadString();
			log.MatchCreator = r.ReadString();
			log.SerializedRunnerID = r.ReadString();
			int numActionsSent = r.ReadInt32();
			List<LoggableAction> l = new(numActionsSent);
			for (int i = 0; i < numActionsSent; i++) {
				l.Add(r.ReadLoggableAction());
			}
			return log;
		}

		public static void Write(this CelesteNetBinaryWriter w, LoggableAction a) {
			w.Write(a.Type.ToString());
			w.Write(a.Instant ?? "");
			w.Write(a.FileTimer ?? "");
			w.Write(a.MatchTimer ?? "");
			w.Write(a.AreaSID ?? "");
			w.Write(a.Room ?? "");
			w.Write(a.Checkpoint ?? "");
			w.Write(a.SaveDataIndex);
			w.Write(a.LevelExitMode ?? "");
		}

		public static LoggableAction ReadLoggableAction(this CelesteNetBinaryReader r) {
			LoggableAction act = new();
			act.Type = Enum.Parse<ActionType>(r.ReadString());
			act.Instant = r.ReadString();
			act.FileTimer = r.ReadString();
			act.MatchTimer = r.ReadString();
			act.AreaSID = r.ReadString();
			act.Room = r.ReadString();
			act.Checkpoint = r.ReadString();
			act.SaveDataIndex = r.ReadInt32();
			act.LevelExitMode = r.ReadString();
			return act;
		}
	}
}
