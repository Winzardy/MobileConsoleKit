using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MobileConsole
{
	/// <summary>
	/// Turns the report options, the console state and the game context into a <see cref="BugReport"/>.
	/// Everything tracker specific happens later, in <see cref="BugReporter"/>.
	/// </summary>
	internal static class BugReportComposer
	{
		public const string ScreenshotAttachmentName = "screenshot";
		public const string LogsAttachmentName = "logs";

		public static BugReport Compose(BugReporter reporter,
										BugReportOptions options,
										IList<LogInfo> filteredLogs,
										LogInfo selectedLog,
										byte[] screenshotPng)
		{
			BugReport report = new BugReport();
			report.title = Trim(options.title);
			report.reporter = Trim(options.reporter);
			report.utcTime = DateTime.UtcNow;
			report.selectedLog = selectedLog;

			AppendLogs(report, options, filteredLogs);

			if (options.applicationAndDeviceInfos)
			{
				report.appAndDeviceInfo = AppAndDeviceInfo.FullInfos();
			}

			AppendStandardContext(report);
			BugReportContext.Collect(report.context);

			if (reporter.supportsAttachments)
			{
				if (options.attachLogs && options.attachLogsAsFile && !string.IsNullOrEmpty(report.logText))
				{
					report.attachments.Add(BugReportAttachment.FromText(LogsAttachmentName, BuildLogFileName(), report.logText));
				}

				if (options.attachScreenshot && screenshotPng != null && screenshotPng.Length > 0)
				{
					report.attachments.Add(BugReportAttachment.FromPng(ScreenshotAttachmentName, "screenshot.png", screenshotPng));
				}
			}

			try
			{
				reporter.OnReportBuilt(report);
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}

			return report;
		}

		static void AppendLogs(BugReport report, BugReportOptions options, IList<LogInfo> filteredLogs)
		{
			if (!options.attachLogs)
				return;

			IList<LogInfo> source = options.useCurrentFilter ? filteredLogs : LogReceiver.LogInfos;
			LogTextFormatter.Snapshot(source, options.maxLogCount, report.logs);
			report.logText = LogTextFormatter.Format(report.logs, options.IsLogTypeEnabled, options.IsStacktraceEnabled);
		}

		static void AppendStandardContext(BugReport report)
		{
			report.SetContext("app.name", Application.productName);
			report.SetContext("app.version", Application.version);
			report.SetContext("app.buildNumber", EventBridge.AppVersionCode);
			report.SetContext("app.bundleId", EventBridge.AppBundleIdentifier);
			report.SetContext("app.unityVersion", Application.unityVersion);
			report.SetContext("app.platform", Application.platform.ToString());
			report.SetContext("app.isDebugBuild", Debug.isDebugBuild.ToString());

			report.SetContext("device.model", SystemInfo.deviceModel);
			report.SetContext("device.name", SystemInfo.deviceName);
			report.SetContext("device.os", SystemInfo.operatingSystem);
			report.SetContext("device.language", Application.systemLanguage.ToString());
			report.SetContext("device.memoryMb", SystemInfo.systemMemorySize.ToString());
			report.SetContext("device.gpu", SystemInfo.graphicsDeviceName);
			report.SetContext("device.screen", string.Format("{0}x{1}", Screen.width, Screen.height));
			report.SetContext("device.connectivity", Application.internetReachability.ToString());

			report.SetContext("log.errors", LogReceiver.NumLogError.ToString());
			report.SetContext("log.warnings", LogReceiver.NumLogWarning.ToString());
			report.SetContext("log.infos", LogReceiver.NumLogInfo.ToString());

			try
			{
				report.SetContext("runtime.scene", SceneManager.GetActiveScene().name);
				report.SetContext("runtime.playtime", FormatDuration(Time.realtimeSinceStartup));
			}
			catch (Exception e)
			{
				Debug.LogWarningFormat("Could not collect the runtime context: {0}", e.Message);
			}
		}

		static string BuildLogFileName()
		{
			return string.Format("{0}_{1}_{2}.txt",
				SanitizeFileName(Application.productName),
				Application.version,
				DateTime.Now.ToString("MMMM-dd_HH-mm-ss"));
		}

		static string FormatDuration(float seconds)
		{
			TimeSpan timeSpan = TimeSpan.FromSeconds(seconds);
			return string.Format("{0:00}:{1:00}:{2:00}", (int)timeSpan.TotalHours, timeSpan.Minutes, timeSpan.Seconds);
		}

		static string Trim(string value)
		{
			return string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
		}

		static string SanitizeFileName(string fileName)
		{
			if (string.IsNullOrEmpty(fileName))
				return "Application";

			foreach (char invalidFileNameChar in Path.GetInvalidFileNameChars())
				fileName = fileName.Replace(invalidFileNameChar, '_');
			foreach (char invalidFileNameChar in "<>:\"/\\|?*")
				fileName = fileName.Replace(invalidFileNameChar, '_');

			string sanitized = fileName.Trim(' ', '.');
			return string.IsNullOrEmpty(sanitized) ? "Application" : sanitized;
		}
	}
}
