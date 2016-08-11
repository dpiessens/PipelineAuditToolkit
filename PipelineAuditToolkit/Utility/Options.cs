using System;

namespace PipelineAuditToolkit.Utility
{
    public class Options
    {
        public string ProjectFilter { get; set; }

        public string ReportPath { get; set; }

        public bool ShowReport { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }
    }
}
