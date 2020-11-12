using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Autodesk.Revit.Mapper
{
    [AttributeUsage(AttributeTargets.Class|AttributeTargets.Property)]
    public class DocumentationAttribute : Attribute
    {
        public readonly string Description;

        public DocumentationAttribute(string description)
        {
            Description = description;
        }
    }
}
