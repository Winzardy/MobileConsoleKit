using System;
using System.Collections.Generic;
using UnityEngine;

namespace MobileConsole
{
	public static class RecentCommandsHistory
	{
		const string PlayerPrefsKey = "MobileConsole.RecentCommands";
		const char Separator = '\n';

		static readonly List<string> _recentCommandPaths = new List<string>();
		static bool _isLoaded;

		public static event Action OnChanged;

		public static void Record(Command command)
		{
			if (command == null || string.IsNullOrEmpty(command.info.fullPath))
				return;

			EnsureLoaded();

			string fullPath = command.info.fullPath;
			_recentCommandPaths.Remove(fullPath);
			_recentCommandPaths.Insert(0, fullPath);

			TrimToLimit();
			Save();
			NotifyChanged();
		}

		public static List<Command> ResolveCommands(List<Command> commands)
		{
			EnsureLoaded();

			var commandByPath = new Dictionary<string, Command>();
			foreach (var command in commands)
			{
				if (command == null || string.IsNullOrEmpty(command.info.fullPath))
					continue;

				if (!commandByPath.ContainsKey(command.info.fullPath))
				{
					commandByPath.Add(command.info.fullPath, command);
				}
			}

			var recentCommands = new List<Command>();
			bool hasRemovedStaleItem = false;
			for (int i = _recentCommandPaths.Count - 1; i >= 0; i--)
			{
				string path = _recentCommandPaths[i];
				if (commandByPath.TryGetValue(path, out var command))
				{
					recentCommands.Add(command);
				}
				else
				{
					_recentCommandPaths.RemoveAt(i);
					hasRemovedStaleItem = true;
				}
			}

			recentCommands.Reverse();

			if (hasRemovedStaleItem)
			{
				Save();
			}

			return recentCommands;
		}

		public static void EnforceLimit()
		{
			EnsureLoaded();

			int oldCount = _recentCommandPaths.Count;
			TrimToLimit();
			if (oldCount == _recentCommandPaths.Count)
				return;

			Save();
			NotifyChanged();
		}

		static void EnsureLoaded()
		{
			if (_isLoaded)
				return;

			_isLoaded = true;
			_recentCommandPaths.Clear();

			string rawValue = PlayerPrefs.GetString(PlayerPrefsKey, string.Empty);
			if (string.IsNullOrEmpty(rawValue))
				return;

			string[] paths = rawValue.Split(new[] { Separator }, StringSplitOptions.RemoveEmptyEntries);
			foreach (var path in paths)
			{
				if (!string.IsNullOrEmpty(path))
				{
					_recentCommandPaths.Add(path);
				}
			}

			TrimToLimit();
		}

		static void TrimToLimit()
		{
			int limit = Mathf.Max(0, LogConsoleSettings.Instance.recentCommandsLimit);
			if (limit == 0)
			{
				_recentCommandPaths.Clear();
				return;
			}

			if (_recentCommandPaths.Count > limit)
			{
				_recentCommandPaths.RemoveRange(limit, _recentCommandPaths.Count - limit);
			}
		}

		static void Save()
		{
			PlayerPrefs.SetString(PlayerPrefsKey, string.Join(Separator.ToString(), _recentCommandPaths));
		}

		static void NotifyChanged()
		{
			if (OnChanged != null)
			{
				OnChanged();
			}
		}
	}
}
