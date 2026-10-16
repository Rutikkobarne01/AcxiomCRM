using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models.Entities
{
    public class Lead
    {
        [Key]
        public int LeadId { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Lead Code")]
        public string LeadCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Lead name is mandatory.")]
        [StringLength(100, ErrorMessage = "Lead name cannot exceed 100 characters.")]
        [Display(Name = "Lead Name")]
        public string LeadName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is mandatory.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is mandatory.")]
        [RegularExpression(@"^[0-9+\-\s()]{7,20}$", ErrorMessage = "Please enter a valid phone number.")]
        [StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        [StringLength(150)]
        [Display(Name = "Company Name")]
        public string? CompanyName { get; set; }

        [Required(ErrorMessage = "Lead source is required.")]
        [StringLength(50)]
        public string Source { get; set; } = "Website"; // Website, Referral, Cold Call, Advertisement, Partner, Other

        [Required(ErrorMessage = "Lead status is required.")]
        [StringLength(50)]
        public string Status { get; set; } = "New"; // New, Contacted, Qualified, Unqualified, Converted, Lost

        [StringLength(20)]
        public string Priority { get; set; } = "Medium"; // Low, Medium, High

        [Range(0, 1000000000, ErrorMessage = "Expected value must be a valid positive amount.")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Expected Value")]
        public decimal ExpectedValue { get; set; } = 0;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Assigned To")]
        public string? AssignedTo { get; set; }

        [ForeignKey("AssignedTo")]
        public virtual ApplicationUser? AssignedUser { get; set; }

        public int? ConvertedCustomerId { get; set; }
        public int? ConvertedOpportunityId { get; set; }

        public virtual ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
        public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();
    }
}
