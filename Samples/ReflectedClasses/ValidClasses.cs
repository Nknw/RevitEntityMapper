using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.Mapper;

namespace ReflectedClasses
{
    [Schema("56f48442-ae75-4a01-98c6-a3b6a4d4bc8f","test")]
    public class Bool
    {
        public bool Some { get; set; }
    }

    [Schema("af7772b6-8088-4f04-bc77-2ca8888fcff8","test")]
    public class Recursive
    {
        public Recursive Some { get; set; }
    }

    [Schema("e6e9bb2d-5041-4542-a73f-65b025db20ce", "test")]
    public class IncludedEntity
    {
        public Bool Some { get; set; }
    }

    [Schema("396c663b-3b48-47d7-bbea-e62ed0958c07","test")]
    public class SameIncludedEntity
    {
        public Bool Some { get; set; }
        public Bool Some2 { get; set; }
    }

    [Schema("f36e7af2-ffcc-44d9-a912-28d7da92fe3c", "test")]
    public class Dict
    {
        public IDictionary<string,bool> Some { get; set; }
    }

    [Schema("f9ce5303-a8f1-4793-a855-0f8b30c5978e", "test")]
    public class Lst
    {
        public IList<bool> Some { get; set; }
    }

    [Schema("0f1d6cfd-c36b-4208-b689-25a9dd61387e", "test")]
    public class IncludedEntityInDictionary
    {
        public IDictionary<string,Bool> Some { get; set; }
    }

    [Schema("bc6869b6-3ef0-44dc-bd13-16cdbc87054c", "test")]
    public class IncludedEntityList
    {
        public IList<Bool> Some { get; set; }
    }
}
