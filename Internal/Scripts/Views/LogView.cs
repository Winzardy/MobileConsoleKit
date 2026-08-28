using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace MobileConsole.UI
{
	public class LogView : BaseView, IRecycleScrollViewDelegate
	{
		[SerializeField]
		GameObject _buttonChannel;

		[SerializeField]
		UIBridge _channelNumber;

		[SerializeField]
		Toggle _toggleCollapse;

		[SerializeField]
		UIBridge _logToggleInfo;

		[SerializeField]
		UIBridge _logToggleWarning;

		[SerializeField]
		UIBridge _logToggleError;

		[SerializeField]
		RecycleScrollView _scrollView;

		[SerializeField]
		AssetConfig _assetConfig;

		[SerializeField]
		[Tooltip("Hidden until a project registers a BugReporter")]
		GameObject _buttonBugReport;

		LogDetailViewBuilder _logDetailViewBuilder = new LogDetailViewBuilder();

		List<LogInfo> _filterLogInfos = new List<LogInfo>();
		public List<LogInfo> FilterLogInfos
		{ 
			get { return _filterLogInfos; }
		}

		StringBuilder _cachedText = new StringBuilder(200);

		int _mainThreadId;
		List<LogInfo> _threadedLogInfos;

		void Start()
		{
			_mainThreadId = Thread.CurrentThread.ManagedThreadId;
			_threadedLogInfos = new List<LogInfo>(16);

			_scrollView.SetDelegate(this);

			SetupLogFilter();
			AddLogBeforeSceneLoaded();
			UpdateLogTexts();
			
			LogReceiver.OnLogReceived += OnLogReceived;
			LogFilter.OnChannelConfigChanged += OnChannelConfigChanged;
			EventBridge.OnTimestampVisibilityChanged += UpdateActiveCells;
			EventBridge.OnChannelVisibilityChanged += UpdateActiveCells;
			BugReportService.OnReportersChanged += UpdateBugReportButton;

			UpdateBugReportButton();
		}

		void OnDestroy()
		{
			LogReceiver.OnLogReceived -= OnLogReceived;
			LogFilter.OnChannelConfigChanged -= OnChannelConfigChanged;
			EventBridge.OnTimestampVisibilityChanged -= UpdateActiveCells;
			EventBridge.OnChannelVisibilityChanged -= UpdateActiveCells;
			BugReportService.OnReportersChanged -= UpdateBugReportButton;
		}

		void UpdateBugReportButton()
		{
			if (_buttonBugReport != null)
			{
				_buttonBugReport.SetActive(BugReportService.isAvailable);
			}
		}

		void SetupLogFilter()
		{
			_toggleCollapse.isOn = LogFilter.Instance.IsCollapse;
			_logToggleInfo.toggle = LogFilter.Instance.IsLogTypeEnable(LogType.Log);
			_logToggleWarning.toggle = LogFilter.Instance.IsLogTypeEnable(LogType.Warning);
			_logToggleError.toggle = LogFilter.Instance.IsLogTypeEnable(LogType.Error);
			_channelNumber.text = LogFilter.Instance.GetNumEnabledChannel().ToString();

			LogFilter.Instance.enabled = true;
		}

		void AddLogBeforeSceneLoaded()
		{
			foreach (var logInfo in LogReceiver.LogInfos)
			{
				LogFilter.Result result = LogFilter.Instance.FilterLog(logInfo);
				if (result.isPassFilter)
				{
					_filterLogInfos.Add(logInfo);
				}
			}

			_scrollView.ReloadData();
			_scrollView.MoveViewToBottom();
		}

		void Update()
		{
			if (_threadedLogInfos.Count > 0)
			{
				lock (_threadedLogInfos)
				{
					foreach (var logInfo in _threadedLogInfos)
						OnLogReceived(logInfo);
					_threadedLogInfos.Clear();
				}
			}
		}

		void OnLogReceived(LogInfo logInfo)
		{
			if (Thread.CurrentThread.ManagedThreadId != _mainThreadId)
			{
				lock (_threadedLogInfos)
				{
					_threadedLogInfos.Add(logInfo);
				}
				return;
			}

			LogFilter.Result result = LogFilter.Instance.FilterLog(logInfo);
			if (result.isPassFilter)
			{
				bool isScrollViewAtBottom = _scrollView.IsViewAtBottom();

				_filterLogInfos.Add(logInfo);
				_scrollView.AddCell();

				if (isScrollViewAtBottom)
				{
					_scrollView.MoveViewToBottom();
				}
			}
			else if (result.hasCollapsedInstance)
			{
				UpdateActiveCell(result.collapsedLogInfo.hash);
			}

			UpdateLogTexts();
		}

		void OnChannelConfigChanged()
		{
			_channelNumber.text = LogFilter.Instance.GetNumEnabledChannel().ToString();
		}
		
		#region Handle UI
		public void OnFilterValueChanged(string filterString)
		{
			LogFilter.Instance.filterString = filterString;
			UpdateFilteredLog();
		}

		public void OnFilterSelectionChanged(bool isSelected)
		{
			bool isPortrait = Screen.width < Screen.height;
			if (isPortrait)
			{
				_buttonChannel.gameObject.SetActive(!isSelected);
				_toggleCollapse.gameObject.SetActive(!isSelected);
				_logToggleInfo.gameObject.SetActive(!isSelected);
				_logToggleWarning.gameObject.SetActive(!isSelected);
				_logToggleError.gameObject.SetActive(!isSelected);
			}
		}

		public void OnClearFilter()
		{
			LogFilter.Instance.filterString = "";
			UpdateFilteredLog();
		}

		public void OnToggleLogInfo(bool enabled)
		{
			LogFilter.Instance.SetLogEnable(LogType.Log, enabled);
			UpdateFilteredLog();
		}

		public void OnToggleLogWarning(bool enabled)
		{
			LogFilter.Instance.SetLogEnable(LogType.Warning, enabled);
			UpdateFilteredLog();
		}

		public void OnToggleLogError(bool enabled)
		{
			LogFilter.Instance.SetLogEnable(LogType.Error, enabled);
			UpdateFilteredLog();
		}

		public void OnToggleLogCollapse(bool enabled)
		{
			LogFilter.Instance.IsCollapse = enabled;
			UpdateFilteredLog();
		}

		public void ClearLog()
		{
			LogReceiver.Clear();

			UpdateLogTexts();
			UpdateFilteredLog();

			lock(_threadedLogInfos)
				_threadedLogInfos.Clear();
		}

		public void MoveToBottomLog()
		{
			_scrollView.MoveViewToBottom();
		}

		public void UpdateActiveCells()
		{
			foreach (var cell in _scrollView.GetActiveCells())
			{
				UpdateCell(cell);
			}
		}
		#endregion

		public void UpdateFilteredLog()
		{
			// Dirty fix: I don't want the this function is called when initializing
			// Setting value for Toggle triggers the OnValueChanged method
			if (!LogFilter.Instance.enabled)
				return;

			_filterLogInfos.Clear();
			LogFilter.Instance.ClearLogHashes();

			foreach (var logInfo in LogReceiver.LogInfos)
			{
				LogFilter.Result result = LogFilter.Instance.FilterLog(logInfo);
				if (result.isPassFilter)
				{
					_filterLogInfos.Add(logInfo);
				}
			}

			bool isScrollViewAtBottom = _scrollView.IsViewAtBottom();
			_scrollView.ReloadData();

			if (isScrollViewAtBottom)
			{
				_scrollView.MoveViewToBottom();
			}
		}

		void UpdateLogTexts()
		{
			_logToggleInfo.text = LogReceiver.FormatCount(LogReceiver.NumLogInfo);
			_logToggleWarning.text = LogReceiver.FormatCount(LogReceiver.NumLogWarning);
			_logToggleError.text = LogReceiver.FormatCount(LogReceiver.NumLogError);
		}

		void UpdateActiveCell(int hash)
		{
			foreach (var cell in _scrollView.GetActiveCells())
			{
				if (_filterLogInfos[cell.info.index].hash == hash)
				{
					UpdateCell(cell);
					break;
				}
			}
		}

		public ScrollViewCell ScrollCellCreated(int cellIndex)
		{
			if (cellIndex < 0 || cellIndex >= _filterLogInfos.Count)
			{
				throw new Exception("Cell index is out of range: " + cellIndex);
			}

			ScrollViewCell cell = _scrollView.CreateCell("LogCell");
			return cell;
		}

		public void ScrollCellWillDisplay(ScrollViewCell cell)
		{
			UpdateCell(cell);
		}

		void UpdateCell(ScrollViewCell cell)
		{
			BaseLogCell logCell = cell as BaseLogCell;
			LogInfo logInfo = _filterLogInfos[logCell.info.index];
			AssetConfig.SpriteInfo iconConfig = _assetConfig.GetSpriteInfo(LogTypeToIconName(logInfo.type));
			logCell.SetIcon(iconConfig.sprite);
			logCell.SetIconColor(iconConfig.color);
			logCell.SetBackgroundColor(LogConsoleSettings.GetCellColor(logCell.info.index));

			_cachedText.Length = 0;
			
			if (LogConsoleSettings.Instance.showTimestamp)
				_cachedText.Append(logInfo.time);
			
			if (LogConsoleSettings.Instance.showLogChannel && logInfo.channelInfo != null)
			{
				_cachedText.Append(logInfo.channelInfo.nameWithColor);
				_cachedText.Append("  ");
			}

			_cachedText.Append(logInfo.shortMessage);
			logCell.SetText(_cachedText.ToString());

			if (LogFilter.Instance.IsCollapse)
			{
				logCell.SetCollapseEnable(true);
				logCell.SetCollapseNumber(logInfo.numInstance);
			}
			else
			{
				logCell.SetCollapseEnable(false);
			}
		}

		public void ScrollCellSelected(int cellIndex)
		{
			if (cellIndex < 0 || cellIndex >= _filterLogInfos.Count)
			{
				throw new Exception("Cell index is out of range: " + cellIndex);
			}

			LogInfo logInfo = _filterLogInfos[cellIndex];
			_logDetailViewBuilder.SetLogInfo(logInfo, LogTypeToIconName(logInfo.type));
			LogConsole.PushSubView(_logDetailViewBuilder);
		}

		public float ScrollCellSize(int cellIndex)
		{
			return 120;
		}

		public int ScrollCellCount()
		{
			return _filterLogInfos.Count;
		}

		static string LogTypeToIconName(LogType type)
		{
			switch (type)
			{
				case LogType.Log: return "log_info";
				case LogType.Warning: return "log_warning";
				default: return "log_error";
			}
		}
	}
}
