using System;

namespace MobileConsole
{
	public static class CommandInfoExtension
	{
		public static void CopyData(this CommandInfo info, ExecutableCommandAttribute attribute, Type type, bool addEllipsis)
		{
			string[] paths = null;
			if (attribute.name != null)
			{
				info.fullPath = attribute.name.Trim('/');
				if (addEllipsis && !info.fullPath.EndsWith("..."))
					info.fullPath += "...";
				paths = info.fullPath.Split('/');
			}

			if (paths == null || paths.Length == 0)
			{
				info.categories = new string[0];
				info.name = type.Name.GetReadableName();
			}
			else
			{
				info.name = paths[paths.Length - 1];
				info.categories = new string[paths.Length - 1];
				if (paths.Length > 1)
				{
					Array.Copy(paths, info.categories, paths.Length - 1);
				}
			}

			AppendHotKeyDisplay(info, CommandHotKeyDisplay.Format(attribute));

			info.description = attribute.description;
			info.order = attribute.order;
			info.isFavorite = attribute.isFavorite;
		}

		public static void CopyData(this CommandInfo info, SettingCommandAttribute attribute, Type type)
		{
			string[] paths = null;
			if (attribute.name != null)
			{
				info.fullPath = attribute.name;
				paths = attribute.name.Trim('/').Split('/');
			}

			if (paths == null || paths.Length == 0)
			{
				info.categories = new string[1] { type.Name.GetReadableName() };
			}
			else
			{
				info.categories = new string[paths.Length];
				Array.Copy(paths, info.categories, paths.Length);
			}

			info.description = attribute.description;
			info.order = attribute.order;
		}

		static void AppendHotKeyDisplay(CommandInfo info, string hotKeyDisplay)
		{
			if (string.IsNullOrEmpty(hotKeyDisplay))
				return;

			info.name = AppendDisplaySuffix(info.name, hotKeyDisplay);
			if (!string.IsNullOrEmpty(info.fullPath))
			{
				info.fullPath = AppendDisplaySuffix(info.fullPath, hotKeyDisplay);
			}
		}

		static string AppendDisplaySuffix(string value, string suffix)
		{
			if (string.IsNullOrEmpty(value))
				return value;

			return string.Format("{0} <size=80%>({1})</size>", value, suffix);
		}
	}
}
