namespace PipelineAuditToolkit.Utility
{
    public interface IConfigurationSettings
    {
        string GetApplicationSetting(string key, string defaultValue = null);
    }
}