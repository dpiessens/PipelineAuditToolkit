using System;
using System.Linq;
using PipelineAuditToolkit.Providers;
using PipelineAuditToolkit.Utility;
using RazorEngine.Templating;
using RazorEngine;
using PipelineAuditToolkit.Resources;
using RazorEngine.Configuration;
using System.Diagnostics;
using Fclp;

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

            var globalOptions = new Options();
            var parser = GetCommandLineParser(globalOptions);

            var buildMatcher = new RegexReleaseNotesBuildMatcher(logger);
            var octopusProvider = new OctopusDeploymentProvider(config, logger, buildMatcher, parser);
            var tfsProvider = new TfsProvider(config, logger, parser);

            // Try to parse the command line
            var result = parser.Parse(args);

            if (result.HasErrors)
            {
                Console.WriteLine(result.ErrorText);
                return;
            }

            if (result.HelpCalled)
            {
                // triggers the SetupHelp Callback which writes the text to the console
                Console.WriteLine("Press any key to continue ...");
                Console.ReadKey();
                return;
            }

            octopusProvider.Initialize();
            tfsProvider.Initialize();

            // Get projects
            var projects = octopusProvider.GetProjects();

            // Filter projects if needed
            if (!string.IsNullOrWhiteSpace(globalOptions.ProjectFilter))
            {
                projects = projects.Where(p => p.Name == globalOptions.ProjectFilter).ToList();
            }

            foreach (var project in projects)
            {
                Console.WriteLine();
                logger.WriteInfo($"Checking Deployments for project: {project.Name}\n");
                project.Deployments = octopusProvider.GetProductionDeploymentsForProject(project, globalOptions.StartDate, globalOptions.EndDate)
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
                    logger.WriteInfo($"   Getting details for deployment {deployment.Id} '{deployment.Name}'");
                    tfsProvider.GetBuildAndRevisions(deployment).Wait();
                }
            }

            var reportPath = System.IO.Path.Combine(Environment.CurrentDirectory, String.Format("PipelineReport_{0}", DateTime.Now.ToString("yyyyMMddHHmmssfff")));
            var reportPath = System.IO.Path.Combine(Environment.CurrentDirectory, "PipelineReport.pdf");
            var viewModel = new DeploymentAuditTemplateViewModel
            {
                EndDate = globalOptions.EndDate,
                Projects = projects,
                StartDate = globalOptions.StartDate
            };

            GenerateReportFile(TemplateResources.DeploymentAuditTemplate, reportPath, projects, logger, globalOptions.ShowReport);
        }

        private static void GenerateReportFile<TModel>(string templateContent, string outputFile, TModel model, ILogger logger, bool showReport)
        {
            const string GridStylesCss = "GridStyles";
            var findString = $"<!-- import {GridStylesCss}.css -->";
            if (templateContent.Contains(findString))
            {
                var importContent = TemplateResources.GridStyles.Replace("@", "@@");
                var replaceContent = $"<style>\r\n{importContent}\r\n</style>";
                templateContent = templateContent.Replace(findString, replaceContent);
            }

            var config = new TemplateServiceConfiguration
            {
                DisableTempFileLocking = true,
                Language = Language.CSharp,
                CachingProvider = new DefaultCachingProvider(t => { })
            };

            var service = RazorEngineService.Create(config);

            var templateKey = System.IO.Path.GetFileNameWithoutExtension(outputFile);
            service.Compile(templateContent, templateKey);

            var htmlContent = service.Run(templateKey, null, model);

            var htmlToPdf = new NReco.PdfGenerator.HtmlToPdfConverter
            {
                Grayscale = false,
                Margins = new NReco.PdfGenerator.PageMargins
                {
                    Bottom = 4,
                    Top = 4,
                    Left = 5,
                    Right = 5
                }
            };

            // To write to PDF
            outputFile = outputFile + ".pdf";
            htmlToPdf.GeneratePdf(htmlContent, null, outputFile);

            // To write to HTML
            //outputFile = outputFile + ".htm";
            //System.IO.File.WriteAllText(outputFile, htmlContent);


            logger.WriteInfo($"Generated report at: {outputFile}");

            if (showReport)
            {
                Process.Start(outputFile);
            }
        }

        private static IFluentCommandLineParser GetCommandLineParser(Options globalOptions)
        {
            var parser = new FluentCommandLineParser { IsCaseSensitive = false };

            parser.Setup<string>('o', "output")
                .SetDefault(Environment.CurrentDirectory)
                .WithDescription("The path to use when generating the report.")
                .Callback(o => globalOptions.ReportPath = o);

            parser.Setup<bool>('s', "showReport")
                .WithDescription("Indicates wither to display the report when completed.")
                .Callback(o => globalOptions.ShowReport = o);

            parser.Setup<string>('f', "projectFilter")
                .WithDescription("Filters the list of projects to query by down to a specific name")
                .Callback(o => globalOptions.ProjectFilter = o);

            parser.Setup<DateTime?>("startDate")
                .WithDescription("The start date for only reporting on a range of deployments")
                .Callback(o => globalOptions.StartDate = o);

            parser.Setup<DateTime?>("endDate")
                .WithDescription("The end date for only reporting on a range of deployments")
                .Callback(o => globalOptions.EndDate = o);

            parser.SetupHelp("?", "help").Callback(text => Console.WriteLine(text));
                        
            return parser;
        }
    }
}
