using System.Collections.Generic;

namespace FrifloECS.Unity.EntityVisualize.Editor.Models
{
    /// <summary>
    /// The entity info class
    /// </summary>
    public class EntityInfo
    {
        /// <summary>
        /// Gets the value of the entity id
        /// </summary>
        public int Id { get; }

        public string Name { get; }

        
        /// <summary>
        /// Gets the value of the components
        /// </summary>
        public List<ComponentInfo> Components { get; } = new();

        public EntityInfo(int id, string name)
        {
            Id = id;
            Name = name;
        }

        /// <summary>
        /// Adds the value
        /// </summary>
        /// <param name="value">The value</param>
        public void Add(ComponentInfo value)
        {
            Components.Add(value);
        }

        /// <summary>
        /// Returns the string
        /// </summary>
        /// <returns>The string</returns>
        public override string ToString()
        {
            return $"Entity id:{Id}";
        }
    }
}