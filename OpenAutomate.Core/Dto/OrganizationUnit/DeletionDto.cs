using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAutomate.Core.Dto.OrganizationUnit
{
    public class DeletionRequestDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime? ScheduledDeletionAt { get; set; }
        public int DaysUntilDeletion { get; set; }
    }

    public class DeletionStatusDto
    {
        public bool IsPendingDeletion { get; set; }
        public DateTime? ScheduledDeletionAt { get; set; }
        public int DaysUntilDeletion { get; set; }
        public int HoursUntilDeletion { get; set; }
        public bool CanCancel { get; set; }
    }
}
