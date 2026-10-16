using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models.Entities
{
    public class Customer
    {
        [Key]
        public int CustomerId { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Customer Code")]
        public string CustomerCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Customer name is mandatory.")]
        [StringLength(100, ErrorMessage = "Customer name cannot exceed 100 characters.")]
        [Display(Name = "Customer Name")]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is mandatory.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is mandatory.")]
        [RegularExpression(@"^[0-9+\-\s()]{7,20}$", ErrorMessage = "Please enter a valid phone number (digits and optional + -).")]
        [StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        [StringLength(150)]
        [Display(Name = "Company Name")]
        public string? CompanyName { get; set; }

        [StringLength(250)]
        public string? Address { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(100)]
        public string? State { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Active"; // Active, Inactive

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Created By / Assigned To")]
        public string? CreatedBy { get; set; }

        [ForeignKey("CreatedBy")]
        public virtual ApplicationUser? Owner { get; set; }

        public virtual ICollection<Opportunity> Opportunities { get; set; } = new List<Opportunity>();
        public virtual ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
        public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();
    }
}
