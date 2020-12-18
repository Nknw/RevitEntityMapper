using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Sample;

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

namespace Autodesk.Revit.DB
{
    public static class ElementExtensions
    {
        public static void SetTask(this Element element, Task task)
            => MapperInstance.Get().SetEntity(element, task);

        public static Task GetTask(this Element element)
            => MapperInstance.Get().GetEntity(element);

        public static void UpdateTask(this Element element,Action<Task> update)
        {
            var mapper = MapperInstance.Get();
            var task = mapper.GetEntity(element) ?? new Task()
            {
                Remarks = new List<string>(),
                FixedRemarks = new List<string>()
            };
            update(task);
            mapper.SetEntity(element, task);
        }
    }
}
