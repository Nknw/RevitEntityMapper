using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB.ExtensibleStorage;
using System.Linq;
using System.Text;
using System;
using System.Collections.Generic;

namespace Sample
{
    public class ShowElementWithUncompletedOnCurrentViewTasksCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uiDoc = commandData.Application.ActiveUIDocument;
            var doc = uiDoc.Document;
            var mapper = MapperInstance.Get();
            var filter = new ExtensibleStorageFilter(mapper.GetGuid<Task>());
            var elems = new FilteredElementCollector(doc,commandData.View.Id)
               .WherePasses(filter)
               .ToElementIds();
            uiDoc.Selection.SetElementIds(elems);
            return Result.Succeeded;
        }
    }

    public class ReadTasksCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uiDoc = commandData.Application.ActiveUIDocument;
            var mapper = MapperInstance.Get();
            var tasks = uiDoc.GetSelectedElement()
                .Select(e =>
                {
                    if (mapper.TryGetEntity<Task>(e, out var task))
                        return (task,e);
                    return default;
                }).Where(t=>t.task != null);
            var sb = new StringBuilder();
            var pos = 1;
            foreach(var (task,element) in tasks)
            {
                var remarks = selector(task)
                    .OrderBy(r => r);
                if (!remarks.Any())
                    continue;
                sb.AppendLine($"{element.Name} :");
                foreach (var r in remarks)
                    sb.AppendLine($"\t{pos} {r}");
                sb.AppendLine();
                pos++;
            }
            var dialog = new TaskDialog("Task reader")
            {
                CommonButtons = TaskDialogCommonButtons.Ok,
                MainContent = sb.ToString()
            };
            dialog.Show();
            return Result.Succeeded;
        }

        private Func<Task,IList<string>> GetSelector()
        {
            var statusTaskDialog = new TaskDialog("Task reader")
            {
                CommonButtons = TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No,
                MainContent = "Show completed task?"
            };
            var result = statusTaskDialog.Show();
            if (result == TaskDialogResult.Yes)
                return task => task.FixedRemarks;
            return task => task.Remarks;
        }
