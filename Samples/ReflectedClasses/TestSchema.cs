using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.Mapper;

namespace ReflectedClasses
{
    public class TestSchemaAttribute : SchemaAttribute
    {
        public TestSchemaAttribute() : base("e7fd718b-d7f5-4dce-ac33-06e138749344", "test")
            {}
    }
}
