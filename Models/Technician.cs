
using System.ComponentModel.DataAnnotations;

namespace  SiteActionLedger.Models
{
    public class Technician
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = "";

        [Required]
        [StringLength(100)]
        public string Specialty { get; set; } = "";

        [Phone]
        public string Phone { get; set; } = "";
    }
}
