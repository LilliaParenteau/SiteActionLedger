
using System.ComponentModel.DataAnnotations;

namespace OpsPilot.Models
{
    public class WorkOrder
    {
        public int Id { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Select a service request.")]
        public int ServiceRequestId { get; set; }

        public ServiceRequest? ServiceRequest { get; set; }

        [DataType(DataType.Date)]
        public DateTime? ScheduledDate { get; set; }

        public string Status { get; set; } = "Pending";

        [StringLength(500)]
        public string Notes { get; set; } = "";

        public int? TechnicianId { get; set; }

        public Technician? Technician { get; set; }
    }
}
