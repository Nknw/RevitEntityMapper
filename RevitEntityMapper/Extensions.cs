using Autodesk.Revit.DB.ExtensibleStorage;
using RevitEntityMapper.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Autodesk.Revit.Mapper;
using Autodesk.Revit.DB;

namespace RevitEntityMapper
{
    internal static class Extensions
    {
        internal static SchemaBuilder SetPermissions(this SchemaBuilder builder,Type eType)
        {
            var permissions = eType.GetCustomAttribute<PermissonAttribute>(true);
            if (permissions == null)
                return builder.SetReadAccessLevel(AccessLevel.Public)
                    .SetWriteAccessLevel(AccessLevel.Public);
            if (string.IsNullOrEmpty(permissions.Vendor))
                return builder.SetReadAccessLevel(permissions.Read)
                    .SetWriteAccessLevel(AccessLevel.Public);
            return builder.SetReadAccessLevel(permissions.Read)
                .SetWriteAccessLevel(permissions.Write)
                .SetVendorId(permissions.Vendor);
        }

        internal static SchemaBuilder SetDocumentation(this SchemaBuilder bilder,Type eType)
        {
            var doc = eType.GetCustomAttribute<DocumentationAttribute>(true);
            if (doc == null)
                return bilder;
            return bilder.SetDocumentation(doc.Description);
        }


        internal static FieldBuilder SetDocumentation(this FieldBuilder bilder, MemberInfo eType)
        {
            var doc = eType.GetCustomAttribute<DocumentationAttribute>(true);
            if (doc == null)
                return bilder;
            return bilder.SetDocumentation(doc.Description);
        }

        internal static FieldBuilder SetUnitType(this FieldBuilder builder,MemberInfo info,UnitType? defaultUnitType = null)
        {
            if (!builder.NeedsUnits())
                return builder;
            var unit = info.GetCustomAttribute<UnitAttribute>(true);
            if (unit == null && defaultUnitType == null)
                throw new ArgumentException($"{info.Name} requared UnitType");
            return builder.SetUnitType(unit?.UnitType ?? (UnitType)defaultUnitType);
        }

        internal static void ForEach<T>(this IEnumerable<T> collection,Action<T> handler)
        {
            foreach (var e in collection)
                handler(e);
        }
    }
}
