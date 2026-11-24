using System.ComponentModel.DataAnnotations;

namespace SiteActionLedger.Models
{
    public class Project
    {
        public int Id { get; set; }

        [Required]
        [StringLength(120)]
        public string Name { get; set; } = "";

        [Required]
        [StringLength(200)]
        public string SiteAddress { get; set; } = "";

        [StringLength(500)]
        public string Description { get; set; } = "";

        public string Status { get; set; } = ProjectStatuses.Active;

        [Range(1, int.MaxValue, ErrorMessage = "Select a client.")]
        public int CustomerId { get; set; }

        public Customer? Customer { get; set; }
    }
}