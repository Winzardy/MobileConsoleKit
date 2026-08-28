using System;
using System.Collections.Generic;
using System.Text;

namespace MobileConsole
{
	/// <summary>
	/// Everything the console knows about an issue. It is built by the console and consumed by
	/// a <see cref="BugReporter"/> implementation, which decides how to map it onto the bug
	/// tracker API.
	/// </summary>
	public class BugReport
	{
		/// <summary>Short summary typed by the user.</summary>
		public string title;

		/// <summary>Who is reporting. Persisted between sessions, so QA types it only once.</summary>
		public string reporter;

		public DateTime utcTime = DateTime.UtcNow;

		/// <summary>The log the report was opened from, null when it was opened from the log list.</summary>
		public LogInfo selectedLog;

		/// <summary>Logs selected by the report options. Empty when logs are not attached.</summary>
		public readonly List<LogInfo> logs = new List<LogInfo>();

		/// <summary>Logs formatted exactly like the "Share log" output. Null when logs are not attached.</summary>
		public string logText;

		/// <summary>Result of <see cref="AppAndDeviceInfo.FullInfos"/>. Null when disabled in the options.</summary>
		public string appAndDeviceInfo;

		/// <summary>
		/// Flat key/value pairs: app version, device model, active scene plus everything registered
		/// in <see cref="BugReportContext"/> by the game. Map it to the custom fields of your tracker.
		/// </summary>
		public readonly Dictionary<string, string> context = new Dictionary<string, string>();

		/// <summary>Binary payloads: screenshot, log file, save game, ...</summary>
		public readonly List<BugReportAttachment> attachments = new List<BugReportAttachment>();

		public bool hasLogs
		{
			get { return logs.Count > 0; }
		}

		public bool hasAttachments
		{
			get { return attachments.Count > 0; }
		}

		public string GetContext(string key, string defaultValue = null)
		{
			return context.GetValueOrDefault(key, defaultValue);
		}

		public void SetContext(string key, string value)
		{
			if (string.IsNullOrEmpty(key))
				return;

			context[key] = value;
		}

		public BugReportAttachment FindAttachment(string name)
		{
			foreach (var attachment in attachments)
			{
				if (attachment.name == name)
					return attachment;
			}

			return null;
		}

		/// <summary>
		/// Single text blob with the context, the selected log and the logs. Useful for trackers
		/// that accept only one text field, and as a fallback when the request fails.
		/// </summary>
		public string BuildPlainTextBody()
		{
			StringBuilder sb = new StringBuilder(1024);

			if (context.Count > 0)
			{
				sb.AppendLine("--- Context ---");
				foreach (var pair in context)
				{
					sb.AppendFormat("{0}: {1}\n", pair.Key, pair.Value);
				}
				sb.AppendLine();
			}

			if (selectedLog != null)
			{
				sb.AppendLine("--- Selected log ---");
				sb.AppendLine(selectedLog.message);
				sb.AppendLine(selectedLog.stackTrace);
				sb.AppendLine();
			}

			if (!string.IsNullOrEmpty(appAndDeviceInfo))
			{
				sb.AppendLine(appAndDeviceInfo);
				sb.AppendLine();
			}

			if (!string.IsNullOrEmpty(logText))
			{
				sb.AppendLine("--- Logs ---");
				sb.AppendLine(logText);
			}

			return sb.ToString();
		}
	}
}
