using System;
using System.Collections.Generic;
using Autodesk.Revit.Mapper;
using Autodesk.Revit.DB.ExtensibleStorage;

namespace Sample
{
    [Schema("3ef20639-1768-49c0-8cf3-ef4c6f717369", "Task")]
    [Documentation("Task with list of remarks")]
    [Permissions(AccessLevel.Public,AccessLevel.Vendor,"TaskScheduler")]
    public class Task
    {
        public bool Completed { get; set; }
        [Documentation("Remarks requiring correction")]
        public IList<string> Remarks { get; set; }
        public IList<string> FixedRemarks { get; set; }

        public static readonly Guid Guid = new Guid("3ef20639-1768-49c0-8cf3-ef4c6f717369");
    }
}
