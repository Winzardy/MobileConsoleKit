using System;
using UnityEngine;

namespace MobileConsole
{
	[Serializable]
	public class DropdownOption
	{
		public string name;
		public Sprite image;

		public DropdownOption(string name)
			: this(name, null)
		{
		}

		public DropdownOption(string name, Sprite image)
		{
			this.name = name;
			this.image = image;
		}

		public override string ToString()
		{
			return name ?? string.Empty;
		}

		public static DropdownOption[] FromNames(string[] names)
		{
			if (names == null)
			{
				return null;
			}

			DropdownOption[] options = new DropdownOption[names.Length];
			for (int i = 0; i < names.Length; i++)
			{
				options[i] = new DropdownOption(names[i]);
			}

			return options;
		}

		public static string[] GetNames(DropdownOption[] options)
		{
			if (options == null)
			{
				return null;
			}

			string[] names = new string[options.Length];
			for (int i = 0; i < options.Length; i++)
			{
				names[i] = options[i] != null ? options[i].name : string.Empty;
			}

			return names;
		}
	}
}
