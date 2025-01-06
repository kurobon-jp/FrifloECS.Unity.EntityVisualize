using System;
using System.Collections.Generic;
using Friflo.Engine.ECS;
using FrifloECS.Unity.EntityVisualize.Editor.Models;

namespace FrifloECS.Unity.EntityVisualize.Editor
{
    /// <summary>
    /// The entity collector class
    /// </summary>
    public class EntityCollector
    {
        private EntityStore _entityStore;

        private readonly SortedDictionary<int, Entity> _entities = new();

        public void Bind(EntityStore entityStore)
        {
            _entityStore = entityStore;
        }

        /// <summary>
        /// Ticks this instance
        /// </summary>
        public SortedDictionary<int, Entity> CollectEntities()
        {
            _entities.Clear();
            foreach (var entity in _entityStore.Entities)
            {
                if (entity.IsNull || !entity.Parent.IsNull) continue;
                _entities[entity.Id] = entity;
            }

            return _entities;
        }

        public EntityInfo GetEntityInfo(int id)
        {
            var entity = _entityStore.GetEntityById(id);
            if (entity.IsNull) return null;
            var entityInfo = new EntityInfo(id, entity.ToString());
            foreach (var component in entity.Components)
            {
                entityInfo.Add(new ComponentInfo(component));
            }


            return entityInfo;
        }
    }
}