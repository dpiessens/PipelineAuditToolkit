using System.Collections.Generic;
using System.Linq;
using Octopus.Client;
using Octopus.Client.Model;
using PipelineAuditToolkit.Models;
using PipelineAuditToolkit.Utility;

namespace PipelineAuditToolkit.Providers
{
    public class OctopusDeploymentProvider
    {
        private readonly RegexReleaseNotesBuildMatcher _buildMatcher;
        private readonly ILogger _logger;
        private readonly IConfigurationSettings _settings;

        private IOctopusRepository _octopus;
        private EnvironmentResource _prodEnv;

        public OctopusDeploymentProvider(IConfigurationSettings settings, ILogger logger, 
            RegexReleaseNotesBuildMatcher buildMatcher)
        {
            _settings = settings;
            _logger = logger;
            _buildMatcher = buildMatcher;
        }

        public void Initialize()
        {
            var serverAddress = _settings.GetApplicationSetting("Octopus.ServerUrl");
            var apiKey = _settings.GetApplicationSetting("Octopus.ApiKey");
            var prodEnvName = _settings.GetApplicationSetting("Octopus.ProdEnvName");
            _octopus = new OctopusRepository(new OctopusServerEndpoint(serverAddress, apiKey));

            // Get Production Environment
            _prodEnv = _octopus.Environments.FindByName(prodEnvName);
            if (_prodEnv == null)
            {
                _logger.WriteError($"Could not find environment '{prodEnvName}'");
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
                Users = new HashSet<string>(deploymentEvents.Items.Select(e => e.Username));
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