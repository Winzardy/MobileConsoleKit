using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MobileConsole.UI
{
	public class GenericTreeView : BaseView, IRecycleScrollViewDelegate
	{
		[SerializeField]
		protected AssetConfig _config;

		[SerializeField]
		protected UIBridge _title;

		[SerializeField]
		protected GameObject _filterGroup;

		[SerializeField]
		protected UIBridge _inputFilter;

		[SerializeField]
		protected GameObject _backButton;

		[SerializeField]
		protected GameObject _actionButton;

		[SerializeField]
		[Tooltip("Hidden unless the view builder sets a second action")]
		protected GameObject _secondActionButton;

		[SerializeField]
		protected RecycleScrollView _scrollView;

		[SerializeField]
		protected Transform _scrollViewContent;

		Dictionary<string, ScrollViewCell> _resizableCells = new Dictionary<string, ScrollViewCell>();

		List<Node> _filterNodes = new List<Node>();
		ViewBuilder _viewBuilder;
		ActionButtonFeedbackAnimator _actionAnimator;
		ActionButtonFeedbackAnimator _secondActionAnimator;
		float _defaultTitleWidth;
		float _defaultTitlePositionX;
		protected string _filterString;
		bool _isInitialized = false;
		bool _isPreparingView = false;

		public void Show(ViewBuilder builder)
		{
			if (builder == null)
			{
				throw new System.Exception("View Builder is null");
			}

			base.Show();

			if (!_isInitialized)
			{
				_isInitialized = true;
				_scrollView.AddCellTemplates(_config.cellTemplates);
				_scrollView.SetDelegate(this);

				_actionAnimator = new ActionButtonFeedbackAnimator(this);
				_secondActionAnimator = new ActionButtonFeedbackAnimator(this);

				RectTransform titleTransform = (RectTransform)_title.transform;
				_defaultTitleWidth = titleTransform.sizeDelta.x;
				_defaultTitlePositionX = titleTransform.anchoredPosition.x;
			}

			ClearOldBuilder();
			SetupNewBuilder(builder);
		}

		public override void Hide()
		{
			base.Hide();
			ClearOldBuilder();
		}

		void ClearOldBuilder()
		{
			_actionAnimator?.Stop();
			_secondActionAnimator?.Stop();

			if (_viewBuilder != null)
			{
				_viewBuilder.OnRequireUpdateUI = null;
				_viewBuilder.scrollViewPosition = _scrollView.ScrollPosition;
				_viewBuilder.filterString = _filterString;
				_viewBuilder.OnPrepareToHide();
			}

			_viewBuilder = null;
		}

		void SetupNewBuilder(ViewBuilder builder)
		{
			// Prevent input filter callback while we set its value
			_isPreparingView = true;

            _viewBuilder = builder;
            _viewBuilder.OnRequireUpdateUI = OnUpdateView;
            _viewBuilder.OnPrepareToShow();

			// Update title
			bool hasTitle = !string.IsNullOrEmpty(_viewBuilder.title);
			_title.gameObject.SetActive(hasTitle);
			if (hasTitle)
			{
				_title.text = _viewBuilder.title;
			}

			// Update filter string
			_filterString = _viewBuilder.filterString;
			_inputFilter.input = _filterString;
			_filterGroup.SetActive(!hasTitle);

			// Update buttons
			_backButton.SetActive(true);
			UpdateActionButtons();

			// Rebuild the whole tree
            _viewBuilder.Rebuild();

			// Update scroll view
            if (!_viewBuilder.saveScrollViewPosition)
                _scrollView.MoveViewToTop();
            else
                _scrollView.ScrollPosition = _viewBuilder.scrollViewPosition;

			_isPreparingView = false;
		}

		void OnUpdateView(ViewBuilder.UpdateUIType updateType)
		{
			switch (updateType)
			{
				case ViewBuilder.UpdateUIType.DataChanged:
				{
					_scrollView.ReloadData();
					break;
				}
				case ViewBuilder.UpdateUIType.CellVisibleChanged:
				case ViewBuilder.UpdateUIType.TreeChanged:
				{
					UpdateTree();
					break;
				}
				default:
					break;
			}
		}

		void UpdateTree()
		{
			RootNode rootNode = _viewBuilder.GetRootNode();

			_filterNodes.Clear();
			if (string.IsNullOrEmpty(_filterString))
			{
				_filterNodes.AddRange(rootNode.FlattenedVisibleChilds());
			}
			else
			{
				HashSet<Node> passFilterNodes = new HashSet<Node>();
				foreach (var node in rootNode.FlattenChilds())
				{
					if (node.name.IndexOf(_filterString, StringComparison.OrdinalIgnoreCase) >= 0)
					{
						if (passFilterNodes.Contains(node))
							continue;

						foreach (var n in node.GetBranch())
						{
							if (n.isVisible)
								passFilterNodes.Add(n);
						}

						foreach (var n in node.GetAllChildren())
						{
							if (n.isVisible)
								passFilterNodes.Add(n);
						}
					}
				}

				foreach (var node in passFilterNodes)
				{
					if (node.level == -1)
					{
						continue;
					}

					_filterNodes.Add(node);
				}
			}

			_scrollView.ReloadData();
		}

		public void OnFilterValueChanged(string filterString)
		{
			// Don't process if view is being prepared
			if (_isPreparingView)
				return;

			_filterString = filterString;
			UpdateTree();
		}

		public void OnClosed()
		{
			LogConsole.CloseAllSubView();
			LogConsole.ToggleShow();
		}

		public void OnBack()
		{
			LogConsole.PopSubView();
		}

		public void OnAction()
		{
			if (_viewBuilder == null)
				return;

			InvokeAction(_viewBuilder.actionButtonCallback, _viewBuilder.actionAfterExecuted, _actionAnimator);
		}

		public void OnSecondAction()
		{
			if (_viewBuilder == null)
				return;

			InvokeAction(_viewBuilder.secondActionButtonCallback, _viewBuilder.secondActionAfterExecuted, _secondActionAnimator);
		}

		void InvokeAction(ViewBuilder.Callback callback, ActionAfterExecuted actionAfterExecuted, ActionButtonFeedbackAnimator animator)
		{
			if (callback == null)
				return;

			try
			{
				callback();
				actionAfterExecuted.Process();
				animator.Play();
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}
		}

		void UpdateActionButtons()
		{
			bool hasSecondAction = _viewBuilder.secondActionButtonCallback != null;
			SetupActionButton(_actionButton, _actionAnimator, _viewBuilder.actionButtonCallback != null, _viewBuilder.actionButtonIcon);
			SetupActionButton(_secondActionButton, _secondActionAnimator, hasSecondAction, _viewBuilder.secondActionButtonIcon);

			// Shrink the title so a long one does not run under the second button
			RectTransform titleTransform = (RectTransform)_title.transform;
			float reserved = hasSecondAction && _secondActionButton != null
				? ((RectTransform)_secondActionButton.transform).rect.width
				: 0f;
			titleTransform.sizeDelta = new Vector2(_defaultTitleWidth - reserved, titleTransform.sizeDelta.y);
			titleTransform.anchoredPosition = new Vector2(_defaultTitlePositionX - reserved * 0.5f, titleTransform.anchoredPosition.y);
		}

		void SetupActionButton(GameObject buttonObject, ActionButtonFeedbackAnimator animator, bool isVisible, string icon)
		{
			if (buttonObject == null)
				return;

			buttonObject.SetActive(isVisible);

			Image image = isVisible ? GetActionButtonImage(buttonObject) : null;
			if (image != null)
			{
				AssetConfig.SpriteInfo spriteInfo = _config.GetSpriteInfo(icon);
				if (spriteInfo != null)
				{
					image.sprite = spriteInfo.sprite;
					image.color = spriteInfo.color;
				}
			}

			animator.Setup(image, image != null ? image.color : Color.white);
		}

		Image GetActionButtonImage(GameObject buttonObject)
		{
			Button button = buttonObject.GetComponent<Button>();
			return button != null && button.targetGraphic is Image targetImage
				? targetImage
				: buttonObject.GetComponentInChildren<Image>();
		}

		public void OnExpandAll()
		{
			RootNode rootNode = _viewBuilder.GetRootNode();
			rootNode.ExpandAllChild(true);
			rootNode.RebuildFlattenVisibleChilds();
			OnUpdateView(ViewBuilder.UpdateUIType.CellVisibleChanged);
		}

		public void OnCollapseAll()
		{
			RootNode rootNode = _viewBuilder.GetRootNode();
			rootNode.ExpandAllChild(false);
			rootNode.RebuildFlattenVisibleChilds();
			OnUpdateView(ViewBuilder.UpdateUIType.CellVisibleChanged);
		}

		public ScrollViewCell ScrollCellCreated(int cellIndex)
		{
			NodeView nodeView = (NodeView)_filterNodes[cellIndex];
			return nodeView.CreateCell(_scrollView, _config, cellIndex);
		}

		public void ScrollCellSelected(int cellIndex)
		{
			NodeView nodeView = (NodeView)_filterNodes[cellIndex];
			nodeView.CellSelected();
		}

		public void ScrollCellWillDisplay(ScrollViewCell cell)
		{
		}

		public float ScrollCellSize(int cellIndex)
		{
			NodeView nodeView = (NodeView)_filterNodes[cellIndex];
			if (nodeView.resizable)
			{
				string cellIdentifier = nodeView.CellIdentifier();
				if (!_resizableCells.ContainsKey(cellIdentifier))
				{
					ScrollViewCell cellTemplate = _config.cellTemplates.Find(c => c.identifier == cellIdentifier);
					ScrollViewCell cell = Instantiate(cellTemplate, _scrollViewContent);
					_resizableCells[cellIdentifier] = cell;

					LayoutRebuilder.ForceRebuildLayoutImmediate(cell.rectTransform);
					cell.gameObject.SetActive(false);
				}

				return Mathf.Max(nodeView.CellSize(_resizableCells[cellIdentifier]), 120);
			}
			else
			{
				return nodeView.CellSize();
			}
		}

		public int ScrollCellCount()
		{
			return _filterNodes.Count;
		}
	}

	class ActionButtonFeedbackAnimator
	{
		static readonly Color FeedbackColor = new Color(0.2f, 0.9f, 0.35f, 1f);
		const float FadeInDuration = 0.15f;
		const float HoldDuration = 0.2f;
		const float FadeOutDuration = 0.15f;

		readonly GenericTreeView _owner;
		Image _image;
		Color _normalColor = Color.white;
		Coroutine _routine;

		public ActionButtonFeedbackAnimator(GenericTreeView owner)
		{
			_owner = owner;
		}

		public void Setup(Image image, Color normalColor)
		{
			Stop();
			_image = image;
			_normalColor = normalColor;
		}

		public void Play()
		{
			Stop();

			if (_image == null || !_owner.isActiveAndEnabled || !_image.gameObject.activeInHierarchy)
			{
				return;
			}

			_routine = _owner.StartCoroutine(Animate());
		}

		public void Stop()
		{
			if (_routine != null)
			{
				_owner.StopCoroutine(_routine);
				_routine = null;
			}

			if (_image != null)
			{
				_image.color = _normalColor;
			}
		}

		IEnumerator Animate()
		{
			yield return LerpColor(_normalColor, FeedbackColor, FadeInDuration);
			yield return new WaitForSecondsRealtime(HoldDuration);
			yield return LerpColor(FeedbackColor, _normalColor, FadeOutDuration);

			_image.color = _normalColor;
			_routine = null;
		}

		IEnumerator LerpColor(Color from, Color to, float duration)
		{
			float elapsed = 0f;
			while (elapsed < duration)
			{
				elapsed += Time.unscaledDeltaTime;
				float t = Mathf.Clamp01(elapsed / duration);
				_image.color = Color.Lerp(from, to, t);
				yield return null;
			}

			_image.color = to;
		}
	}
}
