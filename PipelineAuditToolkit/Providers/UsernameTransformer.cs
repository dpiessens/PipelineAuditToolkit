using PipelineAuditToolkit.Utility;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace PipelineAuditToolkit.Providers
{
    public class UsernameTransformer : IUsernameTransformer
    {
        private const string DomainAppSetting = "UserNameTransformer.Translations";
        private const string UserRegexAppSetting = "UserNameTransformer.RegexUserTranslations";
        private const string UserFixedAppSetting = "UserNameTransformer.FixedUserTranslations";

        private readonly IConfigurationSettings _configurationSettings;
        private readonly ILogger _logger;
        private readonly List<Tuple<Regex, string>> _domainTransformations;
        private readonly List<Tuple<Regex, string>> _userRegexTransformations;
        private readonly List<Tuple<string, string>> _userFixedTransformations;

        public UsernameTransformer(IConfigurationSettings configurationSettings, ILogger logger)
        {
            _configurationSettings = configurationSettings;
            _logger = logger;

            _domainTransformations = new List<Tuple<Regex, string>>();
            _userRegexTransformations = new List<Tuple<Regex, string>>();
            _userFixedTransformations = new List<Tuple<string, string>>();
        }

        public int DomainRuleCount
        {
            get
            {
                return _domainTransformations.Count;
            }
        }

        public int UserRegexRuleCount
        {
            get
            {
                return _userRegexTransformations.Count;
            }
        }

        public int UserFixedRuleCount
        {
            get
            {
                return _userFixedTransformations.Count;
            }
        }

        public void Initalize()
        {
            var regexFunc = new Func<string, Regex>(d => new Regex(d, RegexOptions.Compiled | RegexOptions.IgnoreCase));
            ParseList(_domainTransformations, DomainAppSetting, "domain", regexFunc);

            ParseList(_userRegexTransformations, UserRegexAppSetting, "user regex", regexFunc);

            ParseList(_userFixedTransformations, UserFixedAppSetting, "user fixed", s => s);
        }

        public string GetEmailAddress(string originalEmail)
        {
            var domainIndex = originalEmail.IndexOf('@');

            if (domainIndex > -1 && originalEmail.Length > (domainIndex + 1))
            {
                var userName = originalEmail.Substring(0, domainIndex);
                var domain = originalEmail.Substring(domainIndex + 1);

                var transformedDomain = MatchRegex(_domainTransformations, domain);

                var transformedUser = MatchRegex(_userRegexTransformations, userName);
                if (transformedUser == userName)
                {
                    foreach (var item in _userFixedTransformations)
                    {
                        if (string.Equals(userName, item.Item1, StringComparison.InvariantCultureIgnoreCase))
                        {
                            transformedUser = item.Item2;
                            break;
                        }
                    }
                }

                var transformedEmail = $"{transformedUser}@{transformedDomain}";
                _logger.WriteDebug($"Transformed user '{originalEmail}' to '{transformedEmail}'");
                return transformedEmail;
            }

            return originalEmail.ToLower();
        }

        private static string MatchRegex(List<Tuple<Regex, string>> collection, string originalData)
        {
            foreach (var tranform in collection)
            {
                var match = tranform.Item1.Match(originalData);
                if (match.Success)
                {
                    return match.Result(tranform.Item2).ToLower();
                }
            }

            return originalData;
        }

        private void ParseList<T>(List<Tuple<T, string>> collection, string settingName, string parseType, Func<string, T> creator)
        {
            var settingData = _configurationSettings.GetApplicationSetting(settingName);
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
                    _logger.WriteError($"Could not parse '{item}' as a {parseType} transformation, no replace exists.");
                    continue;
                }

                try
                {
                    T findItem = creator(parts[0]);
                    collection.Add(new Tuple<T, string>(findItem, parts[1]));
                }
                catch (ArgumentException ex)
                {
                    _logger.WriteError($"Could not parse '{item}' as a {parseType} transformation. Error: {ex.Message}");
                }
            }
        }
    }
}
