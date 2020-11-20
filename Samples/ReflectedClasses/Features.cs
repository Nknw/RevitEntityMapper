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
    [Permissions(AccessLevel.Public,AccessLevel.Vendor,"ADSK")]
    public class Permissions 
    {
        public bool Some { get; set; }
    }

    [TestSchema]
    public class Unit
    {
        [Unit(UnitType.UT_Length,DisplayUnitType.DUT_CENTIMETERS)]
        public double Length { get; set; }
    }

    public class CustomUnit
    {
        [Unit]
        public double Length { get; set; }
    }

    [TestSchema]
    [Unit(UnitType.UT_Length,DisplayUnitType.DUT_CENTIMETERS)]
    public class DefaultUnit
    {
        public double Length { get; set; }
        public double Width { get; set; }
    }

    [TestSchema]
    [Unit(UnitType.UT_Length, DisplayUnitType.DUT_CENTIMETERS)]
    public class OverrideDefaultUnits
    {
        public double Length { get; set; }
        [Unit(UnitType.UT_Mass,DisplayUnitType.DUT_KILOGRAMS_MASS)]
        public double Width { get; set; }
    }

    [TestSchema]
    public class ExcludeProperty
    {
        public string Some { get; set; }
        [Exclude]
        public double Length { get; set; }
    }
}
