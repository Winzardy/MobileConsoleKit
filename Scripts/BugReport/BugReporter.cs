using System;
using System.Collections;

namespace MobileConsole
{
	/// <summary>
	/// Base class for a bug tracker integration. The package knows nothing about the tracker:
	/// it collects the logs, the screenshot and the context, renders the form and hands the
	/// resulting <see cref="BugReport"/> over to this class.
	///
	/// Register an implementation once at startup:
	/// <code>
	/// BugReportService.Register(new MyTrackerBugReporter());
	/// </code>
	/// As soon as at least one reporter is registered, a bug button shows up in the log list and
	/// in the log detail window.
	/// </summary>
	public abstract class BugReporter
	{
		/// <summary>Shown as the report window title and in the picker when several reporters are registered.</summary>
		public virtual string name
		{
			get { return GetType().Name.GetReadableName(); }
		}

		/// <summary>Name of the icon in the Asset Config, "bug" by default.</summary>
		public virtual string icon
		{
			get { return BugReportService.DefaultIcon; }
		}

		/// <summary>
		/// Tracker specific options (severity, assignee, ...). Return a plain class derived from
		/// <see cref="Command"/> with public fields: the console renders them alongside the common
		/// report fields (title, reporter, ...) in the report window and persists them in
		/// PlayerPrefs automatically. Return null when the tracker needs no extra options.
		///
		/// Fields inherited from a shared base class are collected too, fields declared on
		/// <see cref="Command"/> itself are not. Give every reporter its own options class: the
		/// values are persisted per type, so two reporters sharing one options class would share
		/// the saved values as well.
		/// </summary>
		public virtual Command customOptions
		{
			get { return null; }
		}

		/// <summary>Set to false when the tracker cannot take files: attachment options are hidden.</summary>
		public virtual bool supportsAttachments
		{
			get { return true; }
		}

		/// <summary>Called once when the reporter is registered.</summary>
		public virtual void OnRegistered()
		{
		}

		/// <summary>
		/// Last chance to enrich or patch the report before it is sent: extra context fields,
		/// extra attachments, a title template, and so on.
		/// </summary>
		public virtual void OnReportBuilt(BugReport report)
		{
		}

		/// <summary>
		/// Returns an error message when the report cannot be sent (empty title, endpoint not
		/// configured, ...), null when everything is fine. The message is shown in the window and
		/// the request is not fired.
		/// </summary>
		public virtual string Validate(BugReport report)
		{
			return string.IsNullOrEmpty(report.title) ? "Title is required" : null;
		}

		/// <summary>
		/// Sends the report. Implemented as a coroutine so UnityWebRequest can simply be yielded.
		/// Always call <paramref name="onComplete"/> exactly once, the console waits for it to
		/// unlock the send button. Exceptions thrown here are caught and reported as a failure.
		/// </summary>
		public abstract IEnumerator Send(BugReport report, Action<BugReportResult> onComplete);
	}
}
