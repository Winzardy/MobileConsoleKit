using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MobileConsole
{
	public class CommandHotKeyProcessor
	{
		class HotKeyCommandBinding
		{
			public Command command;
			public KeyCode hotKey;
			public HotKeyModifier hotKeyModifier;
			public HotKeyTrigger hotKeyTrigger;
		}

		readonly List<HotKeyCommandBinding> _hotKeyCommands = new List<HotKeyCommandBinding>();

		public void Clear()
		{
			_hotKeyCommands.Clear();
		}

		public void Register(Command command, ExecutableCommandAttribute attribute)
		{
			if (command == null || attribute == null)
				return;

			if (attribute.hotKey == KeyCode.None)
				return;

			if (attribute.hotKeyTrigger == HotKeyTrigger.None)
				return;

			_hotKeyCommands.Add(new HotKeyCommandBinding()
			{
				command = command,
				hotKey = attribute.hotKey,
				hotKeyModifier = attribute.hotKeyModifier,
				hotKeyTrigger = attribute.hotKeyTrigger,
			});
		}

		public void Process()
		{
#if ENABLE_LEGACY_INPUT_MANAGER
			if (_hotKeyCommands.Count == 0 || IsInputFieldFocused())
				return;

			foreach (var hotKeyCommand in _hotKeyCommands)
			{
				if (!AreModifiersPressed(hotKeyCommand.hotKeyModifier))
				{
					continue;
				}

				if (ShouldTrigger(hotKeyCommand.hotKeyTrigger, HotKeyTrigger.Down) &&
				    Input.GetKeyDown(hotKeyCommand.hotKey))
				{
					SafeExecuteHotKeyCallback(hotKeyCommand.command.OnHotKeyDown);
				}

				if (ShouldTrigger(hotKeyCommand.hotKeyTrigger, HotKeyTrigger.Up) &&
				    Input.GetKeyUp(hotKeyCommand.hotKey))
				{
					SafeExecuteHotKeyCallback(hotKeyCommand.command.OnHotKeyUp);
				}

				if (ShouldTrigger(hotKeyCommand.hotKeyTrigger, HotKeyTrigger.Hold) &&
				    Input.GetKey(hotKeyCommand.hotKey))
				{
					SafeExecuteHotKeyCallback(hotKeyCommand.command.OnHotKeyHold);
				}
			}
#endif
		}

#if ENABLE_LEGACY_INPUT_MANAGER
		static bool IsInputFieldFocused()
		{
			if (EventSystem.current == null || EventSystem.current.currentSelectedGameObject == null)
				return false;

			GameObject selectedObject = EventSystem.current.currentSelectedGameObject;
			return selectedObject.GetComponentInParent<TMP_InputField>() != null ||
			       selectedObject.GetComponentInParent<InputField>() != null;
		}

		static bool AreModifiersPressed(HotKeyModifier modifiers)
		{
			if ((modifiers & HotKeyModifier.Shift) != 0 &&
			    !IsAnyKeyPressed(KeyCode.LeftShift, KeyCode.RightShift))
			{
				return false;
			}

			if ((modifiers & HotKeyModifier.Control) != 0 &&
			    !IsAnyKeyPressed(KeyCode.LeftControl, KeyCode.RightControl))
			{
				return false;
			}

			if ((modifiers & HotKeyModifier.Alt) != 0 &&
			    !IsAnyKeyPressed(KeyCode.LeftAlt, KeyCode.RightAlt))
			{
				return false;
			}

			return true;
		}

		static bool IsAnyKeyPressed(KeyCode keyA, KeyCode keyB)
		{
			return Input.GetKey(keyA) || Input.GetKey(keyB);
		}

		static bool ShouldTrigger(HotKeyTrigger value, HotKeyTrigger trigger)
		{
			return (value & trigger) != 0;
		}

		static void SafeExecuteHotKeyCallback(Action callback)
		{
			try
			{
				callback();
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}
		}
#endif
	}
}
