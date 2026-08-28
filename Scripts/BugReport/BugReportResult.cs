namespace MobileConsole
{
	/// <summary>
	/// Outcome of a single submit attempt, reported back to the console UI.
	/// </summary>
	public struct BugReportResult
	{
		public bool isSuccess;

		/// <summary>Human readable text shown in the report window and written to the log.</summary>
		public string message;

		/// <summary>Issue key created by the tracker, e.g. "QA-1234". Optional.</summary>
		public string issueId;

		/// <summary>Direct link to the created issue. Optional.</summary>
		public string issueUrl;

		public static BugReportResult Success(string issueId = null, string issueUrl = null, string message = null)
		{
			BugReportResult result = new BugReportResult();
			result.isSuccess = true;
			result.issueId = issueId;
			result.issueUrl = issueUrl;
			result.message = message;
			return result;
		}

		public static BugReportResult Failure(string message)
		{
			BugReportResult result = new BugReportResult();
			result.isSuccess = false;
			result.message = message;
			return result;
		}

		public override string ToString()
		{
			if (!isSuccess)
				return string.IsNullOrEmpty(message) ? "Failed" : message;

			if (!string.IsNullOrEmpty(message))
				return message;

			if (!string.IsNullOrEmpty(issueUrl))
				return string.IsNullOrEmpty(issueId) ? issueUrl : string.Format("{0} - {1}", issueId, issueUrl);

			return string.IsNullOrEmpty(issueId) ? "Sent" : "Sent: " + issueId;
		}
	}
}
