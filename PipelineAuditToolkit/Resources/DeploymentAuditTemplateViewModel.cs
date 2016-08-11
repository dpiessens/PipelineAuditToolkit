using PipelineAuditToolkit.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace PipelineAuditToolkit.Resources
{
    public class DeploymentAuditTemplateViewModel
    {
        public string DateRange
        {
            get
            {
                if (StartDate.HasValue && EndDate.HasValue)
                {
                    return $"{StartDate.Value.ToReportDateTime()} to {EndDate.Value.ToReportDateTime()}";
                }
                else if (StartDate.HasValue)
                {
                    return $"Beginning {StartDate.Value.ToReportDateTime()}";
                }
                else if (EndDate.HasValue)
                {
                    return $"Before {EndDate.Value.ToReportDateTime()}";
                }

                return string.Empty;
            }
        }

        public DateTime? EndDate { get; set; }

        public bool HasDates
        {
            get
            {
                return StartDate.HasValue || EndDate.HasValue;
            }
        }

        public List<IProject> Projects { get; set; }

        public DateTime? StartDate { get; set; }
    }
}
