﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenAutomate.Core.Domain.Entities
{
    public class UserAuthority
    {
        [Required]
        public Guid UserId { set; get; }
        [ForeignKey("UserId")]
        public User User { get; set; }

        [Required]
        public Guid AuthorityID  { set; get; }
        [ForeignKey("AuthorityID")]
        public Authority Authority { get; set; }


    }
}
