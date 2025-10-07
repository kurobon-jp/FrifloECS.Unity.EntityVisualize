using FrifloECS.Unity.EntityVisualize.Editor.Models;
using UnityEditor;
using UnityEngine;

namespace FrifloECS.Unity.EntityVisualize.Editor
{
    [CustomEditor(typeof(EntitiesHierarchyWindow))]
    public class EntityInspector : UnityEditor.Editor
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
            _entityInfo = _window.GetSelectedEntityInfo();
            if (_entityInfo == null) return;
            DrawCustomHeader($"{_entityInfo}");
        }

        /// <summary>
        /// Ons the inspector gui
        /// </summary>
        public override void OnInspectorGUI()
        {
            if (_entityInfo == null) return;
            _entityInfo.OnInspectorGUI();
            EditorUtility.SetDirty(target);
        }

        private void DrawCustomHeader(string title)
        {
            var rect = GUILayoutUtility.GetRect(1, 48);
            rect.xMin = 0;
            rect.xMax += 4;
            EditorGUI.DrawRect(rect,  new(0.18f, 0.18f, 0.18f));
            var style = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 24,
                padding = new RectOffset(8, 8, 8, 8)
            };
            var labelRect = new Rect(rect.x + 6, rect.y + 2, rect.width - 40, rect.height);
            EditorGUI.LabelField(labelRect, title, style);
            var menuRect = new Rect(rect.xMax - 25, rect.y + 2, 20, 20);
            if (GUI.Button(menuRect, EditorGUIUtility.IconContent("_Menu"), GUIStyle.none))
            {
                ShowHeaderMenu();
            }
        }

        private void ShowHeaderMenu()
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Delete Entity"), false, DeleteEntity);
            menu.ShowAsContext();
        }

        private void DeleteEntity()
        {
            if (_entityInfo == null) return;
            _entityInfo.Entity.DeleteEntity();
            _entityInfo = null;
            EditorUtility.SetDirty(target);
        }
    }
}