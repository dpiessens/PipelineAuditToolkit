using PipelineAuditToolkit.Utility;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace PipelineAuditToolkit.Providers
{
    public class UsernameTransformer : IUsernameTransformer
    {
        private const string AppSetting = "UserNameTransformer.Translations";

        private readonly IConfigurationSettings _configurationSettings;
        private readonly ILogger _logger;
        private readonly List<Tuple<Regex, string>> _transformations;

        public UsernameTransformer(IConfigurationSettings configurationSettings, ILogger logger)
        {
            _configurationSettings = configurationSettings;
            _logger = logger;
            _transformations = new List<Tuple<Regex, string>>();
        }

        public int RuleCount
        {
            get
            {
                return _transformations.Count;
            }
        }

        public void Initalize()
        {
            var settingData = _configurationSettings.GetApplicationSetting(AppSetting);
            if (string.IsNullOrWhiteSpace(settingData))
            {
                return;
            }

            var transformEncoded = settingData.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var item in transformEncoded)
            {
                var parts = item.Split(new[] { ':' }, 2);
                if (parts.Length < 2)
                {
                    _logger.WriteError($"Could not parse '{item}' as a user transformation, no replace exists.");
                    continue;
                }

                try
                {
                    var regex = new Regex(parts[0], RegexOptions.Compiled | RegexOptions.IgnoreCase);
                    _transformations.Add(new Tuple<Regex, string>(regex, parts[1]));
                }
                catch (ArgumentException ex)
                {
                    _logger.WriteError($"Could not parse '{item}' as a user transformation. Error: {ex.Message}");
                }
            }
        }

        public string GetEmailAddress(string originalEmail)
        {
            var domainIndex = originalEmail.IndexOf('@');

            if (domainIndex > -1 && originalEmail.Length > domainIndex)
            {
                var userName = originalEmail.Substring(0, domainIndex);
                var domain = originalEmail.Substring(domainIndex + 1);

                foreach (var tranform in _transformations)
                {
                    var match = tranform.Item1.Match(domain);
                    if (match.Success)
                    {
                        var transformedDomain = match.Result(tranform.Item2).ToLower();
                        var transformedEmail = $"{userName}@{transformedDomain}";
                        _logger.WriteDebug($"Transformed user '{originalEmail}' to '{transformedEmail}'");
                        return transformedEmail;
                    }
                }
            }

            return originalEmail.ToLower();
        }
    }
}
