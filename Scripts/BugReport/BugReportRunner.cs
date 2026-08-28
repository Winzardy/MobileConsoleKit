using UnityEngine;

namespace MobileConsole
{
	/// <summary>
	/// Hidden MonoBehaviour that hosts the sending and the screenshot coroutines, so the bug report
	/// keeps running even when the console window is closed.
	/// </summary>
	public class BugReportRunner : MonoBehaviour
	{
		static BugReportRunner _instance;

		public static BugReportRunner Instance
		{
			get
			{
				if (_instance == null)
				{
					GameObject go = new GameObject("MCK Bug Report Runner");
					go.hideFlags = HideFlags.HideInHierarchy;
					DontDestroyOnLoad(go);
					_instance = go.AddComponent<BugReportRunner>();
				}

				return _instance;
			}
		}

		void OnDestroy()
		{
			if (_instance == this)
			{
				_instance = null;
			}
		}
	}
}
