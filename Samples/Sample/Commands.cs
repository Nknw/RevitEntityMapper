using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB.ExtensibleStorage;
using System.Linq;


namespace Sample
{
    public class AddTaskCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            throw new System.Exception();
        }
    }

    public class ShowElementWithUncompletedTasks : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var doc = commandData.Application.ActiveUIDocument.Document;
            var mapper = MapperInstance.Get();
            var filter = new ExtensibleStorageFilter(mapper.GetGuid<Task>());
            var elems = new FilteredElementCollector(doc)
                .WherePasses(filter)
                .Select(mapper.GetEntity<Task>);

        }
    }
}
