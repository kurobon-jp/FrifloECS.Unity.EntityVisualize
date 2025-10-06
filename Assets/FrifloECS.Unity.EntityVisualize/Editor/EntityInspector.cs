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
            var style = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 24,
                padding = new RectOffset(8, 8, 8, 8)
            };
            GUILayout.Label($"{_entityInfo}", style);

            _entityInfo.OnInspectorGUI();
            EditorUtility.SetDirty(target);
        }

        /// <summary>
        /// Ons the inspector gui
        /// </summary>
        public override void OnInspectorGUI()
        {
        }
    }
}