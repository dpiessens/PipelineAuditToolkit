using PipelineAuditToolkit.Models;

namespace PipelineAuditToolkit.Providers
{
    public interface IBuildMatcher
    {
        bool FindMatchingBuild(IProductionDeployment deployment);
    }
}