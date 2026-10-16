using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models.Entities;

namespace AcxiomCRM.ViewModels
{
    public class OpportunityListViewModel
    {
        public List<Opportunity> Opportunities { get; set; } = new();
        public string? SearchTerm { get; set; }
        public string? StageFilter { get; set; }
        public string? StatusFilter { get; set; }
        public decimal TotalPipelineAmount { get; set; }
        public decimal TotalWeightedAmount { get; set; }
        public int PageNumber { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalCount { get; set; }
    }

    public class OpportunityCreateEditViewModel
    {
        public int OpportunityId { get; set; }

        [Required(ErrorMessage = "Opportunity deal name is mandatory.")]
        [StringLength(150, ErrorMessage = "Opportunity name cannot exceed 150 characters.")]
        [Display(Name = "Opportunity / Deal Name *")]
        public string OpportunityName { get; set; } = string.Empty;

        [Display(Name = "Related Customer")]
        public int? CustomerId { get; set; }

        [Display(Name = "Originating Lead")]
        public int? LeadId { get; set; }

        [Required(ErrorMessage = "Opportunity Amount is mandatory.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Opportunity Amount must be greater than 0.")]
        [Display(Name = "Deal Amount (₹) *")]
        public decimal Amount { get; set; } = 10000;

        [Required(ErrorMessage = "Pipeline stage is required.")]
        [Display(Name = "Pipeline Stage *")]
        public string Stage { get; set; } = "Qualification"; // Qualification, Proposal, Negotiation, Won, Lost

        [Required(ErrorMessage = "Probability is mandatory.")]
        [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
        [Display(Name = "Win Probability (%) *")]
        public int Probability { get; set; } = 25;

        [Required(ErrorMessage = "Expected close date is mandatory.")]
        [DataType(DataType.Date)]
        [Display(Name = "Expected Close Date *")]
        public DateTime ExpectedCloseDate { get; set; } = DateTime.UtcNow.AddDays(30);

        [Required]
        [Display(Name = "Deal Status")]
        public string Status { get; set; } = "Open"; // Open, Won, Lost

        [Display(Name = "Assigned Sales Representative")]
        public string? AssignedTo { get; set; }

        [StringLength(500)]
        [Display(Name = "Strategic Notes / Next Steps")]
        public string? Notes { get; set; }
    }
}
