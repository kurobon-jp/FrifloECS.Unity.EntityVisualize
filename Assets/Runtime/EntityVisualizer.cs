#if UNITY_EDITOR
using System.Collections.Generic;
using Friflo.Engine.ECS;

namespace FrifloECS.Unity.EntityVisualize
{
    /// <summary>
    /// The entity visualizer class
    /// </summary>
    public static class EntityVisualizer
    {
        public static Dictionary<string, EntityStore> EntityStores { get; } = new();

        public static void Register(string name, EntityStore entityStore)
        {
            EntityStores[name] = entityStore;
        }

        public static void UnRegister(string name)
        {
            EntityStores.Remove(name);
        }
    }
}
#endif