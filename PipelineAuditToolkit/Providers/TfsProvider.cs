using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using PipelineAuditToolkit.Models;
using PipelineAuditToolkit.Utility;

namespace PipelineAuditToolkit.Providers
{
    public class TfsProvider : IDisposable
    {
        private readonly IConfigurationSettings _settings;
        private readonly ILogger _logger;

        private BuildHttpClient _buildClient;
        private Guid _projectId;
        private GitHttpClient _gitClient;


        public TfsProvider(IConfigurationSettings settings, ILogger logger)
        {
            _settings = settings;
            _logger = logger;
            _projectId = Guid.Empty;
        }

        public void Initialize()
        {
            var tfsUrl = _settings.GetApplicationSetting("Tfs.ServerUrl");
            var tfsUser = _settings.GetApplicationSetting("Tfs.User");
            var tfsApiKey = _settings.GetApplicationSetting("Tfs.ApiKey");
            var tfsProject = _settings.GetApplicationSetting("Tfs.Project");

            var creds = new VssCredentials(new VssBasicCredential(tfsUser, tfsApiKey))
            {
                PromptType = CredentialPromptType.DoNotPrompt
            };

            _logger.WriteDebug($"Getting TFS Project ID for name: {tfsProject}");
            using (var generalClient = new ProjectHttpClient(new Uri(tfsUrl), creds))
            {
                var project = generalClient.GetProjects().Result.FirstOrDefault(p => string.Equals(p.Name, tfsProject));
                if (project == null)
                {
                    _logger.WriteError($"ERROR Cannot get project name: {tfsProject}");
                    return;
                }

                _projectId = project.Id;
            }

            var tfsUri = new Uri(tfsUrl);
            _buildClient = new BuildHttpClient(tfsUri, creds);
            _gitClient = new GitHttpClient(tfsUri, creds);
        }

        public void Dispose()
        {
            if (_buildClient != null)
            {
                _buildClient.Dispose();
                _buildClient = null;
            }

            if (_gitClient != null)
            {
                _gitClient.Dispose();
                _gitClient = null;
            }
        }

        public async Task GetBuildAndRevisions(IProductionDeployment deployment)
        {
            var definitions =
                await _buildClient.GetDefinitionsAsync(_projectId, deployment.BuildName, DefinitionType.Build);
            var definition = definitions.FirstOrDefault();

            if (definition == null)
            {
                _logger.WriteError($"Cannot locate build definition: {deployment.BuildName}");
                return;
            }

            // Get the build relating
            var build = await GetBuildForDeployment(definition, deployment);
            if (build == null)
            {
                return;
            }

            deployment.BuildDate = build.FinishTime.GetValueOrDefault();
            deployment.BuildId = build.Id;
            deployment.CommitId = build.SourceVersion;

            var previousBuild = deployment.PreviousDeployment.BuildId;

            List<Change> changes;
            try
            {
                if (previousBuild == null || previousBuild == build.Id)
                {
                    changes = await _buildClient.GetBuildChangesAsync(_projectId, build.Id, includeSourceChange: true);
                }
                else
                {
                    changes = await _buildClient.GetChangesBetweenBuildsAsync(_projectId, previousBuild, build.Id);
                }
            }
            catch (BuildNotFoundException)
            {
                // As a backup get the raw commits between the two versions
                var sourceChanges = await GetChangesByRevisions(build.Repository.Id, deployment.PreviousDeployment?.CommitId, deployment.CommitId);
                sourceChanges.ForEach(c => deployment.Changes.Add(c));
                return;
            }

            foreach (var change in changes)
            {
                deployment.Changes.Add(new ChangeItem(change.Id, change.Timestamp.GetValueOrDefault(), change.Author.UniqueName, change.Message));
            }
        }

        private async Task<Build> GetBuildForDeployment(ShallowReference definition, IProductionDeployment deployment)
        {
            List<Build> builds;
            try
            {
                builds = await _buildClient.GetBuildsAsync(
                    _projectId, new[] {definition.Id},
                    buildNumber: deployment.BuildNumber,
                    deletedFilter: QueryDeletedOption.IncludeDeleted);
            }
            catch (BuildNotFoundException)
            {
                _logger.WriteError($"Cannot locate build number: {deployment.BuildNumber}");
                return null;
            }

            var build = builds.FirstOrDefault();
            if (build == null)
            {
                _logger.WriteError($"Cannot locate build number: {deployment.BuildNumber}");
                return null;
            }
            return build;
        }


        private async Task<List<ChangeItem>> GetChangesByRevisions(string repositoryId, string previousRevisionId, string revisionId)
        {
            var query = new GitQueryCommitsCriteria {ToCommitId = revisionId};

            if (!string.IsNullOrEmpty(previousRevisionId))
            {
                query.FromCommitId = previousRevisionId;
            }

            var commits = await _gitClient.GetCommitsAsync(_projectId, repositoryId, query);

            return commits.Select(c => new ChangeItem(c.CommitId, c.Author.Date, c.Author.Email, c.Comment)).ToList();
        }
    }
}