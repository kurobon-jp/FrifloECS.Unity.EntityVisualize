using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FrifloECS.Unity.EntityVisualize.Editor.Models
{
    public partial class ComponentInfo
    {
        private static readonly Dictionary<Type, Action<string, object>> FieldGUILayouts = new()
        {
            {
                typeof(string), (fieldName, value) => EditorGUILayout.LabelField(fieldName, (string)value)
            },
            {
                typeof(bool), (fieldName, value) => EditorGUILayout.Toggle(fieldName, (bool)value)
            },
            {
                typeof(byte), (fieldName, value) => EditorGUILayout.LabelField(fieldName, (string)value)
            },
            {
                typeof(short), (fieldName, value) => EditorGUILayout.IntField(fieldName, (int)value)
            },
            {
                typeof(ushort),
                (fieldName, value) => EditorGUILayout.IntField(fieldName, Convert.ToInt16((ushort)value))
            },
            {
                typeof(int), (fieldName, value) => EditorGUILayout.IntField(fieldName, (int)value)
            },
            {
                typeof(uint),
                (fieldName, value) => EditorGUILayout.IntField(fieldName, Convert.ToInt32((uint)value))
            },
            {
                typeof(long), (fieldName, value) => EditorGUILayout.LongField(fieldName, (long)value)
            },
            {
                typeof(ulong),
                (fieldName, value) => EditorGUILayout.LongField(fieldName, Convert.ToInt64((ulong)value))
            },
            {
                typeof(float), (fieldName, value) => EditorGUILayout.FloatField(fieldName, (float)value)
            },
            {
                typeof(double), (fieldName, value) => EditorGUILayout.DoubleField(fieldName, (double)value)
            },

            {
                typeof(Color), (fieldName, value) => EditorGUILayout.ColorField(fieldName, (Color)value)
            },
            {
                typeof(Vector2), (fieldName, value) => EditorGUILayout.Vector2Field(fieldName, (Vector2)value)
            },
            {
                typeof(Vector2Int),
                (fieldName, value) => EditorGUILayout.Vector2IntField(fieldName, (Vector2Int)value)
            },
            {
                typeof(Vector3), (fieldName, value) => EditorGUILayout.Vector3Field(fieldName, (Vector3)value)
            },
            {
                typeof(Vector3Int),
                (fieldName, value) => EditorGUILayout.Vector3IntField(fieldName, (Vector3Int)value)
            },
            {
                typeof(Vector4), (fieldName, value) => EditorGUILayout.Vector4Field(fieldName, (Vector4)value)
            },
            {
                typeof(Quaternion), (fieldName, value) =>
                {
                    var quaternion = (Quaternion)value;
                    EditorGUILayout.Vector4Field(fieldName,
                        new Vector4(quaternion.x, quaternion.y, quaternion.z, quaternion.w));
                }
            },
            {
                typeof(Friflo.Engine.ECS.Position),
                (fieldName, value) =>
                {
                    var position = (Friflo.Engine.ECS.Position)value;
                    EditorGUILayout.Vector4Field(fieldName,
                        new Vector3(position.x, position.y, position.z));
                }
            }
        };

        public void OnInspectorGUI()
        {
            object component;
            try
            {
                component = EntityComponent.Value;
            }
            catch
            {
                return;
            }

            foreach (var field in component.GetType().GetFields())
            {
                var value = field.GetValue(component);
                var type = field.FieldType;
                if (FieldGUILayouts.TryGetValue(type, out var onGUILayout))
                {
                    onGUILayout(field.Name, value);
                }
                else if (type.IsEnum)
                {
                    EditorGUILayout.EnumPopup(field.Name, (Enum)value);
                }
                else if (value is UnityEngine.Object obj)
                {
                    EditorGUILayout.ObjectField(field.Name, obj, type, true);
                }
                else
                {
                    EditorGUILayout.LabelField(field.Name, value?.ToString());
                }
            }
        }
    }
}