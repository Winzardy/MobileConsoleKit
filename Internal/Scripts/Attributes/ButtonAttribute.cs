using System;

namespace MobileConsole
{
	[AttributeUsage(AttributeTargets.Method)]
	public class ButtonAttribute : Attribute
	{
		public ButtonAttribute()
		{
			actionAfterExecutedRaw = -1;
		}

		public ButtonAttribute(ActionAfterExecuted actionAfterExecuted)
		{
			actionAfterExecutedRaw = (int)actionAfterExecuted;
		}

		public string icon;
		public string category;
		public int actionAfterExecutedRaw;
	}
}
