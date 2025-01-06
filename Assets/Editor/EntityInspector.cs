using System;
using FrifloECS.Unity.EntityVisualize.Editor.Models;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FrifloECS.Unity.EntityVisualize.Editor
{
    /// <summary>
    /// The entity inspector class
    /// </summary>
    /// <seealso cref="ScriptableObject"/>
    public class EntityInspector : ScriptableObject
    {
        private EntityCollector _collector;

        private int _id;

        public void Bind(EntityCollector collector, int id)
        {
            _collector = collector;
            _id = id;
            Selection.activeObject = id == 0 ? null : this;
            if (this != null)
            {
                EditorUtility.SetDirty(this);
            }
        }

        private EntityInfo GetEntityInfo()
        {
            return _collector.GetEntityInfo(_id);
        }

        /// <summary>
        /// The entity inspector editor class
        /// </summary>
        /// <seealso cref="UnityEditor.Editor"/>
        [CustomEditor(typeof(EntityInspector))]
        public class EntityInspectorEditor : UnityEditor.Editor
        {
            /// <summary>
            /// The inspector
            /// </summary>
            private EntityInspector _inspector;

            private EntityInfo _entityInfo;

            /// <summary>
            /// Ons the header gui
            /// </summary>
            protected override void OnHeaderGUI()
            {
                _inspector = (EntityInspector)target;
                _entityInfo = _inspector.GetEntityInfo();

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
                    var component = componentInfo.Component;
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
                            else if (value is Object obj)
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