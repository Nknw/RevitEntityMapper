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
        {
            using (var tr = new Transaction(element.Document,"set task"))
            {
                tr.Start();
                MapperInstance.Get().SetEntity(element, task);
                tr.Commit();
            }
        }

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
            using (var tr = new Transaction(element.Document,"set task"))
            {
                tr.Start();
                mapper.SetEntity(element, task);
                tr.Commit();
            }
        }

        public static IEnumerable<Element> GetElements(this Document doc,IEnumerable<ElementId> ids)
        {
            foreach (var id in ids)
                yield return doc.GetElement(id);
        }
    }
}
