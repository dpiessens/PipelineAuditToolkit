using System.Collections.Generic;
using System.Linq;
using Octopus.Client;
using Octopus.Client.Model;
using PipelineAuditToolkit.Models;
using PipelineAuditToolkit.Utility;
using System;
using Fclp;

namespace PipelineAuditToolkit.Providers
{
    public class OctopusDeploymentProvider : ProviderBase
    {
        private readonly IBuildMatcher _buildMatcher;
        
        private string _serverAddress;
        private string _apiKey;
        private string _environmentName;
        private IOctopusRepository _octopus;
        private EnvironmentResource _prodEnv;

        public OctopusDeploymentProvider(
            IConfigurationSettings settings,
            ILogger logger, 
            IBuildMatcher buildMatcher,
            IFluentCommandLineParser parser) : 
            base(logger, settings)
        {
            _buildMatcher = buildMatcher;

            SetupOption(parser,
                "octopusUrl",
                "The server URL of the Octopus Deploy server to use.",
                "Octopus.ServerUrl",
                data => _serverAddress = data);

            SetupOption(parser,
                "octopusApiKey",
                "The API Key of the Octopus Deploy server to use.",
                "Octopus.ApiKey",
                data => _apiKey = data);

            SetupOption(parser,
                "octopusEnv",
                "The Octopus envrionment to validate, defaults to Prod",
                "Octopus.ProdEnvName",
                data => _environmentName = data);

            SetupOption(parser,
                "octopusEnv",
                "The Octopus envrionment to validate, defaults to Prod",
                "Octopus.ProdEnvName",
                data => _environmentName = data);
        }

        public void Initialize()
        {
            _octopus = new OctopusRepository(new OctopusServerEndpoint(_serverAddress, _apiKey));
            
            // Get Production Environment
            _prodEnv = _octopus.Environments.FindByName(_environmentName);
            if (_prodEnv == null)
            {
                _logger.WriteError($"Could not find environment '{_environmentName}'");
            }
        }

        public List<IProject> GetProjects()
        {
            var projectGroup = _settings.GetApplicationSetting("Octopus.ProjectGroup");

            List<ProjectResource> items;
            if (projectGroup == null)
            {
                items = _octopus.Projects.FindAll();
            }
            else
            {
                var group = _octopus.ProjectGroups.FindByName(projectGroup);
                if (group == null)
                {
                    _logger.WriteError($"Cannot locate Octopus project group: {projectGroup}");
                    return null;
                }

                items = _octopus.ProjectGroups.GetProjects(group);
            }

            return items.Select(p => new Project(p.Id, p.Name))
                        .OrderBy(p => p.Name)
                        .OfType<IProject>()
                        .ToList();
        }

        public List<IProductionDeployment> GetProductionDeploymentsForProject(IProject project)
        {
            var prodDeployments = new List<IProductionDeployment>();

            var deployments = _octopus.Deployments.FindAll(new[] { project.Id }, new[] { _prodEnv.Id }).Items.OrderBy(d => d.Created);

            foreach (var deployment in deployments)
            {
                // Get releases for each deployment
                var deploymentEvents = _octopus.Events.List(regardingDocumentId: $"{deployment.TaskId},{deployment.Id}");
                
                // Get the release for the deployment
                var release = _octopus.Releases.Get(deployment.ReleaseId);

                var previousDeployment = prodDeployments.LastOrDefault();

                // Create wrapper object
                var deploymentWrapper = new OctopusProductionDeployment(project, deployment, release, deploymentEvents, previousDeployment);

                if (!_buildMatcher.FindMatchingBuild(deploymentWrapper))
                {
                    _logger.WriteError($"Could not find matching build for deployment: {deployment.Name}");
                    deploymentWrapper.Errored = true;
                }
               
                prodDeployments.Add(deploymentWrapper);
            }

            

            return prodDeployments;
        }


        internal class OctopusProductionDeployment : ProductionDeploymentBase
        {
            public OctopusProductionDeployment(IProject project, DeploymentResource deployment, 
                ReleaseResource release, ResourceCollection<EventResource> deploymentEvents, IProductionDeployment previousDeployment)
            {
                // Octopus Objects
                Deployment = deployment;
                Release = release;
                DeploymentEvents = deploymentEvents;

                // Common fields
                DeploymentProject = project;
                Id = deployment.Id;
                Name = deployment.Name;
                DeployDate = deployment.Created.DateTime;
                PreviousDeployment = previousDeployment;
                ReleaseNotes = release.ReleaseNotes;
                Users = new HashSet<string>(deploymentEvents.Items.Select(e => e.Username), StringComparer.InvariantCultureIgnoreCase);
            }
            
            public DeploymentResource Deployment { get; private set; }

            public ResourceCollection<EventResource> DeploymentEvents { get; private set; }

            public ReleaseResource Release { get; private set; }

            internal void MarkPreviousDeployment(IProductionDeployment previousDeployment)
            {
                PreviousDeployment = previousDeployment;
            }
        }
    }
}