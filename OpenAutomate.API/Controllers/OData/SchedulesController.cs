using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using OpenAutomate.API.Attributes;
using OpenAutomate.Core.Constants;
using OpenAutomate.Core.Dto.Schedule;
using OpenAutomate.Core.IServices;
using System.Linq;

namespace OpenAutomate.API.Controllers.OData
{
    /// <summary>
    /// OData controller for querying schedules
    /// </summary>
    [Route("{tenant}/odata/[controller]")]
    [Authorize]
    public class SchedulesController : ODataController
    {
        private readonly IScheduleService _scheduleService;

        /// <summary>
        /// Initializes a new instance of the SchedulesController
        /// </summary>
        public SchedulesController(IScheduleService scheduleService)
        {
            _scheduleService = scheduleService;
        }

        /// <summary>
        /// Gets schedules with OData query support
        /// </summary>
        /// <returns>Queryable collection of schedules</returns>
        [HttpGet]
        [EnableQuery(PageSize = 50)]
        [RequirePermission(Resources.ScheduleResource, Permissions.View)]
        public async Task<IQueryable<ScheduleResponseDto>> Get()
        {
            var schedules = await _scheduleService.GetAllSchedulesAsync();
            return schedules.AsQueryable();
        }
    }
} 