using System;
using Friflo.Engine.ECS;

namespace FrifloECS.Unity.EntityVisualize.Editor.Models
{
    /// <summary>
    /// The component info class
    /// </summary>
    public partial class ComponentInfo
    {
        /// <summary>
        /// Gets the value of the component id
        /// </summary>
        public EntityComponent EntityComponent { get; }

        public Type ComponentType { get; }

        /// <summary>
        /// Gets the value of the component name
        /// </summary>
        public string ComponentName { get; }

        public ComponentInfo(EntityComponent entityComponent)
        {
            EntityComponent = entityComponent;
            ComponentType = entityComponent.Type.Type;
            ComponentName = ComponentType.Name;
        }
    }
}