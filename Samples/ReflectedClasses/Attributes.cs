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
        { }
    }

    public class TestSchema2Attribute : SchemaAttribute
    {
        public TestSchema2Attribute() : base("c7355511-675d-4079-903d-a0684d8d05d1","test2")
        { }
    }
}
