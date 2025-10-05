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

        public bool IsDirty { get; set; } = true;

        public void Bind(EntityStore entityStore)
        {
            if (_entityStore != null)
            {
                _entityStore.OnEntitiesChanged -= OnEntitiesChanged;
                _entityStore.OnEntityCreate -= OnEntityCreate;
                _entityStore.OnEntityDelete -= OnEntityDelete;
                _entityStore.OnChildEntitiesChanged -= OnChildEntitiesChanged;
                _entityStore.OnComponentAdded -= OnComponentAdded;
                _entityStore.OnComponentRemoved -= OnComponentRemoved;
                _entityStore.OnScriptAdded -= OnScriptAdded;
                _entityStore.OnScriptRemoved -= OnScriptRemoved;
            }

            _entityStore = entityStore;
            IsDirty = true;
            if (_entityStore == null) return;
            _entityStore.OnEntitiesChanged += OnEntitiesChanged;
            _entityStore.OnEntityCreate += OnEntityCreate;
            _entityStore.OnEntityDelete += OnEntityDelete;
            _entityStore.OnChildEntitiesChanged += OnChildEntitiesChanged;
            _entityStore.OnComponentAdded += OnComponentAdded;
            _entityStore.OnComponentRemoved += OnComponentRemoved;
            _entityStore.OnScriptAdded += OnScriptAdded;
            _entityStore.OnScriptRemoved += OnScriptRemoved;
        }

        private void OnComponentRemoved(ComponentChanged obj)
        {
            IsDirty = true;
        }

        private void OnComponentAdded(ComponentChanged obj)
        {
            IsDirty = true;
        }

        private void OnScriptRemoved(ScriptChanged obj)
        {
            IsDirty = true;
        }

        private void OnScriptAdded(ScriptChanged obj)
        {
            IsDirty = true;
        }

        private void OnChildEntitiesChanged(ChildEntitiesChanged obj)
        {
            IsDirty = true;
        }

        private void OnEntityDelete(EntityDelete obj)
        {
            IsDirty = true;
        }

        private void OnEntityCreate(EntityCreate obj)
        {
            IsDirty = true;
        }

        private void OnEntitiesChanged(object sender, EntitiesChanged e)
        {
            IsDirty = true;
        }

        /// <summary>
        /// Ticks this instance
        /// </summary>
        public List<Entity> CollectEntities()
        {
            var entities = new List<Entity>();
            if (_entityStore != null)
            {
                foreach (var entity in _entityStore.Entities)
                {
                    if (entity.IsNull || !entity.Parent.IsNull) continue;
                    entities.Add(entity);
                }
            }

            return entities;
        }

        public EntityInfo GetEntityInfo(int id)
        {
            var entity = _entityStore.GetEntityById(id);
            if (entity.IsNull) return null;
            var entityInfo = new EntityInfo(entity);
            foreach (var component in entity.Components)
            {
                entityInfo.Add(new ComponentInfo(component));
            }

            return entityInfo;
        }
    }
}