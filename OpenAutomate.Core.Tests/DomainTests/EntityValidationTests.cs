using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using OpenAutomate.Core.Domain.Entities;
using Xunit;

namespace OpenAutomate.Core.Tests.DomainTests
{
    public class EntityValidationTests
    {
        [Fact]
        public void OrganizationUnitInvitation_Required_Fields_Validation()
        {
            // KHÔNG gán giá trị cho các trường required
            var entity = new OrganizationUnitInvitation
            {
                RecipientEmail = string.Empty, // Set required property
                Token = string.Empty           // Set required property
            };
            var context = new ValidationContext(entity);
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(entity, context, results, true);

            Assert.False(isValid);
            Assert.Contains(results, r => r.MemberNames.Contains("RecipientEmail"));
            Assert.Contains(results, r => r.MemberNames.Contains("Token"));
        }


        [Fact]
        public void Validation_Fails_If_Required_Fields_Missing()
        {
            var token = new PasswordResetToken();
            var context = new ValidationContext(token);
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(token, context, results, true);

            Assert.False(isValid);
            Assert.Contains(results, r => r.MemberNames.Contains("Token"));
            // Không kiểm tra UserId và ExpiresAt vì value type không bị [Required] validation
        }
    }
} 