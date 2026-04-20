using System.Collections.Generic;
using UnityEngine;

namespace MobileConsole
{
	public static class CommandHotKeyDisplay
	{
		public static string Format(ExecutableCommandAttribute attribute)
		{
			if (attribute == null || attribute.hotKey == KeyCode.None)
				return string.Empty;

			var parts = new List<string>();
			if ((attribute.hotKeyModifier & HotKeyModifier.Control) != 0)
			{
				parts.Add("Ctrl");
			}

			if ((attribute.hotKeyModifier & HotKeyModifier.Shift) != 0)
			{
				parts.Add("Shift");
			}

			if ((attribute.hotKeyModifier & HotKeyModifier.Alt) != 0)
			{
				parts.Add("Alt");
			}

			parts.Add(GetKeyDisplayName(attribute.hotKey));
			return string.Join("+", parts);
		}

		static string GetKeyDisplayName(KeyCode keyCode)
		{
			switch (keyCode)
			{
				case KeyCode.LeftShift:
				case KeyCode.RightShift:
					return "Shift";
				case KeyCode.LeftControl:
				case KeyCode.RightControl:
					return "Ctrl";
				case KeyCode.LeftAlt:
				case KeyCode.RightAlt:
					return "Alt";
				default:
					return keyCode.ToString();
			}
		}
	}
}
