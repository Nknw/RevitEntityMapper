using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCExtensibleStorageExtension.Attributes;
using VCExtensibleStorageExtension;

namespace ReflectedClasses
{
    [Schema("1b9cd3cf-9c57-4834-a64b-7bed19683223", "test")]
    [Autodesk.Revit.Mapper.Schema("1b9cd3cf-9c57-4834-a64b-7bed19683223", "test")]
    public class Included : IRevitEntity
    {
        [Field]
        public string Str { get; set; }
    }

    [Schema("749f8b14-80a1-480c-9a88-524bc1ece775", "test")]
    [Autodesk.Revit.Mapper.Schema("749f8b14-80a1-480c-9a88-524bc1ece775", "test")]
    public class Included2 : IRevitEntity
    {
        [Field]
        public string Str { get; set; }
    }

    [Schema("f01b140d-da57-4891-9aab-1800710dbc48", "test")]
    [Autodesk.Revit.Mapper.Schema("f01b140d-da57-4891-9aab-1800710dbc48", "test")]
    public class Included3 : IRevitEntity
    {
        [Field]
        public string Str { get; set; }
    }

    [Schema("231970cc-4909-44ca-9efd-9fca9d016f8b","test")]
    public class BenchmarkExtensions : IRevitEntity
    {
        [Field]
        public string Str { get; set; }

        [Field]
        public Included Entity { get; set; }

        [Field]
        public List<Included2> List { get; set; }

        [Field]
        public Dictionary<string,Included3> Dict { get; set; }

        public static BenchmarkExtensions CreateDefault()
        {
            return new BenchmarkExtensions()
            {
                Str = "str",
                List = Enumerable.Range(0,500).Select(i=>new Included2() { Str = i.ToString()} ).ToList(),
                Dict = Enumerable.Range(0,500).ToDictionary(k=>k.ToString(),v=>new Included3() { Str = v.ToString() }),
                Entity = new Included() { Str = "" }
            };
        }
    }

    [Autodesk.Revit.Mapper.Schema("231970cc-4909-44ca-9efd-9fca9d016f8b", "test")]
    public class BenchmarkMapper
    {
        public string Str { get; set; }

        public Included Entity { get; set; }

        public IList<Included2> List { get; set; }

        public IDictionary<string,Included3> Dict { get; set; }
    }
}
