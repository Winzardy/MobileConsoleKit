using System;
using System.Reflection;
using UnityEngine;

namespace MobileConsole.UI
{
	public class CommandDetailViewBuilder : ViewBuilder
	{
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
				buttonNode.data = methodInfo;
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
			MethodInfo methodInfo = (MethodInfo)nodeView.data;
			if (methodInfo != null)
			{
				try
				{
					methodInfo.Invoke(_command, null);
				}
				catch (System.Exception e)
				{
					Debug.LogException(e);
				}
			}
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
