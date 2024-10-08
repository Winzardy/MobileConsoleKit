﻿namespace MobileConsole
{
	[ExecutableCommand(name = "System/Player Prefs Inspector")]
	public class PlayerPrefsCommand : Command
	{
		PlayerPrefsViewBuilder _viewBuilder;

		public override void Execute()
		{
			// Lazy init, reduce pressure on launch
			if (_viewBuilder == null)
				_viewBuilder = new PlayerPrefsViewBuilder();

			info.actionAfterExecuted = ActionAfterExecuted.DoNothing;
			LogConsole.PushSubView(_viewBuilder);
		}
	}
}
