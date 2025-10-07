using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Friflo.Engine.ECS;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FrifloECS.Unity.EntityVisualize.Editor.Models
{
    public partial class ComponentInfo
    {
        private static readonly Dictionary<Type, Func<string, object, object>> FieldDrawers = new()
        {
            {
                typeof(string), (fieldName, value) => EditorGUILayout.TextField(fieldName, (string)value)
            },
            {
                typeof(bool), (fieldName, value) => EditorGUILayout.Toggle(fieldName, (bool)value)
            },
            {
                typeof(byte), (fieldName, value) => (byte)EditorGUILayout.IntField(fieldName, (int)value)
            },
            {
                typeof(short), (fieldName, value) => (short)EditorGUILayout.IntField(fieldName, (int)value)
            },
            {
                typeof(ushort),
                (fieldName, value) =>
                    (ushort)Mathf.Max(EditorGUILayout.IntField(fieldName, Convert.ToInt16((ushort)value)), 0)
            },
            {
                typeof(int), (fieldName, value) => EditorGUILayout.IntField(fieldName, (int)value)
            },
            {
                typeof(uint),
                (fieldName, value) =>
                    (uint)Mathf.Max(EditorGUILayout.IntField(fieldName, Convert.ToInt32((uint)value)), 0)
            },
            {
                typeof(long), (fieldName, value) => EditorGUILayout.LongField(fieldName, (long)value)
            },
            {
                typeof(ulong),
                (fieldName, value) =>
                    (ulong)Mathf.Max(EditorGUILayout.LongField(fieldName, Convert.ToInt64((ulong)value)), 0)
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
                typeof(Rect), (fieldName, value) => EditorGUILayout.RectField(fieldName, (Rect)value)
            },
            {
                typeof(RectInt), (fieldName, value) => EditorGUILayout.RectIntField(fieldName, (RectInt)value)
            },
            {
                typeof(Quaternion), (fieldName, value) =>
                {
                    var quaternion = (Quaternion)value;
                    var vec4 = EditorGUILayout.Vector4Field(fieldName,
                        new Vector4(quaternion.x, quaternion.y, quaternion.z, quaternion.w));
                    return new Quaternion(vec4.x, vec4.y, vec4.z, vec4.w);
                }
            },
            {
                typeof(Position),
                (fieldName, value) =>
                {
                    var position = (Position)value;
                    var vec3 = EditorGUILayout.Vector3Field(fieldName,
                        new Vector3(position.x, position.y, position.z));
                    return new Position(vec3.x, vec3.y, vec3.z);
                }
            },
            {
                typeof(Scale3),
                (fieldName, value) =>
                {
                    var scale = (Scale3)value;
                    var vec3 = EditorGUILayout.Vector3Field(fieldName,
                        new Vector3(scale.x, scale.y, scale.z));
                    return new Scale3(vec3.x, vec3.y, vec3.z);
                }
            }
        };

        private static readonly Dictionary<Type, FieldInfo[]> FieldsCache = new();

        public void OnInspectorGUI(Entity entity)
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

            var changed = false;
            var componentType = component.GetType();
            if (!FieldsCache.TryGetValue(componentType, out var fields))
            {
                FieldsCache[componentType] =
                    fields = componentType.GetFields(BindingFlags.Instance | BindingFlags.Public);
            }

            foreach (var field in fields)
            {
                var fieldType = field.FieldType;
                if (field.IsDefined(typeof(DebuggerBrowsableAttribute), true))
                {
                    continue;
                }

                var isInitOnly = field.IsInitOnly;
                if (isInitOnly)
                {
                    EditorGUI.BeginDisabledGroup(true);
                }
                else
                {
                    EditorGUI.BeginChangeCheck();
                }

                var value = field.GetValue(component);
                object newValue = null;
                if (FieldDrawers.TryGetValue(fieldType, out var drawer))
                {
                    newValue = drawer(field.Name, value);
                }
                else if (fieldType.IsEnum)
                {
                    newValue = EditorGUILayout.EnumPopup(field.Name, (Enum)value);
                }
                else if (typeof(Object).IsAssignableFrom(fieldType))
                {
                    newValue = EditorGUILayout.ObjectField(field.Name, (Object)value, fieldType, false);
                }
                else
                {
                    value ??= "null";
                    EditorGUILayout.LabelField(field.Name, value.ToString());
                }

                if (isInitOnly)
                {
                    EditorGUI.EndDisabledGroup();
                }
                else if (EditorGUI.EndChangeCheck())
                {
                    field.SetValue(component, newValue);
                    changed = true;
                }
            }

            if (changed)
            {
                EntityUtils.AddEntityComponentValue(entity, EntityComponent.Type, component);
            }
        }
    }
}