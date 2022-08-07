﻿using Celeste.Mod.Head2Head.Control;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Head2Head.IO {

	[Serializable]
	public class ActionLogger {

		[NonSerialized]
		private static ActionLogger _instance;
		private static ActionLogger Instance {
			get {
				if (_instance == null) {
					_instance = new ActionLogger();
				}
				return _instance;
			}
		}

		private List<LoggableAction> log = new List<LoggableAction>();

		public static void Log(LoggableAction action) {
			Instance.log.Add(action);
			WriteLog();
		}

		private static void WriteLog() {
			StringBuilder sb = new StringBuilder();
			foreach (LoggableAction la in Instance.log) {
				sb.AppendLine(la.ToString());
			}
			// TODO (!!!) better action logging
			//File.WriteAllText(GetLogPath(), sb.ToString());
		}

		private static string GetLogPath() {
			string appdata = Environment.GetEnvironmentVariable("APPDATA");
			if (string.IsNullOrEmpty(appdata)) throw new IOException("Could not determine path to write action log");
			string directory = Path.Combine(appdata, "Head2Head");
			if (!Directory.Exists(directory)) {
				Directory.CreateDirectory(directory);
			}
			return Path.Combine(directory, "ActionLog.txt");
		}

		// ######################################

		public static void StartingChapter(string desc) {
		}

		public static void EndingChapter(string desc) {
		}

		public static void EnteringRoom(string desc) {
		}

		public static void DebugView(string desc) {
		}

		public static void DebugEnter(string desc) {
		}

		public static void ClosingApplication(string desc) {
		}
	}

	[Serializable]
	public struct LoggableAction {
		public enum ActionType : int {
			ChapterStart = 1,
			ChapterComplete = 2,
			EnterRoom = 3,
			DebugView = 4,
			DebugEnter = 5,
			ErrorLogged = 6,
			IntentionalCloseApplication = 7,

			ParseError = 999,
		}

		public DateTime instant { get; set; }
		public long? H2HTimer { get; set; }
		public ActionType type { get; set; }
		public long FileTimer { get; set; }
		public string description { get; set; }

		private static readonly char delim = '|';

		public override string ToString() {
			StringBuilder sb = new StringBuilder();
			sb.Append(instant);
			sb.Append(delim);
			sb.Append(type);
			sb.Append(delim);
			sb.Append(FileTimer);
			sb.Append(delim);
			if (H2HTimer != null) sb.Append(H2HTimer);
			sb.Append(delim);
			sb.Append(description.Replace("|","/"));
			return sb.ToString();
		}

		public static LoggableAction Parse(string actionString) {
			string[] split = actionString.Split(delim);

			ActionType type;
			DateTime instant;
			long fileTimer;
			long? h2hTimer;
			long _h2hTimer;

			if (split.Length < 5) {
				return GetParseError("String did not have enough pieces");
			}
			if (!DateTime.TryParse(split[0], out instant))
				return GetParseError(string.Format("parsed instant is not valid: {0}", split[0]));
			if (!Enum.TryParse(split[1], out type))
				return GetParseError(string.Format("parsed action is not valid: {0}", split[1]));
			if (!long.TryParse(split[2], out fileTimer))
				return GetParseError(string.Format("parsed file timer is not valid: {0}", split[2]));
			if (long.TryParse(split[3], out _h2hTimer)) h2hTimer = _h2hTimer;
			else h2hTimer = null;

			return new LoggableAction {
				type = type,
				instant = instant,
				FileTimer = fileTimer,
				H2HTimer = h2hTimer,
				description = split[4],
			};
		}

		private static LoggableAction GetParseError(string description) {
			return new LoggableAction {
				type = ActionType.ParseError,
				instant = DateTime.Now,
				FileTimer = 0,
				H2HTimer = null,
				description = string.Format("This action was generated by the logger by a failed attempt to parse a string: {0}.", description),
			};
		}
	}
}
