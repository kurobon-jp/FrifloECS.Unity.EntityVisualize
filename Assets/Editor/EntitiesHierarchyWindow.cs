using System;
using System.Collections.Generic;
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
        /// The root items
        /// </summary>
        private readonly List<TreeViewItemData<Item>> _rootItems = new();

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
        [NonSerialized] private bool _isRefreshing;

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
        /// Creates the gui
        /// </summary>
        private void CreateGUI()
        {
            hideFlags = HideFlags.DontSave;
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

            _inspector = CreateInstance<EntityInspector>();
        }

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

        private void OnEntitySelected(int id)
        {
            _inspector?.Bind(_collector, id);
        }

        /// <summary>
        /// Updates this instance
        /// </summary>
        private void Update()
        {
            if (EntityVisualizer.EntityStores.Count == 0) return;
            if (_collector == null)
            {
                _collector = new EntityCollector();
                _toolbarMenu.menu.ClearItems();
                var status = DropdownMenuAction.Status.Checked;
                foreach (var pair in EntityVisualizer.EntityStores)
                {
                    _toolbarMenu.menu.AppendAction(pair.Key, _ => { OnSwitchEntityStore(pair.Value); }, status: status);
                    if (status != DropdownMenuAction.Status.Checked) continue;
                    OnSwitchEntityStore(pair.Value);
                    status = DropdownMenuAction.Status.Normal;
                }
            }

            if (_inspector == null || _isRefreshing) return;
            _treeView.SetRootItems(_rootItems);
            _treeView.RefreshItems();
            _isRefreshing = true;
            Task.Run(Refresh);
        }

        private void OnSwitchEntityStore(EntityStore entityStore)
        {
            _collector.Bind(entityStore);
        }

        /// <summary>
        /// Refreshes this instance
        /// </summary>
        private void Refresh()
        {
            try
            {
                var token = Application.exitCancellationToken;
                _rootItems.Clear();
                var entities = _collector.CollectEntities();
                foreach (var entity in entities.Values)
                {
                    token.ThrowIfCancellationRequested();
                    var item = new TreeViewItemData<Item>();
                    if (CreateTreeViewItemData(entity, ref item))
                    {
                        _rootItems.Add(item);
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                _isRefreshing = false;
            }
        }

        private bool CreateTreeViewItemData(Entity entity, ref TreeViewItemData<Item> itemData)
        {
            List<TreeViewItemData<Item>> children = null;
            if (entity.ChildCount > 0)
            {
                foreach (var childEntity in entity.ChildEntities)
                {
                    var child = new TreeViewItemData<Item>();
                    if (CreateTreeViewItemData(childEntity, ref child))
                    {
                        children ??= new List<TreeViewItemData<Item>>();
                        children.Add(child);
                    }
                }
            }

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