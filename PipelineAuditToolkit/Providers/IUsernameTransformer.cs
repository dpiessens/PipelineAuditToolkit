namespace PipelineAuditToolkit.Providers
{
    public interface IUsernameTransformer
    {
        string GetEmailAddress(string originalEmail);
    }
}