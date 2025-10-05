using System.Collections.Generic;
using Friflo.Engine.ECS;
using UnityEditor;
using UnityEngine;

namespace FrifloECS.Unity.EntityVisualize.Editor.Models
{
    public partial class EntityInfo
    {
        private static readonly Dictionary<ComponentType, bool> Foldouts = new();

        public void OnInspectorGUI()
        {
            var defaultColor = GUI.backgroundColor;
            for (var i = 0; i < Components.Count; i++)
            {
                var componentInfo = Components[i];
                GUI.backgroundColor = GetRainbowColor(i);
                var type = componentInfo.EntityComponent.Type;
                Foldouts.TryGetValue(type, out var foldout);
                foldout = EditorGUILayout.BeginFoldoutHeaderGroup(foldout, componentInfo.ComponentName);
                GUI.backgroundColor = defaultColor;
                Foldouts[type] = foldout;
                if (foldout)
                {
                    componentInfo.OnInspectorGUI();
                }

                EditorGUILayout.EndFoldoutHeaderGroup();
            }
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