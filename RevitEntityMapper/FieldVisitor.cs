using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Autodesk.Revit.DB;

namespace Autodesk.Revit.Mapper
{
    internal abstract class FieldVisitor<T>
        where T: class
    {
        protected readonly HashSet<Type> _allowedTypes;
        protected FieldVisitor<T> Next;

        public FieldVisitor(HashSet<Type> allowedTypes)
        {
            _allowedTypes = allowedTypes;
        }

        public T Visit(PropertyInfo propertyInfo)
        {
            if (!IsMyWork(propertyInfo))
                return Next?.Visit(propertyInfo);
            return Handle(propertyInfo);
        }

        protected abstract T Handle(PropertyInfo propertyInfo);

        protected abstract bool IsMyWork(PropertyInfo propertyInfo);
    }

    internal abstract class MapFieldVisistor<T> : FieldVisitor<T>
        where T : class
    {
        private readonly HashSet<Type> allowedKeys;

        public MapFieldVisistor(HashSet<Type> allowedTypes) : base(allowedTypes)
        {
            allowedKeys = new HashSet<Type>(allowedTypes);
            allowedKeys.ExceptWith(new[] { typeof(double), typeof(float), typeof(UV), typeof(XYZ) });
        }

        protected override bool IsMyWork(PropertyInfo propertyInfo)
        {
            var propType = propertyInfo.PropertyType;
            return propType.IsInterface && propType.GetGenericTypeDefinition() == typeof(IDictionary<,>);
        }
    }

    internal abstract class ArrayFieldVisitor<T> : FieldVisitor<T>
        where T : class
    {
        public ArrayFieldVisitor(HashSet<Type> allowedTypes) : base(allowedTypes)
        {
        }

        protected override bool IsMyWork(PropertyInfo propertyInfo)
        {
            var propType = propertyInfo.PropertyType;
            return propType.IsInterface && propType.GetGenericTypeDefinition() == typeof(IList<>);
        }
    }

    internal abstract class SimpleFieldVisitor<T> : FieldVisitor<T>
        where T : class
    {
        public SimpleFieldVisitor(HashSet<Type> allowedTypes) : base(allowedTypes)
        {
        }

        protected override bool IsMyWork(PropertyInfo propertyInfo)
        {
            return _allowedTypes.Contains(propertyInfo.PropertyType);
        }
    }

    internal abstract class EntityFieldVisitor<T> : FieldVisitor<T>
        where T : class
    {
        public EntityFieldVisitor(HashSet<Type> allowedTypes) : base(allowedTypes)
        {
        }

        protected override bool IsMyWork(PropertyInfo propertyInfo)
            => true;
    }

    internal abstract class HeadFieldVisitor<T> : FieldVisitor<T>
        where T : class
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

        public HeadFieldVisitor() : base(basicAllowedTypes)
        {
        }

        protected override bool IsMyWork(PropertyInfo propertyInfo)
            => false;
    }
}
