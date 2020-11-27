using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.Mapper;

namespace Sample
{
    public static class MapperInstance
    {
        private readonly static Mapper mapper = new Mapper();

        public static Mapper Get() => mapper;
    }
}
