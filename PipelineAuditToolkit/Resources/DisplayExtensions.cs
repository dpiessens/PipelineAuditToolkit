using System;

namespace PipelineAuditToolkit.Resources
{
    public static class DisplayExtensions
    {
        public static string ToReportDateTime(this DateTime date)
        {
            return date.ToString("MM/dd/yyyy hh:mm tt");
        }

        public static DateTime ToEndOfDay(this DateTime date)
        {
            var endOfDate = new TimeSpan(11, 59, 59);
            var setTime = new TimeSpan(date.Hour, date.Minute, date.Second);
            var addSpan = endOfDate.Subtract(setTime);
            return date.Add(addSpan);
        }
    }
}
