using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MobileConsole
{
	/// <summary>
	/// Renders logs the way "Share log" does. Shared by the share window and the bug report window
	/// so both produce byte identical dumps.
	/// </summary>
	internal static class LogTextFormatter
	{
		public delegate bool LogTypeFilter(LogType type);

		public const string Separator = "------------------";

		public static string Format(IList<LogInfo> logInfos, LogTypeFilter isTypeEnabled, LogTypeFilter isStacktraceEnabled)
		{
			StringBuilder sb = new StringBuilder(4096);
			Append(sb, logInfos, isTypeEnabled, isStacktraceEnabled);
			return sb.ToString();
		}

		public static void Append(StringBuilder sb, IList<LogInfo> logInfos, LogTypeFilter isTypeEnabled, LogTypeFilter isStacktraceEnabled)
		{
			if (logInfos == null)
				return;

			for (int i = 0; i < logInfos.Count; i++)
			{
				LogInfo logInfo = logInfos[i];
				if (logInfo == null || (isTypeEnabled != null && !isTypeEnabled(logInfo.type)))
					continue;

				sb.AppendFormat("[{0}] ", logInfo.type.ToString());
				sb.Append(logInfo.time);
				if (logInfo.channelInfo != null)
				{
					sb.AppendFormat(LogConsoleSettings.Instance.channelFormat, logInfo.channelInfo.name);
				}
				sb.AppendLine(logInfo.message);

				if (isStacktraceEnabled != null && isStacktraceEnabled(logInfo.type))
				{
					sb.AppendLine(Separator);
					sb.AppendLine(logInfo.stackTrace);
					sb.AppendLine();
				}
			}
		}

		/// <summary>
		/// Snapshots the source list, keeping only the last <paramref name="maxCount"/> entries.
		/// Copying by index instead of foreach, because logs can be appended from a background thread.
		/// </summary>
		public static void Snapshot(IList<LogInfo> source, int maxCount, List<LogInfo> target)
		{
			target.Clear();
			if (source == null)
				return;

			int count = source.Count;
			int startIndex = maxCount > 0 && count > maxCount ? count - maxCount : 0;

			for (int i = startIndex; i < count && i < source.Count; i++)
			{
				LogInfo logInfo = source[i];
				if (logInfo != null)
				{
					target.Add(logInfo);
				}
			}
		}
	}
}
