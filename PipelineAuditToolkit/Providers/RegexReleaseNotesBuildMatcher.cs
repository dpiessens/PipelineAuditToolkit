using System.Text.RegularExpressions;
using PipelineAuditToolkit.Models;
using PipelineAuditToolkit.Utility;

namespace PipelineAuditToolkit.Providers
{
    public class RegexReleaseNotesBuildMatcher : IBuildMatcher
    {
        private const string MatchRegex = @"Release created by Build \[(?<buildname>[A-Za-z _0-9\-]+)\#(?<buildnumber>[0-9.]+)\]";

        private readonly ILogger _logger;
        private readonly Regex _releaseLocateRegex;

        public RegexReleaseNotesBuildMatcher(ILogger logger)
        {
            _logger = logger;
            _releaseLocateRegex = new Regex(MatchRegex, RegexOptions.Singleline);
        }

        public bool FindMatchingBuild(IProductionDeployment deployment)
        {
            if (string.IsNullOrWhiteSpace(deployment.ReleaseNotes))
            {
                _logger.WriteError("Deployment has no release notes, cannot match build");
                return false;
            }

            var match = _releaseLocateRegex.Match(deployment.ReleaseNotes);
            if (!match.Success)
            {
                _logger.WriteError("Cannot locate build for Id in release");
                return false;
            }

            deployment.BuildNumber = match.Groups["buildnumber"].Value.Trim();
            deployment.BuildName = match.Groups["buildname"].Value.Trim();

            return true;
        }
    }
}