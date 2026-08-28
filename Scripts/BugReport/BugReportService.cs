using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace MobileConsole
{
	/// <summary>
	/// Registry of the bug tracker integrations and the entry point used by the console UI.
	/// The package ships no implementation on purpose: a project registers its own
	/// <see cref="BugReporter"/>, and only then the bug button appears in the console.
	/// </summary>
	public static class BugReportService
	{
		public const string DefaultIcon = "bug";

		public delegate void ReportersChangedCallback();
		public delegate void ReportSentCallback(BugReporter reporter, BugReport report, BugReportResult result);

		/// <summary>Raised when a reporter is registered or unregistered, the UI uses it to show the bug button.</summary>
		public static event ReportersChangedCallback OnReportersChanged;

		/// <summary>Raised after every submit attempt, successful or not.</summary>
		public static event ReportSentCallback OnReportSent;

		static readonly List<BugReporter> _reporters = new List<BugReporter>();
		static readonly ReadOnlyCollection<BugReporter> _readonlyReporters = new ReadOnlyCollection<BugReporter>(_reporters);
		static BugReportOptions _options;

		/// <summary>Options shared by every reporter. Change the defaults right after <see cref="Register"/>.</summary>
		public static BugReportOptions options
		{
			get
			{
				if (_options == null)
				{
					_options = new BugReportOptions();
					_options.CacheVariableInfos<BugReportOptions>();
					_options.LoadSavedValue();
				}

				return _options;
			}
		}

		public static ReadOnlyCollection<BugReporter> reporters
		{
			get { return _readonlyReporters; }
		}

		public static bool isAvailable
		{
			get { return _reporters.Count > 0; }
		}

		/// <summary>True while a request is in flight, the console blocks a second submit.</summary>
		public static bool isSending { get; private set; }

		public static void Register(BugReporter reporter)
		{
			if (reporter == null)
				throw new ArgumentNullException(nameof(reporter));

			if (_reporters.Contains(reporter))
				return;

			PrepareCustomOptions(reporter);
			_reporters.Add(reporter);

			try
			{
				reporter.OnRegistered();
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}

			NotifyReportersChanged();
		}

		public static void Unregister(BugReporter reporter)
		{
			if (reporter == null)
			{
				Debug.LogError("BugReportService.Unregister was called with a null reporter");
				return;
			}

			if (!_reporters.Remove(reporter))
				return;

			NotifyReportersChanged();
		}

		public static BugReporter FindReporter(string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				Debug.LogErrorFormat("BugReportService.FindReporter was called with an invalid name [{0}]", name);
				return null;
			}

			foreach (var reporter in _reporters)
			{
				if (reporter.name == name)
					return reporter;
			}

			return null;
		}

		/// <summary>
		/// Sends the report and guarantees a single <paramref name="onComplete"/> call, even when
		/// the reporter throws or forgets to report a result.
		/// </summary>
		public static void Send(BugReporter reporter, BugReport report, Action<BugReportResult> onComplete)
		{
			if (reporter == null || report == null)
			{
				Debug.LogError("BugReportService.Send was called with a null reporter or report");
				InvokeSafe(onComplete, BugReportResult.Failure("Reporter or report is null"));
				return;
			}

			if (isSending)
			{
				InvokeSafe(onComplete, BugReportResult.Failure("Another report is being sent"));
				return;
			}

			isSending = true;
			BugReportRunner.Instance.StartCoroutine(SendRoutine(reporter, report, onComplete));
		}

		static IEnumerator SendRoutine(BugReporter reporter, BugReport report, Action<BugReportResult> onComplete)
		{
			bool isCompleted = false;
			BugReportResult result = BugReportResult.Failure("The reporter has not returned any result");

			Action<BugReportResult> callback = r =>
			{
				if (isCompleted)
					return;

				isCompleted = true;
				result = r;
			};

			IEnumerator routine = null;
			try
			{
				routine = reporter.Send(report, callback);
			}
			catch (Exception e)
			{
				Debug.LogException(e);
				callback(BugReportResult.Failure(e.Message));
			}

			// The routine is stepped manually so an exception thrown inside the reporter is turned
			// into a failure instead of silently killing the coroutine.
			while (routine != null)
			{
				object current = null;
				try
				{
					if (!routine.MoveNext())
						break;

					current = routine.Current;
				}
				catch (Exception e)
				{
					Debug.LogException(e);
					callback(BugReportResult.Failure(e.Message));
					break;
				}

				yield return current;
			}

			isSending = false;

			if (OnReportSent != null)
			{
				try
				{
					OnReportSent(reporter, report, result);
				}
				catch (Exception e)
				{
					Debug.LogException(e);
				}
			}

			InvokeSafe(onComplete, result);

			// Opening the url can send the app to the background, so it goes last, after the
			// console window has already been updated with the result
			TryOpenIssue(result);
		}

		/// <summary>
		/// Opens the created issue in the browser when the option is on and the tracker returned a url.
		/// </summary>
		static void TryOpenIssue(BugReportResult result)
		{
			if (!result.isSuccess || !options.openIssueAfterCreated || string.IsNullOrEmpty(result.issueUrl))
				return;

			try
			{
				Application.OpenURL(result.issueUrl);
			}
			catch (Exception e)
			{
				Debug.LogWarningFormat("Could not open the issue url [{0}]: {1}", result.issueUrl, e.Message);
			}
		}

		static void PrepareCustomOptions(BugReporter reporter)
		{
			Command customOptions = reporter.customOptions;
			if (customOptions == null)
				return;

			try
			{
				customOptions.CacheVariableInfos(customOptions.GetType());
				customOptions.InitDefaultVariableValue();
				customOptions.LoadSavedValue();
				customOptions.OnVariableValueLoaded();
			}
			catch (Exception e)
			{
				Debug.LogErrorFormat("There is something wrong with the custom options of: {0}", reporter.GetType().Name);
				Debug.LogException(e);
			}
		}

		static void NotifyReportersChanged()
		{
			if (OnReportersChanged != null)
			{
				OnReportersChanged();
			}
		}

		static void InvokeSafe(Action<BugReportResult> callback, BugReportResult result)
		{
			if (callback == null)
				return;

			try
			{
				callback(result);
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}
		}
	}
}
