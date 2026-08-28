namespace MobileConsole.UI
{
	/// <summary>
	/// Shown instead of the report form when a project registers several bug trackers,
	/// for example a QA tracker and a designer feedback board.
	/// </summary>
	public class BugReporterPickerViewBuilder : ViewBuilder
	{
		public delegate void SelectCallback(BugReporter reporter);

		readonly SelectCallback _onSelected;

		public BugReporterPickerViewBuilder(SelectCallback onSelected)
		{
			_onSelected = onSelected;
			title = "Report a Bug";
			saveScrollViewPosition = false;
		}

		public override void OnPrepareToShow()
		{
			base.OnPrepareToShow();
			BuildNodes();
		}

		void BuildNodes()
		{
			ClearNodes();

			foreach (var reporter in BugReportService.reporters)
			{
				BugReporter selectedReporter = reporter;
				AddButton(reporter.name, reporter.icon, node => OnReporterSelected(selectedReporter));
			}
		}

		void OnReporterSelected(BugReporter reporter)
		{
			if (_onSelected != null)
			{
				_onSelected(reporter);
			}
		}
	}
}
