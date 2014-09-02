using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace tasksync.Models
{
    public delegate void WorkflowProgress(List<WorkflowActionResult> results);

    public class Manager
    {
        private Action<List<WorkflowActionResult>> OnProgress;

        public Manager(Action<List<WorkflowActionResult>> OnProgress)
        {
            this.OnProgress = OnProgress;
        }

        public WorkflowActionResult Parent { get; set; }

        ConcurrentDictionary<Guid, WorkflowActionResult> subs = new ConcurrentDictionary<Guid, WorkflowActionResult>();

        public List<WorkflowActionResult> ExecuteWithProgress(List<WorkflowAction> behaviours, WorkflowActionResult parent)
        {
            if (behaviours == null || behaviours.Count == 0)
            {
                return null;
            }

            this.Parent = parent;
            var results = new List<WorkflowActionResult>();

            var cohortorder = .01;
            behaviours.ForEach(b =>
                {
                    b.CohortOrder = b.Order + cohortorder;
                    cohortorder = cohortorder + .01;
                });

            behaviours.Where(b => b.ShowToClient).ToList().ForEach(b => this.Add(b, false));
            OnProgress(new List<WorkflowActionResult> { this.Get() });

            foreach (var behaviour in behaviours)
            {
                if (behaviour.ShowToClient)
                {
                    var guid = new System.Guid(behaviour.SystemName);
                    this.Add(behaviour, true);
                    var result = behaviour.Execute();
                    result.CohortOrder = behaviour.CohortOrder;
                    results.Add(result);
                    this.Replace(guid, result); 
                }
                else
                {
                    var guid = this.Add(new WorkflowActionResult("Working") {  CohortOrder = behaviour.CohortOrder});
                    var result = behaviour.Execute();
                    // do not add to returned collection
                    // null out the result
                    this.Replace(guid, null); 
                }
            }

            return results;
        }

        public Guid Add(WorkflowActionResult result)
        {
            result.TempResult = true;
            result.Running = true;

            var guid = System.Guid.NewGuid();
            this.Replace(guid, result);
            return guid;
        }

        public void Add(WorkflowAction behaviour, bool running)
        {
            var result = behaviour.ExecutingMessage;
            result.TempResult = true;
            result.Running = running;
            result.CohortOrder = behaviour.CohortOrder;

            this.Replace(new Guid(behaviour.SystemName), result);
        }

        public void Replace(Guid guid, WorkflowActionResult result)
        {
            if (result == null)
            {
                WorkflowActionResult outvalue;
                subs.TryRemove(guid, out outvalue);
            }
            else
            {
                subs.AddOrUpdate(guid, result, (g, a) => { return result; });
            }
            
            OnProgress(new List<WorkflowActionResult> { this.Get() });
        }

        private WorkflowActionResult Get()
        {
            var parent = this.Parent;
            parent.Subs = subs.Select(s => s.Value).ToList();
            return parent;
        }
    }

    public class WorkflowAction
    {
        public event WorkflowProgress progress;

        public void OnProgress(List<WorkflowActionResult> results)
        {
            if (this.progress != null)
            {
                this.progress(results);
            }
        }

        public WorkflowAction(string executingmessage, string resultmessage, int sleepmiliseconds, int order)
        {
            this.ExecutingMessage = new WorkflowActionResult(executingmessage) { CohortOrder = order };
            this.ResultMessage = new WorkflowActionResult(resultmessage) { CohortOrder = order };
            this.SleepTime = sleepmiliseconds;
            this.Order = order;
            this.SystemName = System.Guid.NewGuid().ToString();
            this.ShowToClient = true;
            this.InnerBehaviours = new List<WorkflowAction>();
        }

        public double Order { get; set; }
        
        public Guid CohertID { get; set; }

        public double CohortOrder { get; set; }

        public int SleepTime { get; set; }

        public WorkflowActionResult ExecutingMessage { get; set; }

        private WorkflowActionResult ResultMessage { get; set; }

        public bool ShowToClient{ get; set; }

        public List<WorkflowAction> InnerBehaviours { get; set; }

        public WorkflowActionResult Execute()
        {
            var subs = new List<WorkflowActionResult>();

            if (this.ShowToClient)
            {
                subs = new Manager(this.OnProgress).ExecuteWithProgress(this.InnerBehaviours, this.ExecutingMessage);
            }

            Thread.Sleep(this.SleepTime);
            this.ResultMessage.Subs = subs;
            this.ResultMessage.CohortOrder = this.CohortOrder;
            return this.ResultMessage;
        }

        public override string ToString()
        {
            return this.ResultMessage.Value;
        }

        public string SystemName { get; set; }
    }
}