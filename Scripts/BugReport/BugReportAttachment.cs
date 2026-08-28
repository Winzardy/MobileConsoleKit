using System.Text;

namespace MobileConsole
{
	/// <summary>
	/// A binary payload sent along with the report. The console produces the screenshot and the
	/// log file, a <see cref="BugReporter"/> implementation is free to append its own ones inside
	/// <see cref="BugReporter.OnReportBuilt"/>.
	/// </summary>
	public class BugReportAttachment
	{
		public const string MimeTypePng = "image/png";
		public const string MimeTypeText = "text/plain";

		/// <summary>Logical id, also used as the multipart field name: "screenshot", "logs", ...</summary>
		public readonly string name;
		public readonly string fileName;
		public readonly string mimeType;
		public readonly byte[] data;

		public BugReportAttachment(string name, string fileName, string mimeType, byte[] data)
		{
			this.name = name;
			this.fileName = fileName;
			this.mimeType = mimeType;
			this.data = data;
		}

		public int size
		{
			get { return data != null ? data.Length : 0; }
		}

		public static BugReportAttachment FromText(string name, string fileName, string content)
		{
			return new BugReportAttachment(name, fileName, MimeTypeText, Encoding.UTF8.GetBytes(content ?? string.Empty));
		}

		public static BugReportAttachment FromPng(string name, string fileName, byte[] png)
		{
			return new BugReportAttachment(name, fileName, MimeTypePng, png);
		}

		public override string ToString()
		{
			return string.Format("{0} ({1}, {2:0.#} KB)", fileName, mimeType, size / 1024f);
		}
	}
}
