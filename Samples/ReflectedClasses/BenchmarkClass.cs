using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.Mapper;

namespace ReflectedClasses
{
    [Schema("1b9cd3cf-9c57-4834-a64b-7bed19683223", "test")]
    public class Included
    {
        public string Str { get; set; }
    }

    [Schema("749f8b14-80a1-480c-9a88-524bc1ece775", "test")]
    public class Included2
    {
        public string Str { get; set; }
    }

    [Schema("f01b140d-da57-4891-9aab-1800710dbc48", "test")]
    public class Included3 
    {
        public string Str { get; set; }
    }

    [Schema("231970cc-4909-44ca-9efd-9fca9d016f8b", "test")]
    public class BenchmarkMapper
    {
        public string Str { get; set; }

        public Included Entity { get; set; }

        public IList<Included2> List { get; set; }

        public IDictionary<string,Included3> Dict { get; set; }
    }
}
