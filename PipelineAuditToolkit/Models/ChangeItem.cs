using System;

namespace PipelineAuditToolkit.Models
{
    public class ChangeItem
    {
        public ChangeItem(string id, DateTime created, string userId, string message)
        {
            Id = id;
            Created = created;
            Message = message;
            UserId = userId;
        }

        public string Id { get; private set; }
        public DateTime Created { get; private set; }
        public string Message { get; set; }
        public string UserId { get; private set; }
    }
}