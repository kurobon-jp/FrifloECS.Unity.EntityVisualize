using System;
using Friflo.Engine.ECS;

namespace FrifloECS.Unity.EntityVisualize.Editor.Models
{
    /// <summary>
    /// The component info class
    /// </summary>
    public class ComponentInfo
    {
        /// <summary>
        /// Gets the value of the component id
        /// </summary>
        public EntityComponent EntityComponent { get; }

        /// <summary>
        /// Gets the value of the component name
        /// </summary>
        public string ComponentName => EntityComponent.Type.Name;

        /// <summary>
        /// Gets or sets the value of the component
        /// </summary>
        [Obsolete("Obsolete")]
        public object Component => EntityComponent.Value;

        /// <summary>
        /// Gets or sets the value of the foldout
        /// </summary>
        public bool Foldout { get; set; } = true;

        public ComponentInfo(EntityComponent entityComponent)
        {
            EntityComponent = entityComponent;
        }
    }
}