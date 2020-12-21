using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.UI;
using System.Reflection;
using Autodesk.Revit.UI.Events;
using Autodesk.Revit.DB;

namespace Sample
{
    public class Application : IExternalApplication
    {
        public Result OnShutdown(UIControlledApplication application)
            => Result.Succeeded;

        public Result OnStartup(UIControlledApplication application)
        {
            var location = Assembly.GetExecutingAssembly().Location;
            application.CreateRibbonTab("Sample");
            var panel = application.CreateRibbonPanel("Sample","Task scheduler");
            panel.AddItem(new PushButtonData("Show", "Show", location,
                typeof(ShowElementsWithTaskOnCurrentViewCommand).FullName));
            panel.AddItem(new PushButtonData("Read", "Read", location,
                typeof(ReadTasksCommand).FullName));
            var tb = panel.AddItem(new TextBoxData("Add")) as TextBox;
            tb.EnterPressed += AddRemark;
            var remtb = panel.AddItem(new TextBoxData("Fix") 
                { 
                    ToolTip = "Enter position of remark to set fixed"
                }) as TextBox;
            remtb.EnterPressed += FixRemark;
            return Result.Succeeded;
        }

        private void FixRemark(object sender, TextBoxEnterPressedEventArgs e)
        {
            var uiDoc = e.Application.ActiveUIDocument;
            var selected = uiDoc.Selection.GetElementIds();
            var tb = sender as TextBox;
            if (selected.Count != 1)
                LogError("Select one element");
            else if (!int.TryParse(tb.Value as string, out var position))
                LogError("Enter integer number");
            else
            {
                var element = uiDoc.Document
                    .GetElement(selected.First());
                var task = element.GetTask();
                tb.Value = "";
                if (task == null)
                    LogError("Element has no task");
                else
                {
                    task.ToFixed(position - 1);
                    element.SetTask(task);
                }
            }
        }

        private void LogError(string body)
        {
            var dialog = new TaskDialog("Error")
            {
                MainContent = body
            };
            dialog.Show();
        }

        private void AddRemark(object sender, TextBoxEnterPressedEventArgs e)
        {
            var tb = sender as TextBox;
            var uiDoc = e.Application.ActiveUIDocument;
            var selectedElements = uiDoc.GetSelectedElement();
            foreach (var element in selectedElements)
                element.UpdateTask(t => t.AddRemark(tb.Value as string));
            tb.Value = "";
        }
    }
}
