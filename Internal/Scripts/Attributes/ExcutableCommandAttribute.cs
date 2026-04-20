using System;
using UnityEngine;

namespace MobileConsole
{
    [Flags]
    public enum HotKeyModifier
    {
        None = 0,
        Shift = 1 << 0,
        Control = 1 << 1,
        Alt = 1 << 2,
    }

    [Flags]
    public enum HotKeyTrigger
    {
        None = 0,
        Down = 1 << 0,
        Up = 1 << 1,
        Hold = 1 << 2,
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class ExecutableCommandAttribute : Attribute
    {
        public string name;
        public string description;
        public int order = 0;
        public bool isFavorite = false;
        public KeyCode hotKey = KeyCode.None;
        public HotKeyModifier hotKeyModifier = HotKeyModifier.None;
        public HotKeyTrigger hotKeyTrigger = HotKeyTrigger.Down;
    }
}
