using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.Mapper;
using RevitEntityMapper.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Autodesk.Revit.Mapper
{
    #region abstraction
    internal interface IFieldMapper
    {
        FieldBuilder Map(PropertyInfo propertyInfo);
    }

    internal abstract class FieldMapperBase : IFieldMapper
    {
        protected readonly SchemaBuilder _schemaBuilder;
        protected readonly HashSet<Type> _allowedTypes;
        protected FieldMapperBase Next;

        public FieldMapperBase(SchemaBuilder schemaBuilder, HashSet<Type> allowedTypes)
        {
            _schemaBuilder = schemaBuilder;
            _allowedTypes = allowedTypes;
        }

        public abstract FieldBuilder Map(PropertyInfo propertyInfo);

        protected FieldBuilder AddSimpleField(Type propType, Func<Type,FieldBuilder> addField)
        {
            FieldBuilder builder;
            if (!_allowedTypes.Contains(propType))
            {
                Mapper.CreateSchema(propType);
                builder = addField(typeof(Entity));
            }
            else builder = addField(propType);
            return builder;
        }
    }
    #endregion

    #region mappers
    internal class MapFieldMapper : FieldMapperBase
    {
        private readonly HashSet<Type> allowedKeys;

        public MapFieldMapper(SchemaBuilder schemaBuilder,HashSet<Type> allowedTypes) 
            : base(schemaBuilder,allowedTypes)
        {
            allowedKeys = new HashSet<Type>(allowedTypes);
            allowedKeys.ExceptWith(new[] { typeof(double), typeof(float), typeof(UV), typeof(XYZ) });
            Next = new ArrayFieldMapper(schemaBuilder, allowedTypes);
        }

        public override FieldBuilder Map(PropertyInfo propertyInfo)
        {
            var propType = propertyInfo.PropertyType;
            if (!propType.IsInterface || propType.GetGenericTypeDefinition() != typeof(IDictionary<,>))
                return Next?.Map(propertyInfo);
            var pairType = propType.GetGenericArguments();
            if (!allowedKeys.Contains(pairType[0]))
                throw new ArgumentException($"{propType} not supported in map");
            return AddSimpleField(pairType[1],t=> _schemaBuilder.AddMapField(propertyInfo.Name, pairType[0], t));
        }
    }

    internal class ArrayFieldMapper : FieldMapperBase
    {
        public ArrayFieldMapper(SchemaBuilder schemaBuilder, HashSet<Type> allowedTypes) 
            : base(schemaBuilder,allowedTypes)
        {
            Next = new SimpleFieldMapper(schemaBuilder, allowedTypes);
        }

        public override FieldBuilder Map(PropertyInfo propertyInfo)
        {
            var propType = propertyInfo.PropertyType;
            if (!propType.IsInterface || propType.GetGenericTypeDefinition() != typeof(IList<>))
                return Next?.Map(propertyInfo);
            var genericType = propType.GetGenericArguments()[0];
            return AddSimpleField(genericType, t => _schemaBuilder.AddArrayField(propType.Name, t));
        }
    }

    internal class SimpleFieldMapper : FieldMapperBase
    {
        public SimpleFieldMapper(SchemaBuilder schemaBuilder, HashSet<Type> allowedTypes) 
            : base(schemaBuilder, allowedTypes)
        {
        }

        public override FieldBuilder Map(PropertyInfo propertyInfo)
        {
            var propType = propertyInfo.PropertyType;
            return AddSimpleField(propType, t => _schemaBuilder.AddSimpleField(propType.Name, t));
        }
    }
    #endregion

    internal class FieldMapper : FieldMapperBase
    { 
        private readonly static HashSet<Type> basicAllowedTypes = new HashSet<Type>()
        {
            typeof(bool),
            typeof(byte),
            typeof(short),
            typeof(float),
            typeof(double),
            typeof(int),
            typeof(ElementId),
            typeof(string),
            typeof(XYZ),
            typeof(UV),
            typeof(Guid)
        };

        public FieldMapper(SchemaBuilder schemaBuilder) : base(schemaBuilder, basicAllowedTypes) 
        {
            Next = new MapFieldMapper(schemaBuilder,_allowedTypes);
        }

        public override FieldBuilder Map(PropertyInfo propertyInfo)
        {
            return Next.Map(propertyInfo);
        }
    }
}
