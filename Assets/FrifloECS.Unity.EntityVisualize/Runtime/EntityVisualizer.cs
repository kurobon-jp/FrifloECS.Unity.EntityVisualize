#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Friflo.Engine.ECS;

namespace FrifloECS.Unity.EntityVisualize
{
    /// <summary>
    /// The entity visualizer class
    /// </summary>
    public static class EntityVisualizer
    {
        internal static Dictionary<string, EntityStore> EntityStores { get; } = new();

        internal static event Action<string, EntityStore> OnRegistered;

        public static void Register(string name, EntityStore entityStore)
        {
            EntityStores[name] = entityStore;
            OnRegistered?.Invoke(name, entityStore);
        }

        public static void Clear()
        {
            EntityStores.Clear();
            OnRegistered = null;
        }
    }
}
#endif