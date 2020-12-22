using System;
using System.Collections.Generic;
using Revit.EntityMapper;
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

        public void AddRemark(string remark)
        {
            Remarks.Add(remark);
            Completed = false;
        }

        public void ToFixed(int pos)
        {
            if (pos >= Remarks.Count)
                return;
            FixedRemarks.Add(Remarks[pos]);
            Remarks.RemoveAt(pos);
            if (Remarks.Count == 0)
                Completed = true;
        }
    }
}
