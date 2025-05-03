using System.Collections.Generic;

namespace OpenAutomate.Core.Dto.OrganizationUnit
{
    /// <summary>
    /// Response DTO containing a user's organization units and count
    /// </summary>
    public class UserOrganizationUnitsResponseDto
    {
        /// <summary>
        /// The total number of organization units the user belongs to
        /// </summary>
        public int Count { get; set; }
        
        /// <summary>
        /// Collection of organization units the user belongs to
        /// </summary>
        public IEnumerable<OrganizationUnitResponseDto> OrganizationUnits { get; set; } = new List<OrganizationUnitResponseDto>();
    }
} 