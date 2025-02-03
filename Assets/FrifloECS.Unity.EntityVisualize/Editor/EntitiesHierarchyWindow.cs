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
                Task.Run(() => Refresh(token), token);
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
        public static void ShowWindow()
        {
            GetWindow<EntitiesHierarchyWindow>("Entities Hierarchy");
        }

        /// <summary>
        /// Gets the entity info
        /// </summary>
        /// <returns>The entity info</returns>
        private EntityInfo GetEntityInfo()
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

        /// <summary>
        /// The entities hierarchy window editor class
        /// </summary>
        /// <seealso cref="UnityEditor.Editor"/>
        [CustomEditor(typeof(EntitiesHierarchyWindow))]
        public class EntitiesHierarchyWindowEditor : UnityEditor.Editor
        {
            /// <summary>
            /// The window
            /// </summary>
            private EntitiesHierarchyWindow _window;

            /// <summary>
            /// The entity info
            /// </summary>
            private EntityInfo _entityInfo;

            /// <summary>
            /// Ons the header gui
            /// </summary>
            protected override void OnHeaderGUI()
            {
                _window = (EntitiesHierarchyWindow)target;
                _entityInfo = _window.GetEntityInfo();
                if (_entityInfo == null) return;
                var style = new GUIStyle(EditorStyles.boldLabel);
                style.fontSize = 24;
                style.padding = new RectOffset(8, 8, 8, 8);
                GUILayout.Label($"{_entityInfo}", style);
            }

            /// <summary>
            /// Ons the inspector gui
            /// </summary>
            public override void OnInspectorGUI()
            {
                if (_entityInfo == null) return;
                var defaultColor = GUI.backgroundColor;
                for (var i = 0; i < _entityInfo.Components.Count; i++)
                {
                    var componentInfo = _entityInfo.Components[i];
                    object component;
                    try
                    {
                        component = componentInfo.Component;
                    }
                    catch
                    {
                        continue;
                    }

                    GUI.backgroundColor = GetRainbowColor(i);
                    componentInfo.Foldout =
                        EditorGUILayout.BeginFoldoutHeaderGroup(componentInfo.Foldout, componentInfo.ComponentName);
                    GUI.backgroundColor = defaultColor;
                    if (componentInfo.Foldout)
                    {
                        foreach (var field in component.GetType().GetFields())
                        {
                            var value = field.GetValue(component);
                            if (field.FieldType == typeof(string))
                            {
                                EditorGUILayout.LabelField(field.Name, (string)value);
                            }
                            else if (field.FieldType == typeof(bool))
                            {
                                EditorGUILayout.Toggle(field.Name, (bool)value);
                            }
                            else if (field.FieldType == typeof(byte))
                            {
                                EditorGUILayout.IntField(field.Name, (int)value);
                            }
                            else if (field.FieldType == typeof(short))
                            {
                                EditorGUILayout.IntField(field.Name, (int)value);
                            }
                            else if (field.FieldType == typeof(ushort))
                            {
                                EditorGUILayout.IntField(field.Name, Convert.ToInt16((ushort)value));
                            }
                            else if (field.FieldType == typeof(int))
                            {
                                EditorGUILayout.IntField(field.Name, (int)value);
                            }
                            else if (field.FieldType == typeof(uint))
                            {
                                EditorGUILayout.IntField(field.Name, Convert.ToInt32((uint)value));
                            }
                            else if (field.FieldType == typeof(long))
                            {
                                EditorGUILayout.LongField(field.Name, (long)value);
                            }
                            else if (field.FieldType == typeof(ulong))
                            {
                                EditorGUILayout.LongField(field.Name, Convert.ToInt64((ulong)value));
                            }
                            else if (field.FieldType == typeof(float))
                            {
                                EditorGUILayout.FloatField(field.Name, (float)value);
                            }
                            else if (field.FieldType == typeof(double))
                            {
                                EditorGUILayout.DoubleField(field.Name, (double)value);
                            }
                            else if (field.FieldType == typeof(Color))
                            {
                                EditorGUILayout.ColorField(field.Name, (Color)value);
                            }
                            else if (field.FieldType == typeof(Vector2))
                            {
                                EditorGUILayout.Vector2Field(field.Name, (Vector2)value);
                            }
                            else if (field.FieldType == typeof(Vector2Int))
                            {
                                EditorGUILayout.Vector2IntField(field.Name, (Vector2Int)value);
                            }
                            else if (field.FieldType == typeof(Vector3))
                            {
                                EditorGUILayout.Vector3Field(field.Name, (Vector3)value);
                            }
                            else if (field.FieldType == typeof(Vector3Int))
                            {
                                EditorGUILayout.Vector3IntField(field.Name, (Vector3Int)value);
                            }
                            else if (field.FieldType == typeof(Vector4))
                            {
                                EditorGUILayout.Vector4Field(field.Name, (Vector4)value);
                            }
                            else if (field.FieldType == typeof(Quaternion))
                            {
                                var quaternion = (Quaternion)value;
                                EditorGUILayout.Vector4Field(field.Name,
                                    new Vector4(quaternion.x, quaternion.y, quaternion.z, quaternion.w));
                            }
                            else if (field.FieldType.IsEnum)
                            {
                                EditorGUILayout.EnumPopup(field.Name, (Enum)value);
                            }
                            else if (field.FieldType == typeof(Friflo.Engine.ECS.Position))
                            {
                                var position = (Friflo.Engine.ECS.Position)value;
                                EditorGUILayout.Vector4Field(field.Name,
                                    new Vector3(position.x, position.y, position.z));
                            }
                            else if (value is UnityEngine.Object obj)
                            {
                                EditorGUILayout.ObjectField(field.Name, obj, field.FieldType, true);
                            }
                            else
                            {
                                EditorGUILayout.LabelField(field.Name, value?.ToString());
                            }
                        }
                    }

                    EditorGUILayout.EndFoldoutHeaderGroup();
                }

                EditorUtility.SetDirty(target);
            }

            /// <summary>
            /// Gets the rainbow color using the specified index
            /// </summary>
            /// <param name="index">The index</param>
            /// <returns>The color</returns>
            private static Color GetRainbowColor(int index)
            {
                return Color.HSVToRGB(index / 16f % 1f, 1, 1);
            }
        }
    }
}