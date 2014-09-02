using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using System.Threading;

namespace tasksync.Models
{
    public class WorkflowService
    {
        public void SaveAsync(Workflow form, Action<WorkflowActionResult> ReceiveResult)
        {
            var workingid = System.Guid.NewGuid();

            form.ExecuteAsync(ReceiveResult,
                () => 
                {
                    ReceiveResult(new WorkflowActionResult("Working on your request") { ID = workingid, TempResult = true, Running = true, CohortOrder = -10 });
                    var saveid = System.Guid.NewGuid();
                    ReceiveResult(new WorkflowActionResult("Saving") { CohortOrder = -1, ID = saveid, TempResult = true, Running = true });
                    this.SaveToDatabase();
                    ReceiveResult(new WorkflowActionResult("Saved View Request <a href=\"#\">RQ123456</a>") { CohortOrder = -1, ID = saveid });
                },
                () => 
                {
                    var saveid = System.Guid.NewGuid();
                    ReceiveResult(new WorkflowActionResult("Saving") { CohortOrder = 10000, ID = saveid, TempResult = true, Running = true });
                    this.SaveToDatabase();
                    ReceiveResult(new WorkflowActionResult(null) { CohortOrder = -1, ID = workingid }); 
                    ReceiveResult(new WorkflowActionResult("Done") { CohortOrder = 10001, ID = saveid }); 
                });
        }

        private void SaveToDatabase()
        {
            Thread.Sleep(1500);
        }
    }
}