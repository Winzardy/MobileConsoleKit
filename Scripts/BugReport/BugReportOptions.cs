using System;

namespace MobileConsole
{
	/// <summary>
	/// Options shared by every bug tracker integration. They are rendered in the report window
	/// and persisted in PlayerPrefs by the standard <see cref="Command"/> machinery, so QA
	/// configures them once per device.
	/// Tracker specific options live in <see cref="BugReporter.customOptions"/>.
	/// </summary>
	public class BugReportOptions : Command
	{
		// --- Report ---
		public string title = string.Empty;
		public string reporter = string.Empty;

		/// <summary>
		/// Opens the created issue in the browser as soon as the tracker returns its url.
		/// Does nothing when the reporter returns no url.
		/// </summary>
		public bool openIssueAfterCreated = true;

		// --- Logs ---
		public bool attachLogs = true;
		public bool useCurrentFilter = false;
		public bool infoEnable = true;
		public bool infoStacktraceEnable = false;
		public bool warningEnable = true;
		public bool warningStacktraceEnable = true;
		public bool errorEnable = true;
		public bool errorStacktraceEnable = true;

		/// <summary>Keeps only the last N logs, so the request stays within the tracker limits. 0 means unlimited.</summary>
		public int maxLogCount = 500;

		/// <summary>Send the logs as a *.log attachment instead of inlining them into the body.</summary>
		public bool attachLogsAsFile = true;

		// --- Context ---
		public bool applicationAndDeviceInfos = true;

		// --- Screenshot ---
		public bool attachScreenshot = true;

		/// <summary>The screenshot is downscaled so that its longest side does not exceed this value.</summary>
		public int screenshotMaxSize = 1280;

		/// <summary>Raised when the user edits any field in the report window.</summary>
		public Action<string> valueChanged { get; set; }

		public override void OnValueChanged(string varName)
		{
			if (valueChanged != null)
			{
				valueChanged(varName);
			}
		}

		public bool IsLogTypeEnabled(UnityEngine.LogType type)
		{
			switch (type)
			{
				case UnityEngine.LogType.Log: return infoEnable;
				case UnityEngine.LogType.Warning: return warningEnable;
				default: return errorEnable;
			}
		}

		public bool IsStacktraceEnabled(UnityEngine.LogType type)
		{
			switch (type)
			{
				case UnityEngine.LogType.Log: return infoStacktraceEnable;
				case UnityEngine.LogType.Warning: return warningStacktraceEnable;
				default: return errorStacktraceEnable;
			}
		}

		/// <summary>Clears the draft after the report has been accepted by the tracker.</summary>
		public void ResetDraft()
		{
			title = string.Empty;
			this.SaveAllFieldInfos();
		}
	}
}
