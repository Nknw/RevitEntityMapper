using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.Mapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitEntityMapper
{
    internal class CachingMapper : Mapper
    {
        private readonly Dictionary<Type, Func<Entity, object>> factories;
        private readonly Dictionary<Type, Guid> identity;

        public override void SetEntity<TEntity>(Element element, TEntity entity)
        {
            throw new NotImplementedException();
        }

        public override bool TryGetEnity<TEntity>(Element element, out TEntity entity)
        {
            var eType = typeof(TEntity);
            entity = null;
            if (!identity.TryGetValue(eType, out var id))
            {
                var elementEntity = element.GetEntity(Schema.Lookup(id));
                if (!elementEntity.IsValid())
                    return false;
                entity = factories[eType](elementEntity) as TEntity;
                return true;
            }
        }
    }
}
