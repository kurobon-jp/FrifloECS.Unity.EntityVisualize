using System.Collections.Generic;
using Friflo.Engine.ECS;
using UnityEditor;
using UnityEngine;

namespace FrifloECS.Unity.EntityVisualize.Editor.Models
{
    public partial class EntityInfo
    {
        private static readonly Dictionary<ComponentType, bool> ComponentFoldouts = new();

        public void OnInspectorGUI()
        {
            var color = 0;
            if (Tags.Count > 0)
            {
                using (new GUILayout.VerticalScope("box"))
                {
                    GUILayout.Label("Tags", EditorStyles.boldLabel);
                }

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Space(15);
                    for (var i = 0; i < Tags.Count; i++)
                    {
                        var tag = Tags[i];
                        var rect = EditorGUILayout.GetControlRect();
                        var width = rect.width;
                        EditorGUI.DrawRect(rect, GetRainbowColor(color++));
                        rect.x += 5;
                        rect.width -= 35;
                        EditorGUI.LabelField(rect, tag.TagName, EditorStyles.boldLabel);
                        rect.width = 25;
                        rect.x = width - 10;
                        if (GUI.Button(rect, "-", EditorStyles.miniButton))
                        {
                            Entity.RemoveTags(new Tags(tag));
                        }
                    }
                }
            }

            if (Components.Count > 0)
            {
                using (new GUILayout.VerticalScope("box"))
                {
                    GUILayout.Label("Components", EditorStyles.boldLabel);
                }

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Space(15);
                    using (new GUILayout.VerticalScope())
                    {
                        for (var i = 0; i < Components.Count; i++)
                        {
                            var componentInfo = Components[i];
                            var componentType = componentInfo.EntityComponent.Type;
                            ComponentFoldouts.TryGetValue(componentType, out var foldout);
                            var rect = EditorGUILayout.GetControlRect();
                            var width = rect.width;
                            EditorGUI.DrawRect(rect, GetRainbowColor(color++));
                            rect.x += 5;
                            rect.width -= 35;
                            foldout = EditorGUI.Foldout(rect, foldout, componentInfo.ComponentName, true);
                            ComponentFoldouts[componentType] = foldout;
                            if (foldout)
                            {
                                EditorGUI.indentLevel++;
                                componentInfo.OnInspectorGUI(Entity);
                                EditorGUI.indentLevel--;
                            }

                            rect.width = 25;
                            rect.x = width - 10;
                            if (GUI.Button(rect, "-", EditorStyles.miniButton))
                            {
                                EntityUtils.RemoveEntityComponent(Entity, componentType);
                            }
                        }
                    }
                }
            }

            if (Scripts.Count > 0)
            {
                using (new GUILayout.VerticalScope("box"))
                {
                    GUILayout.Label("Scripts", EditorStyles.boldLabel);
                }

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Space(15);
                    for (var i = 0; i < Scripts.Count; i++)
                    {
                        var script = Scripts[i];
                        var rect = EditorGUILayout.GetControlRect();
                        EditorGUI.DrawRect(rect, GetRainbowColor(color++));
                        rect.x += 5;
                        EditorGUI.LabelField(rect, script.GetType().Name, EditorStyles.boldLabel);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the rainbow color using the specified index
        /// </summary>
        /// <param name="index">The index</param>
        /// <returns>The color</returns>
        private static Color GetRainbowColor(int index)
        {
            var color = Color.HSVToRGB(index / 16f % 1f, 1, 1) * 0.25f;
            color.a = 1;
            return color;
        }
    }
}