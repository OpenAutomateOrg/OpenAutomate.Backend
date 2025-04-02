using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace OpenAutomate.Core.Domain.BaseEntity
{
    public class BaseEntity
    {
        protected BaseEntity()
        {
            Id = Guid.NewGuid();
            CreateAt = DateTime.Now;
        }

        [Key]
        public Guid Id { get; set; }
        public DateTime? CreateAt { get; set; }
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
