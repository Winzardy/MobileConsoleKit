using System;
using System.Collections.Generic;
using UnityEngine;

namespace MobileConsole.UI
{
	public class CommandViewBuilder : ViewBuilder
	{
		const string ClassName = "CommandView";
		const string FavoritesCategoryName = "Favorites";
		const string RecentCategoryName = "Recent";
		Dictionary<Command, CommandDetailViewBuilder> _commandDetailViewBuilders = new Dictionary<Command, CommandDetailViewBuilder>();
		readonly List<Command> _commands;

		public CommandViewBuilder(List<Command> commands)
		{
			_commands = commands;
			RecentCommandsHistory.OnChanged += OnRecentCommandsChanged;
			BuildTree();
		}

		void OnCommandSelected(GenericNodeView nodeView)
		{
			Command command = nodeView.data as Command;
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
						RecentCommandsHistory.Record(command);
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

		void OnRecentCommandsChanged()
		{
			BuildTree();
			Rebuild();
		}

		void BuildTree()
		{
			ClearNodes();

			CategoryNodeView favoriteCategory = CreateCategory(FavoritesCategoryName);
			foreach (var command in _commands)
			{
				if (!command.info.isFavorite)
					continue;

				string commandName = string.IsNullOrEmpty(command.info.fullPath) ? command.info.name : command.info.fullPath;
				GenericNodeView node = AddButton(commandName, "command", OnCommandSelected, favoriteCategory);
				node.data = command;
			}

			CategoryNodeView recentCategory = CreateCategory(RecentCategoryName);
			foreach (var recentCommand in RecentCommandsHistory.ResolveCommands(_commands))
			{
				GenericNodeView node = AddButton(recentCommand.info.fullPath, "command", OnCommandSelected, recentCategory);
				node.data = recentCommand;
			}

			foreach (var command in _commands)
			{
				CategoryNodeView currentNode = CreateCategories(command.info.categories);
				GenericNodeView node = AddButton(command.info.name, "command", OnCommandSelected, currentNode);
				node.data = command;
			}

			CategoryPlayerPrefs.LoadCategoryStates(_rootNode, ClassName);

			if (LogConsoleSettings.Instance.useCategoryColor)
			{
				NodeColor.AdjustIconColor(_rootNode);
				ApplySpecialCategoryCommandColors(FavoritesCategoryName);
				ApplySpecialCategoryCommandColors(RecentCategoryName);
			}

			SortTree();
		}

		void ApplySpecialCategoryCommandColors(string categoryName)
		{
			Node categoryNode = _rootNode.FindChildByName(categoryName);
			if (categoryNode == null)
				return;

			foreach (var child in categoryNode.children)
			{
				GenericNodeView commandNode = child as GenericNodeView;
				if (commandNode == null)
					continue;

				Command command = commandNode.data as Command;
				if (command == null || command.info == null)
					continue;

				if (command.info.categories == null || command.info.categories.Length == 0 || string.IsNullOrEmpty(command.info.categories[0]))
				{
					commandNode.iconColor = Color.white;
					continue;
				}

				commandNode.iconColor = TextColors.GetUniqueColor(command.info.categories[0], 0.4f, 1.0f);
			}
		}

		void SortTree()
		{
			_rootNode.children.Sort(CompareRootNode);
			foreach (var child in _rootNode.children)
			{
				SortNodeRecursive(child);
			}
		}

		void SortNodeRecursive(Node node)
		{
			if (node == null || node.children.Count == 0)
				return;

			if (node.parent == _rootNode && (node.name == FavoritesCategoryName || node.name == RecentCategoryName))
				return;

			node.children.Sort(DefaultCompareNode);
			foreach (var child in node.children)
			{
				SortNodeRecursive(child);
			}
		}

		int CompareRootNode(Node nodeA, Node nodeB)
		{
			int categoryOrderA = GetRootCategoryOrder(nodeA);
			int categoryOrderB = GetRootCategoryOrder(nodeB);
			if (categoryOrderA != categoryOrderB)
			{
				return categoryOrderA.CompareTo(categoryOrderB);
			}

			return DefaultCompareNode(nodeA, nodeB);
		}

		int GetRootCategoryOrder(Node node)
		{
			if (node.name == FavoritesCategoryName)
				return 0;

			if (node.name == RecentCategoryName)
				return 1;

			return 2;
		}
	}
}
