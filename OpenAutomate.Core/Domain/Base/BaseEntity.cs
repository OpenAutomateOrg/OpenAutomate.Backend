using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace OpenAutomate.Core.Domain.Base
{
    public class BaseEntity
    {
        protected BaseEntity()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.Now;
        }

        [Key]
        public Guid Id { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        [JsonIgnore]
        public DateTime? LastModifyAt { get; set; }
        [JsonIgnore]
        public string? LastModifyBy { get; set; }

        //public bool IsDeleted { get; set; }
        //[JsonIgnore]
        //public DateTime? DeleteAt { get; set; }
        //[JsonIgnore]  
        //public string? DeleteBy { get; set; }
    }
}
