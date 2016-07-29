using System.Collections.Generic;

namespace PipelineAuditToolkit.Models
{
    public interface IProject
    {
        string Id { get; }

        string Name { get; }

        List<IProductionDeployment> Deployments { get; set; }
    }
}