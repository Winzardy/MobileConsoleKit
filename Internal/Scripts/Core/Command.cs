using System;

namespace MobileConsole
{
	public class Command
	{
		public CommandInfo info = new CommandInfo();
		public Action refreshUI;

		public Command()
		{
			info.actionAfterExecuted = LogConsoleSettings.Instance.defaultCommandActionAfterExecuted;
		}

		public virtual void InitDefaultVariableValue() {}

		public virtual void OnVariableValueLoaded() {}

		public virtual void Execute() {}

		public virtual void OnValueChanged(string varName) {}
	}
}
