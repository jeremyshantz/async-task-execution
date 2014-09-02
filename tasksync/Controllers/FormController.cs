using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using tasksync.Models;

namespace tasksync.Controllers
{
    public class FormController : Controller
    {
        public void InsertResultToCache(string coordinationid, WorkflowActionResult result)
        {
            var dictionary = (HttpContext.Cache[coordinationid] as ConcurrentDictionary<Guid, WorkflowActionResult>) ?? new ConcurrentDictionary<Guid, WorkflowActionResult>();

            if (result != null)
            {
                dictionary.AddOrUpdate(result.ID, result, (key, oldvalue) => { return result; });
            }

            HttpContext.Cache.Add(coordinationid, dictionary, null,
                DateTime.Now.AddMonths(1),
                System.Web.Caching.Cache.NoSlidingExpiration,
                System.Web.Caching.CacheItemPriority.Normal, null);
        }

        public ActionResult SubmitJson(int? id)
        {
            if (!id.HasValue)
            {
                id = 0;
            }

            var coordinationid = System.Guid.NewGuid().ToString();

            Action<WorkflowActionResult> handleResult = (WorkflowActionResult a) => { InsertResultToCache(coordinationid, a); };

            // prime the cache
            handleResult(null);

            new WorkflowService().SaveAsync(new Workflow { Actions = GetActions(id.Value) }, handleResult);

            return Json(new SubmitViewModel { ID = coordinationid });
        }

        public ActionResult Submit(int? id)
        {
            if (!id.HasValue)
            {
                id = 0;
            }

            var coordinationid = System.Guid.NewGuid().ToString();

            Action<WorkflowActionResult> handleResult = (WorkflowActionResult a) => { InsertResultToCache(coordinationid, a); };

            // prime the cache
            handleResult(null);

            new WorkflowService().SaveAsync(new Workflow { Actions = GetActions(id.Value) }, handleResult);

            return View(new SubmitViewModel { ID = coordinationid });
        }


        /// <summary>
        /// This is test data
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private List<WorkflowAction> GetActions(int id)
        {
            var forms = new List<WorkflowAction>[] {
            
                new List<WorkflowAction>
                {
                    new WorkflowAction("Creating ticket", "Ticket created <a href=\"#\">T00000000</a>", 2000, 2)
                    {
                        InnerBehaviours = new List<WorkflowAction>
                        {
                            new WorkflowAction("TaskA Executing", "TaskA Complete", 750, 1),
                            new WorkflowAction("TaskB Executing", "TaskB Complete", 1000, 1),
                            new WorkflowAction("TaskC Executing", "TaskC Complete", 750, 1),
                            new WorkflowAction("TaskD Executing", "TaskD Complete", 750, 1){ ShowToClient = false},
                        }
                    },
                    new WorkflowAction("Seeking approval", "Approval requested from your manager", 2000, 3),                    
                    new WorkflowAction("Sending email", "Email sent to A", 1000, 3),
                    new WorkflowAction("Sending email", "Email sent to B", 1000, 3),
                    new WorkflowAction("Sending email", "Email sent to C", 4000, 3){ ShowToClient = false},
                    new WorkflowAction("Creating dl", "DL created", 1200, 5)
                },
                new List<WorkflowAction>
                {
                    new WorkflowAction("Creating ticket", "Ticket created <a href=\"#\">T00000000</a>", 3000, 2),
                    new WorkflowAction("Seeking approval", "Approval requested from your manager", 6000, 3){ ShowToClient = false},
                    new WorkflowAction("Sending email", "Email sent to A", 3000, 3){ ShowToClient = false},
                    new WorkflowAction("Sending email", "Email sent to B", 3000, 3){ ShowToClient = false},
                    new WorkflowAction("Sending email", "Email sent to C", 4000, 3){ ShowToClient = true},
                    new WorkflowAction("Creating dl", "DL created", 3000, 5){ ShowToClient = false}
                },
                new List<WorkflowAction>
                {
                    new WorkflowAction("Creating ticket", "Ticket created <a href=\"#\">T00000000</a>", 3000, 2),
                    new WorkflowAction("Seeking approval", "Approval requested from your manager", 6000, 3),
                    new WorkflowAction("Sending email", "Email sent to A", 3000, 3)
                }
            };

            if (id > forms.Length - 1)
            {
                id = 0;
            }

            return forms[id];
        }

        public ActionResult ResultsJson(string id)
        {
            var results = (HttpContext.Cache[id] as ConcurrentDictionary<Guid, WorkflowActionResult>) ?? new ConcurrentDictionary<Guid, WorkflowActionResult>();

            return Json(results.Select(r => r.Value).OrderBy(x => x.CohortOrder).ToList());
        }

        public ActionResult Results(string id)
        {
            var results = (HttpContext.Cache[id] as ConcurrentDictionary<Guid, WorkflowActionResult>) ?? new ConcurrentDictionary<Guid, WorkflowActionResult>();

            return PartialView(results.Select(r => r.Value).OrderBy(x => x.CohortOrder).ToList());
        }
    }
}