// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.UI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UIElements;

    internal interface IGridItem
    {
        int Id { get; }
        Texture2D Icon { get; }
        string Label { get; }
    }

    internal class GridView<T> : VisualElement where T : class, IGridItem
    {
        private const float DefaultMinTileSize = 64;
        private const float DefaultThumbnailTileSizeThreshold = 80;
        private const float DefaultTileSize = 170;
        private const float DefaultMaxTileSize = 256;
        private const float DefaultThumbnailRatio = 3f / 2f;
        private const int DefaultRowHeight = 32;

        private static readonly Texture2D DefaultThumbnail = null;

        private List<T> selectedItems;
        private List<T> items;
        private Dictionary<int, VisualElement> idToElements;
        private VisualElement header;
        private VisualElement itemsContainer;
        private ScrollView itemsScrollView;

        private float m_SizeLevel;

        public float sizeLevel
        {
            get => m_SizeLevel;
            set
            {
                var wasListView = isListView;
                m_SizeLevel = value;
                if (wasListView != isListView)
                {
                    itemsContainer.EnableInClassList(Styles.gridViewItemsContainerList, isListView);
                    itemsContainer.EnableInClassList(Styles.gridViewItemsContainerGrid, !isListView);
                }

                UpdateItemsLayout();
                sizeLevelChanged?.Invoke(m_SizeLevel, isListView);
            }
        }

        public bool isListView => sizeLevel < MinTileSize;
        public bool thumbnailVisible => sizeLevel >= showThumbnailTileSizeThreshold;

        public event Action<float, bool> sizeLevelChanged;

        public string FilterString
        {
            get => filterString;
            set
            {
                filterString = value.Trim();
                foreach (var element in idToElements.Values)
                {
                    var isFiltered = IsFiltered(element.userData as T, filterString);
                    if (isFiltered && element.style.display == DisplayStyle.None)
                    {
                        element.style.display = DisplayStyle.Flex;
                    }
                    else if (!isFiltered && element.style.display != DisplayStyle.None)
                    {
                        element.style.display = DisplayStyle.None;
                    }
                }

                SetSelectedVisibleIndex(0, true);
            }
        }

        public bool multiSelection { get; set; }

        public bool wrapAroundKeyboardNavigation { get; set; }

        public float MinTileSize { get; }

        public float maxTileSize { get; protected set; }

        public float showThumbnailTileSizeThreshold { get; }

        public float listItemHeight { get; protected set; }

        public float aspectRatio { get; protected set; }

        public Texture2D defaultThumbnail { get; }

        public IEnumerable<T> Items
        {
            get => items;
            set => SetItems(value);
        }

        public event Action<IEnumerable<T>, IEnumerable<T>> onSelectionChanged;
        public event Action<IEnumerable<T>> onItemsActivated;

        private string filterString;

        public GridView()
        {
            AddToClassList(Styles.gridView);
            AddToClassList(Styles.classTemplatesContainer);
            aspectRatio = DefaultThumbnailRatio;
            listItemHeight = DefaultRowHeight;
            MinTileSize = DefaultMinTileSize;
            maxTileSize = DefaultMaxTileSize;
            showThumbnailTileSizeThreshold = DefaultThumbnailTileSizeThreshold;
            defaultThumbnail = DefaultThumbnail;

            itemsScrollView = new ScrollView();
            itemsScrollView.AddToClassList(Styles.gridViewItemsScrollView);
            itemsScrollView.RegisterCallback<MouseDownEvent>(evt => HandleSelect(evt, null));
            itemsScrollView.focusable = true;
            itemsScrollView.RegisterCallback<KeyDownEvent>(OnKeyDown);
            Add(itemsScrollView);

            itemsContainer = new VisualElement();
            itemsContainer.AddToClassList(Styles.gridViewItems);
            itemsScrollView.Add(itemsContainer);

            sizeLevel = DefaultTileSize;
            wrapAroundKeyboardNavigation = true;

            EditorApplication.delayCall += () =>
            {
                SetFocus();
            };
        }

        private void SetItems(IEnumerable<T> newItems)
        {
            items = newItems.ToList();
            idToElements = new Dictionary<int, VisualElement>();
            selectedItems = new List<T>();

            RefreshItemElements();
            if (FilterString != null && !FilterString.Equals(string.Empty))
            {
                FilterString = FilterString;
            }
        }

        private void SetFocus()
        {
            itemsScrollView.tabIndex = 0;
            itemsScrollView.Focus();
        }

        private bool IsSelected(T item)
        {
            return selectedItems.Contains(item);
        }

        public void SetSelection(T itemToSelect)
        {
            SetSelection(new[]
            {
                itemToSelect,
            });
        }

        private void SetSelection(IEnumerable<T> itemToSelect)
        {
            if (selectedItems.Count > 0)
            {
                // Unselect currently selected item:
                foreach (var toUnselectElement in selectedItems.Select(item => idToElements[item.Id]))
                {
                    toUnselectElement.RemoveFromClassList(Styles.selected);
                }
            }

            var oldSelection = selectedItems.ToList();
            selectedItems = itemToSelect.ToList();

            if (itemToSelect.Any())
            {
                // Select new item
                var toSelectElements = itemToSelect.Select(item => idToElements[item.Id]);
                foreach (var toSelectElement in toSelectElements)
                {
                    toSelectElement.AddToClassList(Styles.selected);
                }
            }

            onSelectionChanged?.Invoke(oldSelection, itemToSelect);
        }

        private void ClearSelection()
        {
            SetSelection(new T[0]);
        }

        private void SelectAll()
        {
            SetSelection(items);
        }

        public void SetSelection(int idToSelect)
        {
            SetSelection(IdToItem(idToSelect));
        }

        public void SetSelection(IEnumerable<int> idToSelect)
        {
            SetSelection(idToSelect.Select(IdToItem));
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt == null)
            {
                return;
            }

            var shouldStopPropagation = true;
            VisualElement elementSelected = null;
            switch (evt.keyCode)
            {
                case KeyCode.UpArrow:
                    elementSelected = NavigateToPreviousItem();
                    break;
                case KeyCode.DownArrow:
                    elementSelected = NavigateToNextItem();
                    break;
                case KeyCode.RightArrow:
                    if (!isListView)
                    {
                        elementSelected = NavigateToNextItem();
                    }

                    break;
                case KeyCode.LeftArrow:
                    if (!isListView)
                    {
                        elementSelected = NavigateToPreviousItem();
                    }

                    break;
                case KeyCode.Tab:
                    elementSelected = NavigateToNextItem();
                    break;
                case KeyCode.Home:
                    elementSelected = SetSelectedVisibleIndex(0, true);
                    break;
                case KeyCode.End:
                    elementSelected = SetSelectedVisibleIndex(items.Count - 1, false);
                    break;
                case KeyCode.Return:
                    onItemsActivated?.Invoke(selectedItems);
                    break;
                case KeyCode.A:
                    if (evt.actionKey && multiSelection)
                    {
                        SelectAll();
                    }

                    break;
                case KeyCode.Escape:
                    ClearSelection();
                    break;
                default:
                    shouldStopPropagation = false;
                    break;
            }

            if (shouldStopPropagation)
            {
                evt.StopPropagation();
            }

            if (elementSelected != null)
            {
                itemsScrollView.ScrollTo(elementSelected);
            }
        }

        private T IdToItem(int id)
        {
            return idToElements[id].userData as T;
        }

        private void RefreshItemElements()
        {
            itemsContainer.Clear();
            foreach (var item in items)
            {
                var element = CreateItemElement(item);
                idToElements[item.Id] = element;
                itemsContainer.Add(element);
            }

            UpdateItemsLayout();
        }

        private int GetSelectedVisibleIndex()
        {
            var lastSelectedItem = selectedItems.LastOrDefault();
            var lastSelectedElement = lastSelectedItem == null ? null : idToElements[lastSelectedItem.Id];
            var selectedIndex = lastSelectedElement == null || lastSelectedElement.style.display == DisplayStyle.None
                ? -1
                : itemsContainer.IndexOf(lastSelectedElement);
            return selectedIndex;
        }

        private VisualElement SetSelectedVisibleIndex(int index, bool forward)
        {
            if (index >= itemsContainer.childCount)
            {
                index = itemsContainer.childCount - 1;
            }

            if (index == -1)
            {
                ClearSelection();
                return null;
            }

            var elementToSelect = itemsContainer.ElementAt(index);
            if (forward)
            {
                while (itemsContainer.ElementAt(index).style.display == DisplayStyle.None &&
                       ++index < itemsContainer.childCount)
                {
                    elementToSelect = itemsContainer.ElementAt(index);
                }
            }
            else
            {
                while (elementToSelect.style.display == DisplayStyle.None && --index >= 0)
                {
                    elementToSelect = itemsContainer.ElementAt(index);
                }
            }

            if (elementToSelect.style.display == DisplayStyle.None)
            {
                return null;
            }

            var itemToSelect = elementToSelect.userData as T;
            if (IsSelected(itemToSelect) && selectedItems.Count == 1)
            {
                return elementToSelect;
            }

            SetSelection(itemToSelect);
            return elementToSelect;
        }

        private void AddToSelection(T item)
        {
            var oldSelection = selectedItems.ToList();
            selectedItems.Add(item);
            idToElements[item.Id].AddToClassList(Styles.selected);
            onSelectionChanged?.Invoke(oldSelection, selectedItems);
        }

        private void RemoveFromSelection(T item)
        {
            var oldSelection = selectedItems.ToList();
            selectedItems.Remove(item);
            idToElements[item.Id].RemoveFromClassList(Styles.selected);
            onSelectionChanged?.Invoke(oldSelection, selectedItems);
        }

        private VisualElement NavigateToNextItem()
        {
            var selectedIndex = GetSelectedVisibleIndex();
            if (selectedIndex == -1)
            {
                selectedIndex = 0;
            }
            else if (selectedIndex + 1 < items.Count)
            {
                selectedIndex = selectedIndex + 1;
            }
            else if (wrapAroundKeyboardNavigation)
            {
                selectedIndex = 0;
            }

            return SetSelectedVisibleIndex(selectedIndex, true);
        }

        private VisualElement NavigateToPreviousItem()
        {
            var selectedIndex = GetSelectedVisibleIndex();
            if (selectedIndex == -1)
            {
                selectedIndex = 0;
            }
            else if (selectedIndex > 0)
            {
                selectedIndex = selectedIndex - 1;
            }
            else if (wrapAroundKeyboardNavigation)
            {
                selectedIndex = items.Count - 1;
            }

            return SetSelectedVisibleIndex(selectedIndex, false);
        }

        private static bool IsFiltered(T item, string filter)
        {
            if (string.IsNullOrEmpty(filter))
            {
                return true;
            }

            return item.Label.IndexOf(filter, StringComparison.InvariantCultureIgnoreCase) != -1;
        }

        private VisualElement CreateItemElement(T item)
        {
            var element = new VisualElement();
            element.AddToClassList(Styles.gridViewItemElement);
            element.userData = item;

            var icon = new VisualElement();
            icon.AddToClassList(Styles.gridViewItemIcon);
            if (item.Icon != null)
            {
                icon.style.backgroundImage = item.Icon;
            }

            element.Add(icon);

            var label = new Label(item.Label);
            label.AddToClassList(Styles.gridViewItemLabel);
            element.Add(label);

            element.RegisterCallback<MouseDownEvent>(evt => HandleSelect(evt, element));

            return element;
        }

        private void HandleSelect(MouseDownEvent evt, VisualElement clicked)
        {
            if (clicked == null)
            {
                ClearSelection();
                return;
            }

            if (evt.button != 0)
            {
                return;
            }

            var item = clicked.userData as T;

            if (evt.clickCount == 1)
            {
                if (multiSelection)
                {
                    if (evt.ctrlKey)
                    {
                        if (IsSelected(item))
                        {
                            RemoveFromSelection(item);
                        }
                        else
                        {
                            AddToSelection(item);
                        }
                    }
                    else if (!IsSelected(item))
                    {
                        SetSelection(new[]
                        {
                            item,
                        });
                    }
                }
                else
                {
                    if (IsSelected(item))
                    {
                        if (evt.ctrlKey)
                        {
                            ClearSelection();
                        }
                    }
                    else
                    {
                        SetSelection(new[]
                        {
                            item,
                        });
                    }
                }
            }
            else if (evt.clickCount == 2 && selectedItems.Count > 0)
            {
                onItemsActivated?.Invoke(selectedItems);
            }

            evt.StopPropagation();
        }

        private void UpdateItemsLayout()
        {
            var allItemElements = itemsContainer.Children();
            foreach (var element in allItemElements)
            {
                var item = element.userData as T;
                if (item == null)
                {
                    continue;
                }

                var icon = element.Q(null, Styles.gridViewItemIcon);

                if (isListView)
                {
                    element.style.height = listItemHeight;
                    element.style.width = Length.Percent(100);
                    icon.style.width = listItemHeight;
                }
                else
                {
                    element.style.width = sizeLevel * aspectRatio;
                    element.style.height = sizeLevel;
                    icon.style.width = StyleKeyword.Auto;
                }
            }
        }
    }
}
