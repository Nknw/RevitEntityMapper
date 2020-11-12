using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitEntityMapper.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class PermissonAttribute : Attribute
    {
        public readonly AccessLevel Read;
        public readonly AccessLevel Write;
        public readonly string Vendor;

        public PermissonAttribute(AccessLevel read) : this(read, AccessLevel.Public, string.Empty)
        { }

        public PermissonAttribute(AccessLevel write,string vendor) : this(AccessLevel.Public, write, vendor)
        { }

        public PermissonAttribute(AccessLevel read, AccessLevel write, string vendor)
        {
            Read = read;
            Write = write;
            Vendor = vendor;
        }
    }
}
