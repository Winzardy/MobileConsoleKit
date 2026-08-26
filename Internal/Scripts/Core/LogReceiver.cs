using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;

namespace MobileConsole
{
	public static class LogReceiver
	{
		public delegate void LogCallback(LogInfo logInfo);
		public static event LogCallback OnLogReceived;

		public delegate void CountCallback();

		/// <summary>
		/// Raised whenever the per type counters change (a log was received, or Clear() was called).
		/// WARNING: logs are received on any thread, so this can be raised from a background thread.
		/// Handlers must not touch the Unity API directly.
		/// </summary>
		public static event CountCallback OnLogCountChanged;

		static Pool<LogInfo> _logInfoPool;
		static List<LogInfo> _logInfos = new List<LogInfo>();
		static object _logInfosLock = new object();
		static int _limitCharacterView = 200;

		internal static List<LogInfo> LogInfos
		{
			get { return _logInfos; }
		}

		static int _numLogInfo;
		static int _numLogWarning;
		static int _numLogError;

		public static int NumLogInfo => Volatile.Read(ref _numLogInfo);
		public static int NumLogWarning => Volatile.Read(ref _numLogWarning);
		public static int NumLogError => Volatile.Read(ref _numLogError);

		/// <summary>
		/// Shared display formatting, so every counter view shows the exact same value.
		/// </summary>
		public static string FormatCount(int count)
		{
			return count > 999 ? "999+" : count.ToString();
		}

        public static void Init()
        {
#if UNITY_EDITOR
			_logInfoPool = new Pool<LogInfo>(1);
#else
	        _logInfoPool = new Pool<LogInfo>(128);
#endif
            Application.logMessageReceivedThreaded += LogMessageReceived;
			LogFilter.Instance.RegisterChannelListener();
        }

		static void LogMessageReceived(string message, string stackTrace, LogType type)
		{
			try
			{
				Match match = Regex.Match(message, LogConsoleSettings.Instance.channelRegex);

				LogInfo logInfo = null;

				lock (_logInfosLock)
				{
					logInfo = _logInfoPool.Get();
					if (match.Success)
					{
						logInfo.channelInfo = LogChannelInfoCache.GetOrCreateChannelInfo(match.Groups[1].Value);
					}
					else
					{
						logInfo.channelInfo = null;
					}
				}

				logInfo.message = match.Success ? message.Substring(match.Length) : message;
				logInfo.shortMessage = (logInfo.message.Length <= _limitCharacterView)
					? logInfo.message
					: logInfo.message.Substring(0, _limitCharacterView);
				logInfo.stackTrace = stackTrace;
				logInfo.type = ConvertLogType(type);
				logInfo.time = System.DateTime.Now.ToString(LogConsoleSettings.Instance.timeFormat);
				logInfo.numInstance = 0;
				logInfo.hash = (logInfo.message + logInfo.stackTrace).GetHashCode();

				lock (_logInfosLock)
				{
					_logInfos.Add(logInfo);
					IncreaseLogNumber(logInfo.type);
				}

				// Notify the counters first, so a OnLogReceived handler reading them sees a consistent state
				if (OnLogCountChanged != null)
				{
					OnLogCountChanged();
				}

				if (OnLogReceived != null)
				{
					OnLogReceived(logInfo);
				}
			}
			catch {}
		}

        public static void Clear()
		{
			lock (_logInfosLock)
			{
				if (_logInfoPool != null)
				{
#if UNITY_EDITOR
					_logInfoPool = new Pool<LogInfo>(1);
#else
					_logInfoPool.Return(_logInfos);
#endif
					_logInfos.Clear();
				}

				Interlocked.Exchange(ref _numLogInfo, 0);
				Interlocked.Exchange(ref _numLogWarning, 0);
				Interlocked.Exchange(ref _numLogError, 0);
			}

			if (OnLogCountChanged != null)
			{
				OnLogCountChanged();
			}
		}

		// The type is already collapsed by ConvertLogType, so there are only 3 buckets
		static void IncreaseLogNumber(LogType convertedType)
		{
			switch (convertedType)
			{
				case LogType.Warning:
					Interlocked.Increment(ref _numLogWarning);
					break;
				case LogType.Error:
					Interlocked.Increment(ref _numLogError);
					break;
				default:
					Interlocked.Increment(ref _numLogInfo);
					break;
			}
		}

		static LogType ConvertLogType(LogType logType)
		{
			switch (logType)
			{
				case LogType.Error:
				case LogType.Exception:
				case LogType.Assert:
					return LogType.Error;
				default:
					return logType;
			}
		}
	}
}
