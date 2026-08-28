using System;
using System.Collections.Generic;
using UnityEngine;

namespace MobileConsole
{
	/// <summary>
	/// Game specific values attached to every bug report: player id, current level, session id,
	/// build branch, A/B test buckets and so on. Register the providers once at startup, they are
	/// evaluated at the moment the report is composed.
	/// <code>
	/// BugReportContext.Register("player.id", () => Profile.Current.Id);
	/// BugReportContext.Register("game.level", () => LevelManager.Current.Name);
	/// </code>
	/// </summary>
	public static class BugReportContext
	{
		public delegate string Provider();

		static readonly List<string> _keys = new List<string>();
		static readonly Dictionary<string, Provider> _providers = new Dictionary<string, Provider>();

		public static void Register(string key, Provider provider)
		{
			if (string.IsNullOrEmpty(key) || provider == null)
			{
				Debug.LogErrorFormat("BugReportContext.Register was called with an invalid key [{0}] or a null provider", key);
				return;
			}

			if (!_providers.ContainsKey(key))
			{
				_keys.Add(key);
			}

			_providers[key] = provider;
		}

		public static void Unregister(string key)
		{
			if (string.IsNullOrEmpty(key))
			{
				Debug.LogErrorFormat("BugReportContext.Unregister was called with an invalid key [{0}]", key);
				return;
			}

			if (!_providers.Remove(key))
				return;

			_keys.Remove(key);
		}

		public static void Clear()
		{
			_keys.Clear();
			_providers.Clear();
		}

		/// <summary>
		/// Evaluates every provider into <paramref name="target"/>. A provider that throws does not
		/// break the report, the exception text is stored instead.
		/// </summary>
		public static void Collect(IDictionary<string, string> target)
		{
			if (target == null)
				return;

			foreach (var key in _keys)
			{
				Provider provider;
				if (!_providers.TryGetValue(key, out provider))
					continue;

				try
				{
					target[key] = provider();
				}
				catch (Exception e)
				{
					Debug.LogWarningFormat("Bug report context provider [{0}] has failed: {1}", key, e.Message);
					target[key] = "<error: " + e.Message + ">";
				}
			}
		}
	}
}
