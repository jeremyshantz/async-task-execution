using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace tasksync.Models
{
    public class Workflow
    {
        public List<WorkflowAction> Actions { get; set; }

        private List<WorkflowAction> GetExecutingWorkflowActions()
        {
            // omit logic to determine which actions are executing in this cycle
            return this.Actions;
        }

        public void ExecuteAsync(Action<WorkflowActionResult> ReceiveResult, Action initially, Action @finally)
        {
            var task = Task.Factory.StartNew<bool>(() =>
            {
                initially();

                Action<WorkflowAction, bool> PrepareAndSendTempMessage = (action, running) =>
                {
                    var msg = action.ExecutingMessage;
                    msg.ID = new Guid(action.SystemName);
                    msg.TempResult = true;
                    msg.Running = running;
                    ReceiveResult(msg);
                };

                var actions = this.GetExecutingWorkflowActions();

                // Fix the order so that line items don't jump around
                double modifier = 1.0;
                foreach (var actionGroup in actions.GroupBy(action => action.CohortOrder))
                {
                    modifier += 0.0001;
                    foreach (var action in actionGroup)
                    {
                         modifier += 0.00001;
                         action.CohortOrder += modifier;
                    }
                }

                // Send all temp messages now, so the user sees all the actions that need to execute and has an expectation about time
                foreach (var b in actions.Where(i => i.ShowToClient))
                {
                    PrepareAndSendTempMessage(b, false);
                }

                foreach (var actionGroup in actions.GroupBy(b => b.Order))
                {
                    Parallel.ForEach<WorkflowAction>(actionGroup, (WorkflowAction action) =>
                    {
                        // Send the temp message again, this time with the Running flag set to true
                        // This shows that the action has actually started executing
                        if (action.ShowToClient)
                        {
                            PrepareAndSendTempMessage(action, true);
                        }

                        action.progress += (results) =>
                        {
                            foreach (var tempresult in results)
                            {
                                tempresult.ID = new Guid(action.SystemName);
                                ReceiveResult(tempresult);
                            }
                        };

                        var result = action.Execute();
                        result.ID = new Guid(action.SystemName);
                        result.CohortOrder = action.CohortOrder;
                        if (action.ShowToClient)
                        {
                            ReceiveResult(result);
                        }
                    });
                }
                return true;
            }).ContinueWith((a) =>
            {
                @finally();
            });
        }
    }
}