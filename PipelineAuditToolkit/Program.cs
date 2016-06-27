using System;
using System.Linq;
using PipelineAuditToolkit.Providers;
using PipelineAuditToolkit.Utility;

namespace PipelineAuditToolkit
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Audit Report Builder");
            Console.WriteLine();
            Console.WriteLine("Initializing...");
            Console.WriteLine();

            var config = new ConfigurationSettings();
            var logger = new Logger(false, false);

            var buildMatcher = new RegexReleaseNotesBuildMatcher(logger);
            var octopusProvider = new OctopusDeploymentProvider(config, logger, buildMatcher);
            var tfsProvider = new TfsProvider(config, logger);
            
            octopusProvider.Initialize();
            tfsProvider.Initialize();

            // Get projects
            var projects = octopusProvider.GetProjects();
            foreach (var project in projects)
            {
                Console.WriteLine();
                logger.WriteInfo($"Checking Deployments for project: {project.Name}\n");
                var deployments = octopusProvider.GetProductionDeploymentsForProject(project);

                foreach (var deployment in deployments.Where(d => !d.Errored))
                {
                    // Get matching build and revisions
                    tfsProvider.GetBuildAndRevisions(deployment).Wait();

                    logger.WriteInfo($"Validating Deployment '{deployment.Name}' on {deployment.DeployDate}");
                    logger.WriteInfo($"   Deployment Initiated By User(s): {deployment.Users.Aggregate((a, b) => $"{a},{b}")}");
                    logger.WriteInfo($"   Matching Build: {deployment.BuildNumber} on {deployment.BuildDate}");

                    var violations = deployment.GetChangeViolations().ToList();
                    if (!violations.Any())
                    {
                        logger.WriteInfo("   Deployment has no violations for changes.");
                    }
                    else
                    {
                        logger.WriteInfo("   Deployment has change violations:");
                        foreach (var changeItem in violations)
                        {
                            logger.WriteInfo($"     {changeItem.Id.Substring(0, 8)} {changeItem.Created.ToShortDateString()} {changeItem.UserId} {changeItem.Message}");
                        }
                    }
                }
                
            }  
            
            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
