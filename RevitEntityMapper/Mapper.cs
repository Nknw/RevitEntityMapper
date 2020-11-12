using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using RevitEntityMapper;
using RevitEntityMapper.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;

namespace Autodesk.Revit.Mapper
{
    public abstract class Mapper
    {
        public abstract bool TryGetEnity<TEntity>(Element element, out TEntity entity)
            where TEntity : class, new();

        public abstract void SetEntity<TEntity>(Element element, TEntity entity)
            where TEntity : class, new();

        public static void CreateSchema<TEntity>()
            where TEntity : class, new()
        {
            var eType = typeof(TEntity);
            CreateSchema(eType);
        }

        internal static Func<Entity,object> CreateFactory(Type eType)
        {
            var properties = eType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite && p.GetCustomAttribute<NotMappedAttribute>() == null);
            var entityParameter = Expression.Parameter(typeof(Entity), "entity");

        }

        internal static void CreateSchema(Type eType) 
        {
            var schema = eType.GetCustomAttribute<SchemaAttribute>();
            if (schema == null)
                throw new ArgumentException($"{eType} has no SchemaAttribute");
            if (Schema.Lookup(schema.Guid) != null)
                return;
            var schemaBuilder = new SchemaBuilder(schema.Guid)
                .SetSchemaName(schema.Name)
                .SetPermissions(eType)
                .SetDocumentation(eType);
            var fieldMapper = new FieldMapper(schemaBuilder);
            eType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite && p.GetCustomAttribute<NotMappedAttribute>() == null)
                .ForEach(p => fieldMapper.Map(p)
                    .SetDocumentation(p)
                    .SetUnitType(p));
            schemaBuilder.Finish();
        }
    }
}
