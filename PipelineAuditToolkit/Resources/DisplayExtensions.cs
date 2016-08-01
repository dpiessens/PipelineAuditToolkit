using System;

namespace PipelineAuditToolkit.Resources
{
    public static class DisplayExtensions
    {
        public static string ToReportDateTime(this DateTime date)
        {
            return date.ToString("MM/dd/yyyy hh:mm tt");
        }
    }
}
