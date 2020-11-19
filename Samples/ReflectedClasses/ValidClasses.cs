using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.Mapper;

namespace ReflectedClasses
{
    [TestSchema]
    public class Bool
    {
        public bool Some { get; set; }
    }

    [TestSchema2]
    public class IncludedEntity
    {
        public Bool Some { get; set; }
    }

    [TestSchema]
    public class Dict
    {
        public IDictionary<string,bool> Some { get; set; }
    }

    [TestSchema]
    public class Lst
    {
        public IList<bool> Some { get; set; }
    }

    [TestSchema2]
    public class IncludedEntityInDictionary
    {
        public IDictionary<string,Bool> Some { get; set; }
    }

    [TestSchema2]
    public class IncludedEntityList
    {
        public IList<Bool> Some { get; set; }
    }
}
