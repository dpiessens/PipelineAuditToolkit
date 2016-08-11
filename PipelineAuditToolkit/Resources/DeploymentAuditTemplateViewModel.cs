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
                    return $"{StartDate.Value:d} to {EndDate.Value:d}";
                }
                else if (StartDate.HasValue)
                {
                    return $"Beginning {StartDate.Value:d}";
                }
                else if (EndDate.HasValue)
                {
                    return $"Before {EndDate.Value:d}";
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
