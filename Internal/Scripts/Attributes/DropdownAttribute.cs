using System;

namespace MobileConsole
{
	[AttributeUsage(AttributeTargets.Field)]
	public class DropdownAttribute : Attribute
	{
		public string methodName;
		public bool enableFiltering;

		public DropdownAttribute(string methodName)
			: this(methodName, false)
		{
		}

		public DropdownAttribute(string methodName, bool enableFiltering)
		{
			this.methodName = methodName;
			this.enableFiltering = enableFiltering;
		}
	}
}
