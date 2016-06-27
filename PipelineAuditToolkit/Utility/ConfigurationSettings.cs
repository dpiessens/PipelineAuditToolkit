using System.Configuration;
using System.Linq;

namespace PipelineAuditToolkit.Utility
{
    public class ConfigurationSettings : IConfigurationSettings
    {
        public string GetApplicationSetting(string key, string defaultValue = null)
        {
            var appSettings = ConfigurationManager.AppSettings;
            return appSettings.AllKeys.Contains(key) ? appSettings[key] : defaultValue;
        }
    }
}