using System.Collections.Generic;
using UnityEngine;

namespace MobileConsole.UI
{
	/// <summary>
	/// The bug report form. Everything here is tracker agnostic: it renders the common options,
	/// the options of the selected <see cref="BugReporter"/>, composes the report and shows the
	/// result of the request.
	/// </summary>
	public class BugReportViewBuilder : ViewBuilder
	{
		const string SendIcon = "share";
		const string ActionIcon = "action";
		const string DefaultHint = "Fill in the report and press the send button";

		static readonly Color NeutralColor = new Color(0.9f, 0.9f, 0.9f);
		static readonly Color SuccessColor = new Color(0.2f, 0.9f, 0.35f);
		static readonly Color FailureColor = new Color(0.95f, 0.2f, 0.27f);

		readonly List<LogInfo> _filteredLogs;

		BugReporter _reporter;
		LogInfo _selectedLog;
		GenericNodeView _statusNode;

		string _status = DefaultHint;
		Color _statusColor = NeutralColor;

		string _lastIssueUrl;

		byte[] _screenshotPng;
		Sprite _screenshotSprite;
		bool _isCapturingScreenshot;
		bool _isSending;

		public BugReportViewBuilder(List<LogInfo> filteredLogs)
		{
			_filteredLogs = filteredLogs;
			actionButtonIcon = SendIcon;
			actionButtonCallback = OnSend;

			// Sending is asynchronous, the window stays open until the tracker answers
			actionAfterExecuted = ActionAfterExecuted.DoNothing;
		}

		public void Setup(BugReporter reporter, LogInfo selectedLog)
		{
			_reporter = reporter;
			_selectedLog = selectedLog;
			title = reporter.name;
			_status = DefaultHint;
			_statusColor = NeutralColor;
			_lastIssueUrl = null;

			// The screenshot must show the moment the report is opened, not a stale one
			ClearScreenshot();

			PrefillTitle(selectedLog);
			BuildNodes();
		}

		public override void OnPrepareToShow()
		{
			base.OnPrepareToShow();
			BugReportService.options.valueChanged = OnOptionValueChanged;
			TryCaptureScreenshot(false);
		}

		public override void OnPrepareToHide()
		{
			base.OnPrepareToHide();
			BugReportService.options.valueChanged = null;
		}

		#region Build
		void BuildNodes()
		{
			ClearNodes();

			BugReportOptions options = BugReportService.options;

			_statusNode = AddResizableText(FormatStatus());

			if (!string.IsNullOrEmpty(_lastIssueUrl))
			{
				AddButton("Open Issue", ActionIcon, OnOpenIssue);
			}

			AddCommandField(options, "title", "Title");
			AddCommandField(options, "reporter", "Reporter");
			AddCommandField(options, "openIssueAfterCreated", "Open Issue After Created");
			BuildCustomOptionNodes();

			BuildAttachmentNodes(options);
			BuildScreenshotNodes(options);
		}

		void BuildAttachmentNodes(BugReportOptions options)
		{
			Node attachments = CreateCategories("Attachments");
			AddCommandField(options, "attachLogs", "Attach Logs", attachments);
			AddCommandField(options, "useCurrentFilter", "Use Current Filter", attachments);
			AddCommandField(options, "maxLogCount", "Max Log Count", attachments);
			AddCommandField(options, "applicationAndDeviceInfos", "Application and Device Infos", attachments);

			if (_reporter != null && _reporter.supportsAttachments)
			{
				AddCommandField(options, "attachLogsAsFile", "Attach Logs as File", attachments);
			}

			CategoryNodeView logInfo = CreateCategories("Attachments/Log Info");
			logInfo.isExpanded = false;
			AddCommandField(options, "infoEnable", "Enable", logInfo);
			AddCommandField(options, "infoStacktraceEnable", "Stacktrace Enable", logInfo);

			CategoryNodeView logWarning = CreateCategories("Attachments/Log Warning");
			logWarning.isExpanded = false;
			AddCommandField(options, "warningEnable", "Enable", logWarning);
			AddCommandField(options, "warningStacktraceEnable", "Stacktrace Enable", logWarning);

			CategoryNodeView logError = CreateCategories("Attachments/Log Error");
			logError.isExpanded = false;
			AddCommandField(options, "errorEnable", "Enable", logError);
			AddCommandField(options, "errorStacktraceEnable", "Stacktrace Enable", logError);
		}

		void BuildScreenshotNodes(BugReportOptions options)
		{
			if (_reporter == null || !_reporter.supportsAttachments)
				return;

			Node screenshot = CreateCategories("Screenshot");
			AddCommandField(options, "attachScreenshot", "Attach Screenshot", screenshot);

			if (!options.attachScreenshot)
				return;

			AddCommandField(options, "screenshotMaxSize", "Max Size", screenshot);
			AddButton("Retake Screenshot", ActionIcon, OnRetakeScreenshot, screenshot);

			if (_screenshotSprite != null)
			{
				ImageNodeView imageNode = new ImageNodeView();
				imageNode.data = _screenshotSprite;
				imageNode.resizable = true;
				screenshot.AddNode(imageNode);
			}
			else
			{
				AddResizableText(_isCapturingScreenshot ? "Capturing..." : "No screenshot", screenshot);
			}
		}

		void BuildCustomOptionNodes()
		{
			if (_reporter == null)
				return;

			Command customOptions = _reporter.customOptions;
			if (customOptions == null || customOptions.info.variableInfos == null || customOptions.info.variableInfos.Length == 0)
				return;

			// Reporter options sit next to the common fields (title, reporter, ...) instead of
			// being tucked away in their own category, so they read as one flat list.
			AddCommandFields(customOptions, _rootNode);
		}

		void PrefillTitle(LogInfo logInfo)
		{
			BugReportOptions options = BugReportService.options;
			if (logInfo == null || !string.IsNullOrEmpty(options.title))
				return;

			string message = logInfo.shortMessage ?? logInfo.message ?? string.Empty;
			int lineBreakIndex = message.IndexOf('\n');
			if (lineBreakIndex > 0)
			{
				message = message.Substring(0, lineBreakIndex);
			}

			options.title = message.Length > 120 ? message.Substring(0, 120) : message;
			options.SaveAllFieldInfos();
		}
		#endregion

		#region Screenshot
		void OnOptionValueChanged(string varName)
		{
			if (varName != "attachScreenshot")
				return;

			if (BugReportService.options.attachScreenshot)
			{
				TryCaptureScreenshot(true);
			}
			else
			{
				ClearScreenshot();
			}

			BuildNodes();
			Rebuild();
		}

		void OnRetakeScreenshot(GenericNodeView node)
		{
			TryCaptureScreenshot(true);
			BuildNodes();
			Rebuild();
		}

		void TryCaptureScreenshot(bool force)
		{
			BugReportOptions options = BugReportService.options;
			if (_reporter == null || !_reporter.supportsAttachments || !options.attachScreenshot)
				return;

			if (_isCapturingScreenshot || (!force && _screenshotPng != null))
				return;

			_isCapturingScreenshot = true;
			ScreenshotCapture.Capture(options.screenshotMaxSize, OnScreenshotCaptured);
		}

		void OnScreenshotCaptured(byte[] png)
		{
			_isCapturingScreenshot = false;
			SetScreenshot(png);
			BuildNodes();
			Rebuild();
		}

		void SetScreenshot(byte[] png)
		{
			ClearScreenshot();

			if (png == null || png.Length == 0)
				return;

			_screenshotPng = png;

			Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
			if (texture.LoadImage(png))
			{
				_screenshotSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
			}
			else
			{
				Object.Destroy(texture);
			}
		}

		void ClearScreenshot()
		{
			if (_screenshotSprite != null)
			{
				Object.Destroy(_screenshotSprite.texture);
				Object.Destroy(_screenshotSprite);
				_screenshotSprite = null;
			}

			_screenshotPng = null;
		}
		#endregion

		#region Send
		void OnSend()
		{
			if (_reporter == null)
				return;

			if (_isSending)
			{
				SetStatus("A report is already being sent", NeutralColor);
				return;
			}

			BugReport report = BugReportComposer.Compose(_reporter, BugReportService.options, _filteredLogs, _selectedLog, _screenshotPng);

			string error = _reporter.Validate(report);
			if (!string.IsNullOrEmpty(error))
			{
				SetStatus(error, FailureColor);
				return;
			}

			_isSending = true;
			_lastIssueUrl = null;
			SetStatus(string.Format("Sending to {0}...", _reporter.name), NeutralColor);
			BugReportService.Send(_reporter, report, OnSendCompleted);
		}

		void OnSendCompleted(BugReportResult result)
		{
			_isSending = false;

			if (result.isSuccess)
			{
				Debug.LogFormat("[Bug Report] Sent to {0}: {1}", _reporter.name, result);

				_lastIssueUrl = result.issueUrl;
				BugReportService.options.ResetDraft();
				ClearScreenshot();
				SetStatus("Sent! " + result, SuccessColor);

				BuildNodes();
				Rebuild();
			}
			else
			{
				Debug.LogErrorFormat("[Bug Report] Could not send to {0}: {1}", _reporter.name, result.message);
				SetStatus("Failed: " + result.message, FailureColor);
			}
		}

		void OnOpenIssue(GenericNodeView node)
		{
			if (!string.IsNullOrEmpty(_lastIssueUrl))
			{
				Application.OpenURL(_lastIssueUrl);
			}
		}

		void SetStatus(string status, Color color)
		{
			_status = status;
			_statusColor = color;

			if (_statusNode != null)
			{
				_statusNode.name = FormatStatus();
				RefreshUI();
			}
		}

		string FormatStatus()
		{
			return string.Format("<color=#{0}>{1}</color>", ColorUtility.ToHtmlStringRGB(_statusColor), _status);
		}
		#endregion
	}
}
