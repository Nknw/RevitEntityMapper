using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Revit.EntityMapper;

namespace Sample
{
    public static class MapperInstance
    {
        private readonly static IMapper<Task> mapper = Mapper.CreateAdHoc<Task>(); 

        public static IMapper<Task> Get() => mapper;
    }
}
