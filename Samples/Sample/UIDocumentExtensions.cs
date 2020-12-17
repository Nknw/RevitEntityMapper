using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Autodesk.Revit.UI
{
    public static class UIDocumentExtensions
    {
        public static IEnumerable<Element> GetSelectedElement(this UIDocument uIDocument)
            => uIDocument.Selection
                .GetElementIds()
                .Select(id => uIDocument.Document.GetElement(id));
    }
}
