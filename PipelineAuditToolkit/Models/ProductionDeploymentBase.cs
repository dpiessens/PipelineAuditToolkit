using System;
using System.Collections.Generic;
using System.Linq;

namespace PipelineAuditToolkit.Models
{
    public abstract class ProductionDeploymentBase : IProductionDeployment
    {
        protected ProductionDeploymentBase()
        {
            Changes = new List<ChangeItem>();
        }

        public string Id { get; protected set; }
        public string Name { get; protected set; }
        public string ReleaseNumber { get; protected set; }
        public string BuildNumber { get; set; }
        public string ReleaseNotes { get; protected set; }
        public string BuildName { get; set; }

        public DateTime BuildDate { get; set; }

        public DateTime DeployDate { get; protected set; }

        public IProject DeploymentProject { get; protected set; }
        public IProductionDeployment PreviousDeployment { get; protected set; }
        public HashSet<string> Users { get; protected set; }
        public bool Errored { get; set; }

        public IList<ChangeItem> Changes { get; set; }
        public int? BuildId { get; set; }
        public string CommitId { get; set; }

        public IEnumerable<ChangeItem> GetChangeViolations()
        {
            return Changes.Where(c => Users.Contains(c.UserId));
        }
    }
}