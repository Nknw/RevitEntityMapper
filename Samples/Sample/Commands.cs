﻿using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB.ExtensibleStorage;
using Revit.EntityMapper;
using System.Linq;
using System.Text;
using System;
using System.Collections.Generic;
using Autodesk.Revit.Attributes;

namespace Sample
{
    [Transaction(TransactionMode.ReadOnly)]
    public class ShowElementsWithTaskOnCurrentViewCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uiDoc = commandData.Application.ActiveUIDocument;
            var doc = uiDoc.Document;
            var filter = new ExtensibleStorageFilter(Mapper.GetGuid<Task>());
            var elems = new FilteredElementCollector(doc, commandData.View.Id)
               .WherePasses(filter)
               .ToElementIds();
            uiDoc.Selection.SetElementIds(elems);
            return Result.Succeeded;
        }
    }

    [Transaction(TransactionMode.ReadOnly)]
    public class ReadTasksCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uiDoc = commandData.Application.ActiveUIDocument;
            var tasks = uiDoc.GetSelectedElement()
                .Select(e => (e.GetTask(),e))
                .Where(t => t.Item1 != null);
            var sb = new StringBuilder();
            var pos = 1;
            var selector = GetSelector();
            foreach (var (task, element) in tasks)
            {
                var remarks = selector(task)
                    .OrderBy(r => r);
                if (!remarks.Any())
                    continue;
                sb.AppendLine($"{element.Name} :");
                foreach (var r in remarks)
                {
                    sb.AppendLine($"\t{pos} {r}");
                    pos++;
                }
                sb.AppendLine();
            }
            if (string.IsNullOrWhiteSpace(sb.ToString()))
                sb.AppendLine("Elements have no tasks");
            var dialog = new TaskDialog("Task reader")
            {
                CommonButtons = TaskDialogCommonButtons.Ok,
                MainContent = sb.ToString()
            };
            dialog.Show();
            return Result.Succeeded;
        }

        private Func<Task, IList<string>> GetSelector()
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
    }
}
