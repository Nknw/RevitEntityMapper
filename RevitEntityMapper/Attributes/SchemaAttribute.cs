using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Autodesk.Revit.Mapper
{
    [AttributeUsage(AttributeTargets.Class)]
    public class SchemaAttribute : Attribute
    {
        public readonly Guid Guid;
        public readonly string Name;

        public SchemaAttribute(Guid guid,string name)
        {
            Guid = guid;
            Name = name;
        }
    }
}
