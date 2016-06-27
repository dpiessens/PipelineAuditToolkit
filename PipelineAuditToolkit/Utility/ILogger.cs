namespace PipelineAuditToolkit.Utility
{
    public interface ILogger
    {
        void WriteDebug(string message);

        void WriteInfo(string message);

        void WriteError(string message);
    }
}