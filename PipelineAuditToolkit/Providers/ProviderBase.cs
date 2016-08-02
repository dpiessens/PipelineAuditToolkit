using Fclp;
using PipelineAuditToolkit.Utility;
using System;

namespace PipelineAuditToolkit.Providers
{
    public abstract class ProviderBase
    {
        protected readonly ILogger _logger;
        protected readonly IConfigurationSettings _settings;

        protected ProviderBase(ILogger logger, IConfigurationSettings settings)
        {
            _logger = logger;
            _settings = settings;
        }

        protected void SetupOption(
            IFluentCommandLineParser parser, string name, string description, string configSetting, Action<string> callback, bool isRequired = true)
        {
            var option = parser.Setup<string>(name).WithDescription(description).Callback(callback);
            var configSettingValue = _settings.GetApplicationSetting(configSetting);
            if (!string.IsNullOrEmpty(configSettingValue))
            {
                option.SetDefault(configSettingValue);
            }
            else if (isRequired)
            {
                option.Required();
            }
        }
    }
}
