using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using OpenAutomate.Core.Dto.Asset;
using OpenAutomate.Core.Dto.BotAgent;
using OpenAutomate.Core.Dto.Execution;
using OpenAutomate.Core.Dto.OrganizationUnit;
using OpenAutomate.Core.Dto.OrganizationUnitInvitation;
using OpenAutomate.Core.Dto.Package;
using OpenAutomate.Core.Dto.UserDto;

namespace OpenAutomate.API.Extensions
{
    /// <summary>
    /// Extensions for configuring OData in the application
    /// </summary>
    public static class ODataExtensions
    {
        /// <summary>
        /// Builds and returns the Entity Data Model for OData
        /// </summary>
        /// <returns>The configured EDM model</returns>
        public static IEdmModel GetEdmModel()
        {
            var builder = new ODataConventionModelBuilder();
            
            // Register entity sets based on DTOs
            builder.EntitySet<BotAgentResponseDto>("BotAgents");
            builder.EntitySet<UserResponse>("Users");
            builder.EntitySet<AssetResponseDto>("Assets");
            builder.EntitySet<AutomationPackageResponseDto>("AutomationPackages");
            builder.EntitySet<PackageVersionResponseDto>("PackageVersions");
            builder.EntitySet<ExecutionResponseDto>("Executions");
            builder.EntitySet<OrganizationUnitInvitationDto>("OrganizationUnitInvitations");

            return builder.GetEdmModel();
        }
    }
} 