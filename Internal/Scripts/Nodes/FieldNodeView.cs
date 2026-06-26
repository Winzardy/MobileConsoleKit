using System;
using System.Reflection;
using UnityEngine;


namespace MobileConsole.UI
{
	public class FieldNodeView : NodeView
	{
		const string ID_SLIDER_CELL = "SliderCell";
		const string ID_INPUT_CELL = "InputCell";
		const string ID_DROPDOWN_CELL = "DropdownCell";
		const string ID_CHECKBOX_CELL = "CheckboxCell";

		Command _command;
		VariableInfo _variableInfo;
		BaseCellControl _cellControl;
		string _cellIdentifier;

		public FieldNodeView(Command command, VariableInfo variableInfo)
		{
			_command = command;
			_variableInfo = variableInfo;
			ParseCellIdentifier();
			CreateCellControl();
		}
		
		public override ScrollViewCell CreateCell(RecycleScrollView scrollView, AssetConfig config, int cellIndex)
		{
			BaseCell cell = (BaseCell)scrollView.CreateCell(_cellIdentifier);
			cell.SetText(DisplayText);
			cell.SetHeaderOffset(LogConsoleSettings.GetTreeViewOffsetByLevel(level));
			cell.SetBackgroundColor(LogConsoleSettings.GetCellColor(cellIndex));
			_cellControl.UpdateData(cell);
			return cell;
		}

		public override float CellSize()
		{
			if (_cellIdentifier != ID_DROPDOWN_CELL)
			{
				return base.CellSize();
			}

			DropdownAttribute dropdownAttr;
			if (_variableInfo.fieldInfo.HasAttribute<DropdownAttribute>(out dropdownAttr) && dropdownAttr.enableFiltering)
			{
				return 160.0f;
			}

			return base.CellSize();
		}

		protected void CreateCellControl()
		{
			switch (_cellIdentifier)
			{
				case ID_SLIDER_CELL:
					_cellControl = new SliderCellControl();
					break;
				case ID_INPUT_CELL:
					_cellControl = new InputCellControl();
					break;
				case ID_DROPDOWN_CELL:
					_cellControl = new DropdownCellControl();
					break;
				case ID_CHECKBOX_CELL:
					_cellControl = new CheckboxCellControl();
					break;
				default:
					break;
			}

			_cellControl.command = _command;
			_cellControl.variableInfo = _variableInfo;
		}

		void ParseCellIdentifier()
		{
			Type fieldType = _variableInfo.fieldInfo.FieldType;
			Type nullableType = Nullable.GetUnderlyingType(fieldType);

			if (_variableInfo.fieldInfo.IsNumericType())
			{
				if (_variableInfo.fieldInfo.HasAttribute<RangeAttribute>())
					_cellIdentifier = ID_SLIDER_CELL;
				else if (_variableInfo.fieldInfo.HasAttribute<DropdownAttribute>())
					_cellIdentifier = ID_DROPDOWN_CELL;
				else
					_cellIdentifier = ID_INPUT_CELL;
			}
			else if (fieldType == typeof(string))
			{
				if (_variableInfo.fieldInfo.HasAttribute<DropdownAttribute>())
					_cellIdentifier = ID_DROPDOWN_CELL;
				else
					_cellIdentifier = ID_INPUT_CELL;
			}
			else if (fieldType.IsEnum || (nullableType != null && nullableType.IsEnum))
			{
				_cellIdentifier = ID_DROPDOWN_CELL;
			}
			else if (nullableType == typeof(bool))
			{
				_cellIdentifier = ID_DROPDOWN_CELL;
			}
			else if (fieldType == typeof(bool))
			{
				_cellIdentifier = ID_CHECKBOX_CELL;
			}

			if (string.IsNullOrEmpty(_cellIdentifier))
				throw new System.Exception("Couldn't parse cell identifier, something must wrong");
		}
	}


	public abstract class BaseCellControl
	{
		public Command command;
		public VariableInfo variableInfo;

		public abstract void UpdateData(ScrollViewCell cell);

		protected void OnValueChanged()
		{
			// In case of game logic causes any exception, add try catch here to prevent log console corruption
			try
			{
				command.SaveVariabledInfo(variableInfo);
                command.OnValueChanged(variableInfo.fieldInfo.Name);

                if (variableInfo.callbackMethodInfo != null)
				{
					variableInfo.callbackMethodInfo.Invoke(command, null);
				}
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}
		}
	}

	public class CheckboxCellControl : BaseCellControl
	{
		BaseCheckboxCell _checkboxCell;

		public override void UpdateData(ScrollViewCell cell)
		{
			_checkboxCell = (BaseCheckboxCell)cell;
			_checkboxCell.OnValueChanged = OnValueChanged;
			_checkboxCell.SetToggle((bool)variableInfo.fieldInfo.GetValue(command));
		}

		void OnValueChanged(BaseCheckboxCell cell, bool enabled)
		{
			variableInfo.fieldInfo.SetValue(command, enabled);
			base.OnValueChanged();
		}
	}

	public class InputCellControl : BaseCellControl
	{
		BaseInputCell _inputCell;
		bool _isNumeric;
		
		public override void UpdateData(ScrollViewCell cell)
		{
			_isNumeric = variableInfo.fieldInfo.FieldType.IsNumericType();
			_inputCell = (BaseInputCell)cell;
			_inputCell.OnValueChanged = OnValueChanged;
			_inputCell.SetInput(GetFieldValueAsString());
			_inputCell.SetIsNumeric(_isNumeric);
		}

		string GetFieldValueAsString()
		{
			object fieldValue = variableInfo.fieldInfo.GetValue(command) ?? "";
			return fieldValue.ToString();
		}

		void OnValueChanged(BaseInputCell cell, string value)
		{
			if (string.IsNullOrEmpty(value) && _isNumeric)
			{
				_inputCell.SetInput(GetFieldValueAsString());
				return;
			}
			else
			{
				try
				{
					variableInfo.fieldInfo.SetValue(command, Convert.ChangeType(value, variableInfo.fieldInfo.FieldType));
					base.OnValueChanged();
				}
				catch
				{
					_inputCell.SetInput(GetFieldValueAsString());
				}
			}			
		}
	}

	public class DropdownCellControl : BaseCellControl
	{
		const string EMPTY_OPTIONS_PLACEHOLDER = "--no-elements--";
		const string NULL_OPTION_PLACEHOLDER = "not selected";

		static IDropdownField[] _presetDropdownFields = new IDropdownField[]
		{
			new EnumDropdownField(),
			new NullableBoolDropdownField(),
			new StringDropdownField(),
			new NumericDropdownField()
		};

		BaseDropdownCell _dropdownCell;
		IDropdownField _dropdownField;
		DropdownOption[] _options;
		bool _isNoElementsState;

		public override void UpdateData(ScrollViewCell cell)
		{
			_dropdownCell = (BaseDropdownCell)cell;
			_dropdownCell.OnValueChanged = OnValueChanged;
			_dropdownCell.SetFilterEnabled(IsFilteringEnabled());

			_dropdownField = null;
			foreach (var dropdownField in _presetDropdownFields)
			{
				if (dropdownField.TryParse(command, variableInfo, out _options))
				{
					_dropdownField = dropdownField;
					break;
				}
			}

			if (_dropdownField == null || _options == null || _options.Length == 0)
			{
				_isNoElementsState = true;
				_options = DropdownOption.FromNames(new[] { EMPTY_OPTIONS_PLACEHOLDER });
				_dropdownCell.SetOptions(_options);
				_dropdownCell.SetIndex(0);
				_dropdownCell.SetInteractable(false);

				if (variableInfo.fieldInfo.FieldType == typeof(string))
				{
					variableInfo.fieldInfo.SetValue(command, EMPTY_OPTIONS_PLACEHOLDER);
				}

				return;
			}

			_isNoElementsState = false;
			_dropdownCell.SetInteractable(true);

			// If the field doesn't have value yet, set it to the first element (0) in options
			int dropdownIndex = _dropdownField.GetDropdownIndex(command, variableInfo, _options);
			if (dropdownIndex == -1)
			{
                dropdownIndex = 0;
                _dropdownField.OnValueChanged(command, variableInfo, _options, dropdownIndex);
			}

			_dropdownCell.SetOptions(_options);
			_dropdownCell.SetIndex(dropdownIndex);
		}

		bool IsFilteringEnabled()
		{
			DropdownAttribute dropdownAttr;
			if (!variableInfo.fieldInfo.HasAttribute<DropdownAttribute>(out dropdownAttr))
			{
				return false;
			}

			return dropdownAttr.enableFiltering;
		}

		void OnValueChanged(BaseDropdownCell cell, int index)
		{
			if (_isNoElementsState || _dropdownField == null || _options == null || index < 0 || index >= _options.Length)
			{
				return;
			}

			_dropdownField.OnValueChanged(command, variableInfo, _options, index);
			base.OnValueChanged();
		}


		#region Dropdown Field
		abstract class IDropdownField
		{
			public abstract bool TryParse(Command command, VariableInfo variableInfo, out DropdownOption[] options);
			public abstract int GetDropdownIndex(Command command, VariableInfo variableInfo, DropdownOption[] options);
			public abstract void OnValueChanged(Command command, VariableInfo variableInfo, DropdownOption[] options, int index);
		}

		class EnumDropdownField : IDropdownField
		{
			public override bool TryParse(Command command, VariableInfo variableInfo, out DropdownOption[] options)
			{
				Type fieldType = variableInfo.fieldInfo.FieldType;
				Type nullableType = Nullable.GetUnderlyingType(fieldType);
				Type enumType = nullableType ?? fieldType;

				if (!enumType.IsEnum)
				{
					options = null;
					return false;
				}

				string[] optionNames = Enum.GetNames(enumType);
				if (nullableType != null)
				{
					Array.Resize(ref optionNames, optionNames.Length + 1);
					Array.Copy(optionNames, 0, optionNames, 1, optionNames.Length - 1);
					optionNames[0] = NULL_OPTION_PLACEHOLDER;
				}

				options = DropdownOption.FromNames(optionNames);
				return true;
			}

			public override int GetDropdownIndex(Command command, VariableInfo variableInfo, DropdownOption[] options)
			{
				Type nullableType = Nullable.GetUnderlyingType(variableInfo.fieldInfo.FieldType);
				object enumValue = variableInfo.fieldInfo.GetValue(command);
				if (nullableType != null && enumValue == null)
				{
					return 0;
				}

				Type enumType = nullableType ?? variableInfo.fieldInfo.FieldType;
				string enumName = Enum.GetName(enumType, enumValue);
				return IndexOfOptionName(options, enumName);
			}

			public override void OnValueChanged(Command command, VariableInfo variableInfo, DropdownOption[] options, int index)
			{
				Type nullableType = Nullable.GetUnderlyingType(variableInfo.fieldInfo.FieldType);
				if (nullableType != null && index == 0)
				{
					variableInfo.fieldInfo.SetValue(command, null);
					return;
				}

				Type enumType = nullableType ?? variableInfo.fieldInfo.FieldType;
				object enumValue = Enum.Parse(enumType, GetOptionName(options, index));
				variableInfo.fieldInfo.SetValue(command, enumValue);
			}
		}

		class NullableBoolDropdownField : IDropdownField
		{
			public override bool TryParse(Command command, VariableInfo variableInfo, out DropdownOption[] options)
			{
				if (Nullable.GetUnderlyingType(variableInfo.fieldInfo.FieldType) != typeof(bool))
				{
					options = null;
					return false;
				}

				options = DropdownOption.FromNames(new[] { NULL_OPTION_PLACEHOLDER, "True", "False" });
				return true;
			}

			public override int GetDropdownIndex(Command command, VariableInfo variableInfo, DropdownOption[] options)
			{
				object value = variableInfo.fieldInfo.GetValue(command);
				if (value == null)
				{
					return 0;
				}

				return (bool)value ? 1 : 2;
			}

			public override void OnValueChanged(Command command, VariableInfo variableInfo, DropdownOption[] options, int index)
			{
				variableInfo.fieldInfo.SetValue(command, index == 0 ? null : (object)(index == 1));
			}
		}

		class StringDropdownField : IDropdownField
		{
			public override bool TryParse(Command command, VariableInfo variableInfo, out DropdownOption[] options)
			{
				if (variableInfo.fieldInfo.FieldType != typeof(string))
				{
					options = null;
					return false;
				}

				DropdownAttribute dropdownAttr;
				if (!variableInfo.fieldInfo.HasAttribute<DropdownAttribute>(out dropdownAttr))
				{
					options = null;
					return false;
				}

				MethodInfo methodInfo = command.GetType().GetMethod(dropdownAttr.methodName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
				if (methodInfo == null)
				{
					throw new Exception("Could not found method name: " + dropdownAttr.methodName);
				}

				try
				{
					options = ParseStringDropdownOptions(methodInfo.Invoke(command, null), dropdownAttr.methodName);
				}
				catch (Exception ex)
				{
					throw new Exception("Could not retrieve options from method name: " + dropdownAttr.methodName, ex);
				}

				return true;
			}

			public override int GetDropdownIndex(Command command, VariableInfo variableInfo, DropdownOption[] options)
			{
				string strValue = (string)variableInfo.fieldInfo.GetValue(command);
				return IndexOfOptionName(options, strValue);
			}

			public override void OnValueChanged(Command command, VariableInfo variableInfo, DropdownOption[] options, int index)
			{
				variableInfo.fieldInfo.SetValue(command, GetOptionName(options, index));
			}
		}

		class NumericDropdownField : IDropdownField
		{
			public override bool TryParse(Command command, VariableInfo variableInfo, out DropdownOption[] options)
			{
				if (!variableInfo.fieldInfo.FieldType.IsNumericType())
				{
					options = null;
					return false;
				}

				DropdownAttribute dropdownAttr;
				if (!variableInfo.fieldInfo.HasAttribute<DropdownAttribute>(out dropdownAttr))
				{
					options = null;
					return false;
				}

				MethodInfo methodInfo = command.GetType().GetMethod(dropdownAttr.methodName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
				if (methodInfo == null)
				{
					throw new Exception("Could not found method name: " + dropdownAttr.methodName);
				}

				object rawOptions;
				try
				{
					rawOptions = methodInfo.Invoke(command, null);
				}
				catch
				{
					throw new Exception("Could not retrieve options from method name: " + dropdownAttr.methodName);
				}

				if (rawOptions == null)
				{
					options = null;
					return true;
				}

				DropdownOption[] dropdownOptions = rawOptions as DropdownOption[];
				if (dropdownOptions != null)
				{
					options = dropdownOptions;
					return true;
				}

				Array fieldOptions = rawOptions as Array;
				if (fieldOptions == null)
				{
					throw new Exception("Could not retrieve options from method name: " + dropdownAttr.methodName);
				}

				options = new DropdownOption[fieldOptions.Length];
				for (int i = 0; i < fieldOptions.Length; i++)
				{
					object option = fieldOptions.GetValue(i);
					options[i] = new DropdownOption(option != null ? option.ToString() : string.Empty);
				}

				return true;
			}

			public override int GetDropdownIndex(Command command, VariableInfo variableInfo, DropdownOption[] options)
			{
				return IndexOfOptionName(options, variableInfo.fieldInfo.GetValue(command).ToString());
			}

			public override void OnValueChanged(Command command, VariableInfo variableInfo, DropdownOption[] options, int index)
			{
				variableInfo.fieldInfo.SetValue(command, Convert.ChangeType(GetOptionName(options, index), variableInfo.fieldInfo.FieldType));
			}
		}

		static DropdownOption[] ParseStringDropdownOptions(object rawOptions, string methodName)
		{
			if (rawOptions == null)
			{
				return null;
			}

			string[] stringOptions = rawOptions as string[];
			if (stringOptions != null)
			{
				return DropdownOption.FromNames(stringOptions);
			}

			DropdownOption[] dropdownOptions = rawOptions as DropdownOption[];
			if (dropdownOptions != null)
			{
				return dropdownOptions;
			}

			throw new Exception("Could not retrieve options from method name: " + methodName);
		}

		static int IndexOfOptionName(DropdownOption[] options, string name)
		{
			if (options == null)
			{
				return -1;
			}

			for (int i = 0; i < options.Length; i++)
			{
				if (GetOptionName(options, i) == name)
				{
					return i;
				}
			}

			return -1;
		}

		static string GetOptionName(DropdownOption[] options, int index)
		{
			if (options == null || index < 0 || index >= options.Length || options[index] == null)
			{
				return string.Empty;
			}

			return options[index].name;
		}
		#endregion
	}

	public class SliderCellControl : BaseCellControl
	{
		BaseSliderCell _sliderCell;

		public override void UpdateData(ScrollViewCell cell)
		{
			_sliderCell = (BaseSliderCell)cell;
			_sliderCell.OnValueChanged = OnValueChanged;

			var rangeAttr = variableInfo.fieldInfo.GetCustomAttribute<RangeAttribute>(false);
			var relativeAttr = variableInfo.fieldInfo.GetCustomAttribute<RelativeSliderAttribute>(false);
			bool isWholeNumbers = variableInfo.fieldInfo.FieldType != typeof(float) && variableInfo.fieldInfo.FieldType != typeof(double);
			float value = (float)Convert.ChangeType(variableInfo.fieldInfo.GetValue(command), typeof(float));
			_sliderCell.UseRelativeSlider(relativeAttr != null);
			_sliderCell.SetConfig(rangeAttr.min, rangeAttr.max, isWholeNumbers);
			_sliderCell.SetValue(Mathf.Clamp(value, rangeAttr.min, rangeAttr.max));
		}

		void OnValueChanged(BaseSliderCell cell, float value)
		{
			variableInfo.fieldInfo.SetValue(command, Convert.ChangeType(value, variableInfo.fieldInfo.FieldType));
			base.OnValueChanged();
		}
	}
}
