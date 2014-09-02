using System;
using System.Collections.Generic;

namespace tasksync.Models
{
    public class WorkflowActionResult
    {
        public WorkflowActionResult(string value)
            : this(value, System.Guid.NewGuid())
        {
        }

        public WorkflowActionResult(string value, Guid guid)
        {
            this.Value = value;
            this.ID = guid;
            this.TempResult = false;
        }

        public string Value { get; set; }

        public Guid ID { get; set; }

        public bool TempResult { get; set; }

        public bool Running { get; set; }

        public double CohortOrder { get; set; }

        public List<WorkflowActionResult> Subs { get; set; }

        public override string ToString()
        {
            return string.Format("{0} - {1}", this.Value, this.ID);
        }
    }
}