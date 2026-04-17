using System;
using System.Reflection;
using UnityEngine;

namespace MobileConsole.UI
{
	public class CommandDetailViewBuilder : ViewBuilder
	{
		class CustomButtonData
		{
			public MethodInfo methodInfo;
			public ActionAfterExecuted actionAfterExecuted;
		}

		readonly Command _command;

		public CommandDetailViewBuilder(Command command)
		{
			_command = command;
			actionButtonCallback = OnActionButton;
			actionButtonIcon = "execute";
			actionAfterExecuted = command.info.actionAfterExecuted;

			Node topNode = CreateCategory(_command.info.fullPath, "command");
			AddCommandFields(_command, topNode);

			// Add custom button (if have)
			foreach (var methodInfo in _command.info.customButtonInfos)
			{
				Node methodParent = topNode;
				ButtonAttribute attribute = methodInfo.GetCustomAttribute<ButtonAttribute>();
				if (!string.IsNullOrEmpty(attribute.category))
				{
					methodParent = CreateCategories(attribute.category);
				}

				string icon = !string.IsNullOrEmpty(attribute.icon) ? attribute.icon : "action";
				Node buttonNode = AddButton(methodInfo.Name.GetReadableName(), icon, OnCustomButtom, methodParent);
				buttonNode.data = new CustomButtonData()
				{
					methodInfo = methodInfo,
					actionAfterExecuted = GetCustomButtonActionAfterExecuted(methodInfo, attribute),
				};
			}
		}

		void OnActionButton()
		{
			try
			{
				_command.Execute();
				RecentCommandsHistory.Record(_command);
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}
		}

		void OnCustomButtom(GenericNodeView nodeView)
		{
			CustomButtonData buttonData = nodeView.data as CustomButtonData;
			if (buttonData != null && buttonData.methodInfo != null)
			{
				try
				{
					buttonData.methodInfo.Invoke(_command, null);
					RecentCommandsHistory.Record(_command);
					buttonData.actionAfterExecuted.Process();
				}
				catch (System.Exception e)
				{
					Debug.LogException(e);
				}
			}
		}

		ActionAfterExecuted GetCustomButtonActionAfterExecuted(MethodInfo methodInfo, ButtonAttribute attribute)
		{
			if (attribute == null || attribute.actionAfterExecutedRaw < 0)
			{
				return LogConsoleSettings.Instance.defaultCustomButtonActionAfterExecuted;
			}

			if (Enum.IsDefined(typeof(ActionAfterExecuted), attribute.actionAfterExecutedRaw))
			{
				return (ActionAfterExecuted)attribute.actionAfterExecutedRaw;
			}

			Debug.LogWarningFormat("Invalid actionAfterExecutedRaw [{0}] for button [{1}] in command [{2}]. Fallback to LogConsoleSettings.defaultCustomButtonActionAfterExecuted.",
				attribute.actionAfterExecutedRaw,
				methodInfo.Name,
				_command.GetType().Name);
			return LogConsoleSettings.Instance.defaultCustomButtonActionAfterExecuted;
		}

		public override void OnPrepareToShow()
		{
			base.OnPrepareToShow();
			_command.refreshUI += RefreshUI;
		}

		public override void OnPrepareToHide()
		{
			_command.refreshUI -= RefreshUI;
			base.OnPrepareToHide();
		}
	}
}
