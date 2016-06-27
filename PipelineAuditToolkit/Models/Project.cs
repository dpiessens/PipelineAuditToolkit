﻿namespace PipelineAuditToolkit.Models
{
    public class Project : IProject
    {
        public Project(string id, string name)
        {
            Id = id;
            Name = name;
        }

        public string Id { get; private set; }
        public string Name { get; private set; }
    }
}