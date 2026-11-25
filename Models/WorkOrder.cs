using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SiteActionLedger.Models
{
    public class WorkOrder
    {
        public int Id { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Select a site issue.")]
        [Column("ServiceRequestId")]
        public int SiteIssueId { get; set; }

        public SiteIssue? SiteIssue { get; set; }

        [DataType(DataType.Date)]
        public DateTime? ScheduledDate { get; set; }

        public string Status { get; set; } = WorkOrderStatuses.Pending;

        [Required(ErrorMessage = "Enter work notes.")]
        [StringLength(500)]
        public string Notes { get; set; } = "";

        public int? TechnicianId { get; set; }

        public Technician? Technician { get; set; }

        [StringLength(1000)]
        public string? ResolutionNotes { get; set; }

        public DateTime? CompletedAt { get; set; }
    }
}