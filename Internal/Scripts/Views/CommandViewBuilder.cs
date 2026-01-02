using System;
using System.Collections.Generic;
using UnityEngine;

namespace MobileConsole.UI
{
	public class CommandViewBuilder : ViewBuilder
	{
		const string ClassName = "CommandView";
		Dictionary<Command, CommandDetailViewBuilder> _commandDetailViewBuilders = new Dictionary<Command, CommandDetailViewBuilder>();
		readonly List<Command> _commands;

		public CommandViewBuilder(List<Command> commands)
		{
			_commands = commands;
			foreach (var command in _commands)
			{
				CategoryNodeView currentNode = CreateCategories(command.info.categories);
				AddButton(command.info.name, "command", OnCommandSelected, currentNode);
			}

			CategoryPlayerPrefs.LoadCategoryStates(_rootNode, ClassName);

			if (LogConsoleSettings.Instance.useCategoryColor)
				NodeColor.AdjustIconColor(_rootNode);

			_rootNode.Sort(DefaultCompareNode);
		}

		void OnCommandSelected(GenericNodeView nodeView)
		{
			string nodePath = nodeView.name;
			Node parent = nodeView.parent;
			while (parent != null)
			{
				if (parent.name != null)
					nodePath = $"{parent.name}/{nodePath}";
				parent = parent.parent;
			}

			Command command = _commands.Find(c => c.info.fullPath == nodePath);
			if (command != null)
			{
				if (command.info.IsComplex())
				{
					if (!_commandDetailViewBuilders.ContainsKey(command))
					{
						_commandDetailViewBuilders[command] = new CommandDetailViewBuilder(command);
					}

					LogConsole.PushSubView(_commandDetailViewBuilders[command]);
				}
				else
				{
					try
					{
						command.Execute();
					}
					catch (Exception e)
					{
						Debug.LogException(e);
					}

					command.info.actionAfterExecuted.Process();
				}
			}
		}

		protected override void OnCategoryToggled(GenericNodeView nodeView)
		{
			base.OnCategoryToggled(nodeView);
			CategoryPlayerPrefs.SaveCategoryState(nodeView, ClassName);
		}
	}
}
