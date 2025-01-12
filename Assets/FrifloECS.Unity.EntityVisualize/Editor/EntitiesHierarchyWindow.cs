using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Friflo.Engine.ECS;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace FrifloECS.Unity.EntityVisualize.Editor
{
    /// <summary>
    /// The entities hierarchy window class
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
        /// The inspector
        /// </summary>
        [NonSerialized] private EntityInspector _inspector;

        /// <summary>
        /// The collector
        /// </summary>
        [NonSerialized] private EntityCollector _collector;

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
        [NonSerialized] private List<TreeViewItemData<Item>> _rootItems;

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

        private void Reset()
        {
            EntityVisualizer.EntityStores.Clear();
            EntityVisualizer.OnRegistered -= OnStoreRegistered;
            EntityVisualizer.OnRegistered += OnStoreRegistered;
            _collector = null;
            _inspector = CreateInstance<EntityInspector>();
            _refreshState = RefreshState.Idle;
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
        }

        /// <summary>
        /// Ons the store registered using the specified name
        /// </summary>
        /// <param name="name">The name</param>
        /// <param name="store">The store</param>
        private void OnStoreRegistered(string name, EntityStore store)
        {
            var status = DropdownMenuAction.Status.Normal;
            if (_collector == null)
            {
                _collector = new EntityCollector();
                _collector.Bind(store);
                status = DropdownMenuAction.Status.Checked;
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
            _inspector.Bind(_collector, id);
        }

        /// <summary>
        /// Updates this instance
        /// </summary>
        private void Update()
        {
            if (EntityVisualizer.EntityStores.Count == 0 ||
                _refreshState == RefreshState.Refreshing && !Application.isPlaying) return;
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
                Task.Run(() => Refresh(token), token);
            }
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
        private void Refresh(CancellationToken token)
        {
            try
            {
                var rootItems = new List<TreeViewItemData<Item>>();
                var entities = _collector.CollectEntities();
                token.ThrowIfCancellationRequested();
                foreach (var entity in entities.Values)
                {
                    token.ThrowIfCancellationRequested();
                    var item = new TreeViewItemData<Item>();
                    if (CreateTreeViewItemData(entity, token, ref item))
                    {
                        rootItems.Add(item);
                    }
                }

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
        [InitializeOnEnterPlayMode]
        public static void ShowWindow()
        {
            GetWindow<EntitiesHierarchyWindow>("Entities Hierarchy").Reset();
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