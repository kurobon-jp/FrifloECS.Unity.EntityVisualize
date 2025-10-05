using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Friflo.Engine.ECS;
using FrifloECS.Unity.EntityVisualize.Editor.Models;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace FrifloECS.Unity.EntityVisualize.Editor
{
    /// <summary>
    /// The entities hierarchy window
    /// </summary>
    /// <seealso cref="EditorWindow"/>
    internal sealed class EntitiesHierarchyWindow : EditorWindow
    {
        /// <summary>
        /// The refresh state enum
        /// </summary>
        enum RefreshState
        {
            /// <summary>
            /// The idle refresh state
            /// </summary>
            Idle,

            /// <summary>
            /// The refreshing refresh state
            /// </summary>
            Refreshing,

            /// <summary>
            /// The complete refresh state
            /// </summary>
            Complete
        }

        /// <summary>
        /// The tree view
        /// </summary>
        [NonSerialized] private TreeView _treeView;

        /// <summary>
        /// The is refreshing
        /// </summary>
        [NonSerialized] private RefreshState _refreshState;

        /// <summary>
        /// The root items
        /// </summary>
        [NonSerialized] private IList<TreeViewItemData<Item>> _rootItems;

        /// <summary>
        /// The search text
        /// </summary>
        [SerializeField] private string _searchText;

        /// <summary>
        /// The toolbar menu
        /// </summary>
        private ToolbarMenu _toolbarMenu;

        /// <summary>
        /// The search field
        /// </summary>
        private ToolbarSearchField _searchField;

        /// <summary>
        /// The cancellation token source
        /// </summary>
        private CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// The selected id
        /// </summary>
        private int _selectedId;

        /// <summary>
        /// The collector
        /// </summary>
        private readonly EntityCollector _collector = new();

        /// <summary>
        /// Ons the enable
        /// </summary>
        void OnEnable()
        {
            EntityVisualizer.OnRegistered += OnStoreRegistered;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        /// <summary>
        /// Creates the gui
        /// </summary>
        private void CreateGUI()
        {
            _treeView = new TreeView
            {
                viewDataKey = "tree-view",
                focusable = true,
                makeItem = () =>
                {
                    var label = new Label();
                    var doubleClickable = new Clickable(OnDoubleClick);
                    doubleClickable.activators.Clear();
                    doubleClickable.activators.Add(new ManipulatorActivationFilter
                        { button = MouseButton.LeftMouse, clickCount = 2 });
                    label.AddManipulator(doubleClickable);
                    return label;
                }
            };
            _treeView.bindItem = (e, i) => e.Q<Label>().text = _treeView.GetItemDataForIndex<Item>(i).name;
            _treeView.selectionChanged += OnSelectionChanged;

            var toolbar = new Toolbar();
            _toolbarMenu = new ToolbarMenu();
            _toolbarMenu.text = "Entity Store";
            _toolbarMenu.variant = ToolbarMenu.Variant.Popup;
            toolbar.Add(_toolbarMenu);
            _searchField = new ToolbarSearchField();
            _searchField.RegisterValueChangedCallback(x => OnSearchTextChanged(x.newValue));
            _searchField.value = _searchText;
            toolbar.Add(_searchField);
            rootVisualElement.Add(toolbar);
            rootVisualElement.Add(_treeView);
            _refreshState = RefreshState.Idle;

            if (EditorApplication.isPlaying)
            {
                OnPlayEditor();
            }
        }

        /// <summary>
        /// Ons the play mode state changed using the specified state
        /// </summary>
        /// <param name="state">The state</param>
        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.EnteredPlayMode:
                    EditorApplication.delayCall += OnPlayEditor;
                    break;
                case PlayModeStateChange.ExitingPlayMode:
                    OnStopEditor();
                    EntityVisualizer.Clear();
                    break;
            }
        }

        /// <summary>
        /// Ons the play editor
        /// </summary>
        private void OnPlayEditor()
        {
            if (_toolbarMenu == null) return;
            _toolbarMenu.menu.ClearItems();
            foreach (var pair in EntityVisualizer.EntityStores)
            {
                OnStoreRegistered(pair.Key, pair.Value);
            }
        }

        /// <summary>
        /// Ons the store registered using the specified name
        /// </summary>
        /// <param name="name">The name</param>
        /// <param name="store">The store</param>
        private void OnStoreRegistered(string name, EntityStore store)
        {
            if (_toolbarMenu == null) return;
            var status = DropdownMenuAction.Status.Normal;
            if (_toolbarMenu.menu.MenuItems().Count == 0)
            {
                status = DropdownMenuAction.Status.Checked;
                _collector.Bind(store);
            }

            _toolbarMenu.menu.AppendAction(name, _ => { OnSwitchEntityStore(store); }, status: status);
 }

        /// <summary>
        /// Ons the double click
        /// </summary>
        private void OnDoubleClick()
        {
            var inspectorWindowType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.InspectorWindow");
            var inspectorWindow = GetWindow(inspectorWindowType);
            inspectorWindow.Focus();
        }

        /// <summary>
        /// Ons the search text changed using the specified text
        /// </summary>
        /// <param name="text">The text</param>
        private void OnSearchTextChanged(string text)
        {
            _searchText = text;
            _collector.IsDirty = true;
        }

        /// <summary>
        /// Ons the selection changed using the specified selections
        /// </summary>
        /// <param name="selections">The selections</param>
        private void OnSelectionChanged(IEnumerable<object> selections)
        {
            foreach (var selection in selections)
            {
                if (selection is not Item item) continue;
                OnEntitySelected(item.id);
                break;
            }
        }

        /// <summary>
        /// Ons the entity selected using the specified id
        /// </summary>
        /// <param name="id">The id</param>
        private void OnEntitySelected(int id)
        {
            _selectedId = id;
            Selection.activeObject = id == 0 || !EditorApplication.isPlaying ? null : this;
            EditorUtility.SetDirty(this);
        }

        /// <summary>
        /// Updates this instance
        /// </summary>
        private void Update()
        {
            if (EntityVisualizer.EntityStores.Count == 0 || !EditorApplication.isPlaying ||
                _refreshState == RefreshState.Refreshing) return;
            if (_refreshState == RefreshState.Complete)
            {
                _treeView.SetRootItems(_rootItems);
                _treeView.RefreshItems();
                _refreshState = RefreshState.Idle;
                return;
            }

            if (_collector.IsDirty)
            {
                _collector.IsDirty = false;
                _refreshState = RefreshState.Refreshing;
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource =
                    CancellationTokenSource.CreateLinkedTokenSource(Application.exitCancellationToken);
                var token = _cancellationTokenSource.Token;
                Task.Run(() => RefreshAsync(token), token);
            }
        }

        /// <summary>
        /// Ons the stop editor
        /// </summary>
        private void OnStopEditor()
        {
            _cancellationTokenSource?.Cancel();
            Selection.activeObject = null;
        }

        /// <summary>
        /// Ons the destroy
        /// </summary>
        private void OnDestroy()
        {
            OnStopEditor();
        }

        /// <summary>
        /// Ons the switch entity store using the specified entity store
        /// </summary>
        /// <param name="entityStore">The entity store</param>
        private void OnSwitchEntityStore(EntityStore entityStore)
        {
            _collector.Bind(entityStore);
        }

        /// <summary>
        /// Refreshes this instance
        /// </summary>
        private void RefreshAsync(CancellationToken token)
        {
            try
            {
                var entities = _collector.CollectEntities();
                var rootItems = new List<TreeViewItemData<Item>>();
                token.ThrowIfCancellationRequested();
                Parallel.For(0, entities.Count, i =>
                {
                    token.ThrowIfCancellationRequested();
                    var item = new TreeViewItemData<Item>();
                    if (!CreateTreeViewItemData(entities[i], token, ref item)) return;
                    lock (rootItems)
                    {
                        rootItems.Add(item);
                    }
                });

                rootItems.Sort((a, b) => a.id.CompareTo(b.id));
                _rootItems = rootItems;
                _refreshState = RefreshState.Complete;
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _refreshState = RefreshState.Idle;
                Debug.LogException(ex);
            }
        }

        /// <summary>
        /// Describes whether this instance create tree view item data
        /// </summary>
        /// <param name="entity">The entity</param>
        /// <param name="token">The token</param>
        /// <param name="itemData">The item data</param>
        /// <returns>The bool</returns>
        private bool CreateTreeViewItemData(Entity entity, CancellationToken token, ref TreeViewItemData<Item> itemData)
        {
            List<TreeViewItemData<Item>> children = null;
            if (entity is { IsNull: false, ChildCount: > 0 })
            {
                foreach (var childEntity in entity.ChildEntities)
                {
                    token.ThrowIfCancellationRequested();
                    var child = new TreeViewItemData<Item>();
                    if (CreateTreeViewItemData(childEntity, token, ref child))
                    {
                        children ??= new List<TreeViewItemData<Item>>();
                        children.Add(child);
                    }
                }
            }

            token.ThrowIfCancellationRequested();
            var entityName = $"{entity}";
            if (!string.IsNullOrEmpty(_searchText) && !entityName.Contains(_searchText) && children == null)
            {
                return false;
            }

            itemData = new TreeViewItemData<Item>(entity.Id,
                new Item { name = entityName, id = entity.Id }, children);
            return true;
        }

        /// <summary>
        /// Shows the window
        /// </summary>
        [MenuItem("Window/Friflo.ECS/Entities Hierarchy")]
        public static void ShowWindow()
        {
            GetWindow<EntitiesHierarchyWindow>("Entities Hierarchy");
        }

        /// <summary>
        /// Gets the entity info
        /// </summary>
        /// <returns>The entity info</returns>
        public EntityInfo GetSelectedEntityInfo()
        {
            return _collector.GetEntityInfo(_selectedId);
        }

        /// <summary>
        /// The item
        /// </summary>
        private struct Item
        {
            /// <summary>
            /// The name
            /// </summary>
            public string name;

            /// <summary>
            /// The id
            /// </summary>
            public int id;
        }
    }
}