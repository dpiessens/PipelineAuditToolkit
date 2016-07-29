using System;
using System.Linq;
using PipelineAuditToolkit.Providers;
using PipelineAuditToolkit.Utility;
using RazorEngine.Templating;
using RazorEngine;
using PipelineAuditToolkit.Resources;
using RazorEngine.Configuration;

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
                project.Deployments = octopusProvider.GetProductionDeploymentsForProject(project)
                                                 .Where(d => !d.Errored)
                                                 .ToList();

                if (project.Deployments.Count == 0)
                {
                    logger.WriteDebug("No deployments for project, skipping...");
                    continue;
                }

                foreach (var deployment in project.Deployments)
                {
                    // Get matching build and revisions
                    tfsProvider.GetBuildAndRevisions(deployment).Wait();

                    logger.WriteInfo($"Validating Deployment '{deployment.Name}' on {deployment.DeployDate}");
                    logger.WriteInfo($"   Deployment Initiated By User(s): {deployment.Users.Aggregate((a, b) => $"{a},{b}")}");
                    logger.WriteInfo($"   Matching Build: {deployment.BuildNumber} on {deployment.BuildDate}");

                    if (!deployment.HasViolations)
                    {
                        logger.WriteInfo("   Deployment has no violations for changes.");
                    }
                    else
                    {
                        logger.WriteInfo("   Deployment has change violations:");
                        foreach (var changeItem in deployment.GetChangeViolations())
                        {
                            logger.WriteInfo($"     {changeItem.FormattedId} {changeItem.Created.ToShortDateString()} {changeItem.UserId} {changeItem.Message}");
                        }
                    }
                }
            }

            var reportPath = System.IO.Path.Combine(Environment.CurrentDirectory, "TestReport.pdf");
            GenerateReportFile(TemplateResources.DeploymentAuditTemplate, reportPath, projects);

            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private static void GenerateReportFile<TModel>(string templateContent, string outputFile, TModel model)
        {
            var config = new TemplateServiceConfiguration();
            config.DisableTempFileLocking = true;
            config.Language = Language.CSharp;
            
            var service = RazorEngineService.Create(config);

            var templateKey = System.IO.Path.GetFileNameWithoutExtension(outputFile);
            service.Compile(templateContent, templateKey);

            var htmlContent = service.Run(templateKey, null, model);

            var htmlToPdf = new NReco.PdfGenerator.HtmlToPdfConverter();
            htmlToPdf.GeneratePdf(htmlContent, null, outputFile);
            Console.WriteLine("Generated report at: {0}", outputFile);
        }
    }
}
