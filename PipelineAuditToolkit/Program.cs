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
using System.Text;

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

            var usernameTransformer = new UsernameTransformer(config, logger);
            var buildMatcher = new RegexReleaseNotesBuildMatcher(logger);
            var octopusProvider = new OctopusDeploymentProvider(config, logger, buildMatcher, parser, usernameTransformer);

            // Try to parse the command line
            var result = parser.Parse(args);

            if (result.HasErrors)
            {
                Console.WriteLine(result.ErrorText);
                Console.Read();
                return;
            }

            if (result.HelpCalled)
            {
                // triggers the SetupHelp Callback which writes the text to the console
                Console.WriteLine("Press any key to continue ...");
                Console.ReadKey();
                return;
            }

            usernameTransformer.Initalize();
            octopusProvider.Initialize();

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
            }

            var reportPath = System.IO.Path.Combine(Environment.CurrentDirectory, string.Format("PipelineReport_{0}", DateTime.Now.ToString("yyyyMMddHHmmssfff")));
            var viewModel = new DeploymentAuditTemplateViewModel
            {
                EndDate = globalOptions.EndDate,
                Projects = projects,
                StartDate = globalOptions.StartDate
            };

            GenerateReportFile(TemplateResources.DeploymentAuditTemplate, reportPath, viewModel, logger, globalOptions.ShowReport);
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

            parser.Setup<DateTime>("startDate")
                .WithDescription("The start date for only reporting on a range of deployments")
                .Callback(o => globalOptions.StartDate = o);

            parser.Setup<DateTime>("endDate")
                .WithDescription("The end date for only reporting on a range of deployments")
                .Callback(o => globalOptions.EndDate = o.ToEndOfDay());

            parser.SetupHelp("h", "?", "help").Callback(text => Console.WriteLine(GenerateHelpText(text)));

            return parser;
        }

        private static string GenerateHelpText(string commandLineOptions)
        {
            var builder = new StringBuilder("Delivery Pipeline Toolkit");
            builder.AppendLine("Generates reports documenting SoC for a pipeline");
            builder.AppendLine();
            builder.Append(commandLineOptions);
            builder.AppendLine();
            builder.AppendLine("Example:");
            builder.AppendLine("PipelineAuditToolikit.exe /octopusUrl https://my.octopusserver.com /octopusApiKey API-ABC123 /tfsUrl https://my.visualstudio.com /tfsApiKey abc123 /tfsProject \"My Project\"");

            return builder.ToString();
        }
    }
}
