using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.Mapper;
using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.DB;

namespace ReflectedClasses
{
    [TestSchema]
    [Documentation("doc")]
    public class Documentation
    { 
        [Documentation("prop doc")]
        public bool Some { get; set; }
    }

    [TestSchema]
    [Permissions(AccessLevel.Vendor,AccessLevel.Vendor,"ADSK")]
    public class Permissions 
    {
        public bool Some { get; set; }
    }

    [TestSchema]
    [Unit(UnitType.UT_Length,DisplayUnitType.DUT_CENTIMETERS)]
    public class DefaultUnit
    {
        public double Length { get; set; }
        public double Width { get; set; }
    }

}
