using System;
using System.Collections.Generic;

namespace PipelineAuditToolkit.Models
{
    /// <summary>
    /// Represents a deployment to production
    /// </summary>
    public interface IProductionDeployment
    {
        string Id { get; }

        string Name { get; }

        string ReleaseNumber { get; }

        bool Errored { get; }

        bool HasViolations { get; }

        string BuildNumber { get; set; }
        string ReleaseNotes { get; }
        string BuildName { get; set; }

        IProject DeploymentProject { get; }

        IProductionDeployment PreviousDeployment { get; }

        HashSet<string> Users { get; }
        DateTime BuildDate { get; set; }
        DateTime DeployDate { get;  }
        string DeployUsers { get; }
        IList<ChangeItem> Changes { get; }
        int? BuildId { get; set; }
        string CommitId { get; set; }
        IList<ChangeItem> GetChangeViolations();
        void CheckChangeViolations();
    }
}
