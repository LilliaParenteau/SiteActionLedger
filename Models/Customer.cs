using System.ComponentModel.DataAnnotations;

namespace SiteActionLedger.Models
{
    public class Customer
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = "";

        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";

        [Required]
        public string Phone { get; set; } = "";

        [Required]
        public string Address { get; set; } = "";
    }
}