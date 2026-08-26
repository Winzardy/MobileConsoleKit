using TMPro;
using UnityEngine;

namespace MobileConsole.UI
{
	/// <summary>
	/// Always on screen error/warning counter badge.
	/// This component lives on an always active GameObject and toggles its "_content" child, so it
	/// keeps running - and can show itself again - while the badge is not visible.
	/// </summary>
	public class LogCounterBadge : MonoBehaviour
	{
		[SerializeField]
		GameObject _content;

		[SerializeField]
		TextMeshProUGUI _errorText;

		[SerializeField]
		TextMeshProUGUI _warningText;

		[SerializeField]
		bool _hideWhileConsoleOpen = true;

		// -1 forces the first write
		int _displayedErrors = -1;
		int _displayedWarnings = -1;

		bool _isConsoleOpen;
		volatile bool _isDirty = true;

		void OnEnable()
		{
			LogReceiver.OnLogCountChanged += SetDirty;
			EventBridge.OnLogCounterBadgeModeChanged += SetDirty;
			LogConsole.OnVisibilityChanged += OnConsoleVisibilityChanged;

			_isDirty = true;
		}

		void OnDisable()
		{
			LogReceiver.OnLogCountChanged -= SetDirty;
			EventBridge.OnLogCounterBadgeModeChanged -= SetDirty;
			LogConsole.OnVisibilityChanged -= OnConsoleVisibilityChanged;
		}

		// Logs are received on any thread. Writing a volatile bool is all this handler is allowed
		// to do: every Unity call is deferred to Update.
		void SetDirty()
		{
			_isDirty = true;
		}

		void OnConsoleVisibilityChanged(bool isConsoleOpen)
		{
			_isConsoleOpen = isConsoleOpen;
			_isDirty = true;
		}

		void Update()
		{
			if (!_isDirty)
				return;

			_isDirty = false;
			Refresh();
		}

		void Refresh()
		{
			int errors = LogReceiver.NumLogError;
			int warnings = LogReceiver.NumLogWarning;

			bool show = ShouldShow(errors, warnings);
			if (_content.activeSelf != show)
			{
				_content.SetActive(show);
			}

			if (!show)
				return;

			if (errors != _displayedErrors)
			{
				_displayedErrors = errors;
				_errorText.text = LogReceiver.FormatCount(errors);
			}

			if (warnings != _displayedWarnings)
			{
				_displayedWarnings = warnings;
				_warningText.text = LogReceiver.FormatCount(warnings);
			}
		}

		bool ShouldShow(int errors, int warnings)
		{
			if (_hideWhileConsoleOpen && _isConsoleOpen)
				return false;

			switch (LogConsoleSettings.Instance.logCounterBadgeMode)
			{
				case LogCounterBadgeMode.Always:
					return true;
				case LogCounterBadgeMode.OnErrorOrWarning:
					return errors > 0 || warnings > 0;
				case LogCounterBadgeMode.OnError:
					return errors > 0;
				default:
					return false;
			}
		}
	}
}
