﻿namespace MobileConsole
{
	[ExecutableCommand(name = "System/Search Game Object")]
	public class SearchGameObjectCommand : Command
	{
		SearchGameObjectViewBuilder _viewBuilder;
		public override void Execute()
		{
			// Lazy init, reduce pressure on launch
			if (_viewBuilder == null)
				_viewBuilder = new SearchGameObjectViewBuilder();

			info.actionAfterExecuted = ActionAfterExecuted.DoNothing;
			LogConsole.PushSubView(_viewBuilder);
		}
	}
}
