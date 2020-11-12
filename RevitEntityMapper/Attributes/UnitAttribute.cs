using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitEntityMapper.Attributes
{
    [AttributeUsage(AttributeTargets.Class|AttributeTargets.Property)]
    public class UnitAttribute:Attribute
    {
        public readonly UnitType UnitType;
        public readonly DisplayUnitType DisplayUnitType;

        public UnitAttribute(UnitType unitType,DisplayUnitType displayUnitType)
        {
            UnitType = unitType;
            DisplayUnitType = displayUnitType;
        }
    }
}
