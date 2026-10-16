using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models.Entities
{
    public class Opportunity
    {
        [Key]
        public int OpportunityId { get; set; }

        [Required(ErrorMessage = "Opportunity name is mandatory.")]
        [StringLength(150, ErrorMessage = "Opportunity name cannot exceed 150 characters.")]
        [Display(Name = "Opportunity Name")]
        public string OpportunityName { get; set; } = string.Empty;

        [Display(Name = "Customer")]
        public int? CustomerId { get; set; }

        [ForeignKey("CustomerId")]
        public virtual Customer? Customer { get; set; }

        [Display(Name = "Lead Source")]
        public int? LeadId { get; set; }

        [ForeignKey("LeadId")]
        public virtual Lead? Lead { get; set; }

        [Required(ErrorMessage = "Opportunity amount is mandatory.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Opportunity Amount must be greater than 0.")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Amount")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Stage is mandatory.")]
        [StringLength(50)]
        public string Stage { get; set; } = "Qualification"; // Qualification, Proposal, Negotiation, Won, Lost

        [Required(ErrorMessage = "Probability is mandatory.")]
        [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
        public int Probability { get; set; } = 20;

        [Required(ErrorMessage = "Expected close date is mandatory.")]
        [DataType(DataType.Date)]
        [Display(Name = "Expected Close Date")]
        public DateTime ExpectedCloseDate { get; set; } = DateTime.UtcNow.AddDays(30);

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Open"; // Open, Won, Lost

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Assigned To / Owner")]
        public string? AssignedTo { get; set; }

        [ForeignKey("AssignedTo")]
        public virtual ApplicationUser? AssignedUser { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        [NotMapped]
        [Display(Name = "Weighted Pipeline Value")]
        public decimal WeightedValue => (Amount * Probability) / 100m;
    }
}
