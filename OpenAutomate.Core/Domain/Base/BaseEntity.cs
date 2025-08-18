using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using OpenAutomate.Core.Utilities;

namespace OpenAutomate.Core.Domain.Base
{
    public class BaseEntity
    {
        protected BaseEntity()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTimeUtility.UtcNow;
        }

        [Key]
        public Guid Id { get; set; }
        public DateTime? CreatedAt { get; set; }
        public Guid? CreatedBy { get; set; }
        [JsonIgnore]
        public DateTime? LastModifyAt { get; set; }
        [JsonIgnore]
        public Guid? LastModifyBy { get; set; }

        //public bool IsDeleted { get; set; }
        //[JsonIgnore]
        //public DateTime? DeleteAt { get; set; }
        //[JsonIgnore]  
        //public string? DeleteBy { get; set; }
    }
}
