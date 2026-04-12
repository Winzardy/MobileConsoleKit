using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace MobileConsole.UI
{
    public class DropdownCell : BaseDropdownCell
    {
#pragma warning disable 0649
		[SerializeField]
		TextMeshProUGUI _fieldName;
		
		[SerializeField]
		TMP_Dropdown _dropdown;

		[SerializeField]
		TMP_InputField _filterInputField;

		[SerializeField]
		GameObject _filterLabel;

		[SerializeField]
		RectTransform _dropdownRectTransform;

		[SerializeField]
		Image _backgroundImage;
#pragma warning restore 0649

		const string NO_MATCHES_PLACEHOLDER = "--no-matches--";

		bool _canDispatch;
		bool _baseInteractable = true;
		bool _filterEnabled;
		bool _hasVisibleOptions;
		string _filterQuery = string.Empty;
		string[] _options = Array.Empty<string>();
		int _selectedOriginalIndex = -1;
		readonly List<int> _visibleOptionIndexes = new List<int>();

		void Awake()
		{
			_canDispatch = true;
			_dropdown.onValueChanged.AddListener(OnDropdownValueChanged);

			if (_filterInputField != null)
			{
				_filterInputField.onValueChanged.AddListener(OnFilterValueChanged);
				_filterInputField.onEndEdit.AddListener(OnFilterValueChanged);
			}

			if (_dropdownRectTransform == null && _dropdown != null)
			{
				_dropdownRectTransform = _dropdown.GetComponent<RectTransform>();
			}
		}		

		public override void SetText(string text)
		{
			_fieldName.text = text;
		}

		public override void SetIndex(int index)
		{
			if (_options == null || _options.Length == 0)
			{
				_selectedOriginalIndex = -1;
				SyncSelectedIndex();
				return;
			}

			_selectedOriginalIndex = Mathf.Clamp(index, 0, _options.Length - 1);
			SyncSelectedIndex();
		}

		public override void SetOptions(string[] options)
		{
			_options = options ?? Array.Empty<string>();
			ApplyFilter(_filterQuery, false);
		}

		public override void SetInteractable(bool interactable)
		{
			_baseInteractable = interactable;
			UpdateDropdownInteractable();
		}

		public override void SetFilterEnabled(bool enabled)
		{
			_filterEnabled = enabled;
			_filterQuery = string.Empty;
			SetFilterTextWithoutNotify(string.Empty);
			ApplyFilter(_filterQuery, false);
			ApplyFilterLayout();
		}

		public override void SetHeaderOffset(float offset)
		{
			_fieldName.margin = new Vector4(offset, 0, 0, 0);
		}

		void OnDropdownValueChanged(int index)
		{
			if (!_canDispatch || !_hasVisibleOptions || index < 0 || index >= _visibleOptionIndexes.Count)
			{
				return;
			}

			_selectedOriginalIndex = _visibleOptionIndexes[index];
			base.OnValueChanged(this, _selectedOriginalIndex);
		}

		void OnFilterValueChanged(string query)
		{
			if (!_filterEnabled)
			{
				return;
			}

			_filterQuery = query ?? string.Empty;
			ApplyFilter(_filterQuery, true);
		}

		void ApplyFilter(string query, bool notifySelectionChange)
		{
			_visibleOptionIndexes.Clear();
			string[] terms = GetFilterTerms(_filterEnabled ? query : string.Empty);

			for (int i = 0; i < _options.Length; i++)
			{
				string option = _options[i] ?? string.Empty;
				if (IsMatch(option, terms))
				{
					_visibleOptionIndexes.Add(i);
				}
			}

			RebuildDropdownOptions();
			bool selectionChanged = SyncSelectedIndex();
			UpdateDropdownInteractable();

			if (notifySelectionChange && selectionChanged && _hasVisibleOptions)
			{
				base.OnValueChanged(this, _selectedOriginalIndex);
			}
		}

		void RebuildDropdownOptions()
		{
			_canDispatch = false;
			_dropdown.options.Clear();

			_hasVisibleOptions = _visibleOptionIndexes.Count > 0;
			if (!_hasVisibleOptions)
			{
				_dropdown.options.Add(new TMP_Dropdown.OptionData(NO_MATCHES_PLACEHOLDER));
			}
			else
			{
				for (int i = 0; i < _visibleOptionIndexes.Count; i++)
				{
					int optionIndex = _visibleOptionIndexes[i];
					_dropdown.options.Add(new TMP_Dropdown.OptionData(_options[optionIndex]));
				}
			}

			_canDispatch = true;
		}

		bool SyncSelectedIndex()
		{
			int previousSelectedIndex = _selectedOriginalIndex;
			_canDispatch = false;

			if (!_hasVisibleOptions)
			{
				_selectedOriginalIndex = -1;
				_dropdown.value = 0;
				_canDispatch = true;
				_dropdown.RefreshShownValue();
				return previousSelectedIndex != _selectedOriginalIndex;
			}

			int visibleIndex = _visibleOptionIndexes.IndexOf(_selectedOriginalIndex);
			if (visibleIndex < 0)
			{
				visibleIndex = 0;
			}
			
			_selectedOriginalIndex = _visibleOptionIndexes[visibleIndex];

			_dropdown.value = visibleIndex;
			_canDispatch = true;
			_dropdown.RefreshShownValue();
			return previousSelectedIndex != _selectedOriginalIndex;
		}

		void UpdateDropdownInteractable()
		{
			_dropdown.interactable = _baseInteractable && _hasVisibleOptions;
		}

		void SetFilterTextWithoutNotify(string text)
		{
			if (_filterInputField == null)
			{
				return;
			}

			_filterInputField.SetTextWithoutNotify(text);
		}

		void ApplyFilterLayout()
		{
			bool hasFilterInput = _filterInputField != null;
			if (hasFilterInput)
			{
				_filterInputField.gameObject.SetActive(_filterEnabled);
			}

			if (_filterLabel != null)
			{
				_filterLabel.SetActive(_filterEnabled && hasFilterInput);
			}

			if (_dropdownRectTransform == null)
			{
				return;
			}

			if (_filterEnabled && hasFilterInput)
			{
				_dropdownRectTransform.anchorMin = new Vector2(0.6f, 0.5f);
				_dropdownRectTransform.anchorMax = new Vector2(1.0f, 0.5f);
				_dropdownRectTransform.anchoredPosition = new Vector2(10.0f, -26.0f);
				_dropdownRectTransform.sizeDelta = new Vector2(-50.0f, 60.0f);
			}
			else
			{
				_dropdownRectTransform.anchorMin = new Vector2(0.6f, 0.5f);
				_dropdownRectTransform.anchorMax = new Vector2(1.0f, 0.5f);
				_dropdownRectTransform.anchoredPosition = new Vector2(10.0f, 0.0f);
				_dropdownRectTransform.sizeDelta = new Vector2(-50.0f, 60.0f);
			}
		}

		static string[] GetFilterTerms(string filter)
		{
			if (string.IsNullOrEmpty(filter))
			{
				return Array.Empty<string>();
			}

			return filter.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
		}

		static bool IsMatch(string option, string[] terms)
		{
			if (terms == null || terms.Length == 0)
			{
				return true;
			}

			for (int i = 0; i < terms.Length; i++)
			{
				if (option.IndexOf(terms[i], StringComparison.OrdinalIgnoreCase) < 0)
				{
					return false;
				}
			}

			return true;
		}
	}
}
