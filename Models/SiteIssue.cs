using System.ComponentModel.DataAnnotations;

namespace SiteActionTrace.Models
{
    public class SiteIssue
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; } = "";

        [Required]
        [StringLength(500)]
        public string Description { get; set; } = "";

        [StringLength(60)]
        public string? Category { get; set; }

        [StringLength(120)]
        public string? SiteArea { get; set; }

        public string Priority { get; set; } = "Normal";

        public string Status { get; set; } = SiteIssueStatuses.Open;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Range(1, int.MaxValue, ErrorMessage = "Select a client.")]
        public int CustomerId { get; set; }

        public Customer? Customer { get; set; }

        public int? ProjectId { get; set; }

        public Project? Project { get; set; }
    }
}