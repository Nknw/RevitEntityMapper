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
    [Schema("4961cf0c-a44b-4910-9140-39af78a961a4", "test")]
    [Documentation("doc")]
    public class Documentation
    { 
        [Documentation("prop doc")]
        public bool Some { get; set; }
    }

    [Schema("6ff2cb8a-e6d0-47b9-9eb9-9ca19d26cd1b", "test")]
    [Permissions(AccessLevel.Public,AccessLevel.Vendor,"ADSK")]
    public class Permissions 
    {
        public bool Some { get; set; }
    }

    [Schema("2885738d-5a49-4c5b-8792-043622ffc9ce", "test")]
    public class Unit
    {
        [Unit(UnitType.UT_Length,DisplayUnitType.DUT_CENTIMETERS)]
        public double Length { get; set; }
    }

    [Schema("189c06ab-b577-49b4-a06f-c198ebcbd9a7", "test")]
    public class CustomUnit
    {
        [Unit]
        public double Length { get; set; }
    }

    [Schema("12705a47-a614-413a-b01e-bcba9e0d2a50", "test")]
    [Unit(UnitType.UT_Length,DisplayUnitType.DUT_CENTIMETERS)]
    public class DefaultUnit
    {
        public double Length { get; set; }
        public double Width { get; set; }
    }

    [Schema("a4d0b7d0-ec18-4b17-93e2-84103bde53b1", "test")]
    [Unit(UnitType.UT_Length, DisplayUnitType.DUT_CENTIMETERS)]
    public class OverrideDefaultUnits
    {
        public double Length { get; set; }
        [Unit(UnitType.UT_Mass,DisplayUnitType.DUT_KILOGRAMS_MASS)]
        public double Width { get; set; }
    }

    [Schema("e9f4f284-cbbd-4e8d-8f6d-817f7ed90499", "test")]
    public class ExcludeProperty
    {
        public string Some { get; set; }
        [Exclude]
        public double Length { get; set; }
    }
}
