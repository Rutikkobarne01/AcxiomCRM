using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models.Entities;

namespace AcxiomCRM.ViewModels
{
    public class LeadListViewModel
    {
        public List<Lead> Leads { get; set; } = new();
        public string? SearchTerm { get; set; }
        public string? StatusFilter { get; set; }
        public string? PriorityFilter { get; set; }
        public string? SourceFilter { get; set; }
        public int PageNumber { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalCount { get; set; }
    }

    public class LeadCreateEditViewModel
    {
        public int LeadId { get; set; }

        [Display(Name = "Lead Code")]
        public string? LeadCode { get; set; }

        [Required(ErrorMessage = "Lead name is mandatory.")]
        [StringLength(100, ErrorMessage = "Lead name cannot exceed 100 characters.")]
        [Display(Name = "Lead Name *")]
        public string LeadName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is mandatory.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(150)]
        [Display(Name = "Email Address *")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is mandatory.")]
        [RegularExpression(@"^[0-9+\-\s()]{7,20}$", ErrorMessage = "Enter a valid phone number.")]
        [StringLength(20)]
        [Display(Name = "Phone Number *")]
        public string Phone { get; set; } = string.Empty;

        [StringLength(150)]
        [Display(Name = "Company Name")]
        public string? CompanyName { get; set; }

        [Required(ErrorMessage = "Lead source is required.")]
        [Display(Name = "Source *")]
        public string Source { get; set; } = "Website";

        [Required(ErrorMessage = "Lead status is required.")]
        [Display(Name = "Status *")]
        public string Status { get; set; } = "New"; // New, Contacted, Qualified, Unqualified, Converted, Lost

        [Required]
        [Display(Name = "Priority")]
        public string Priority { get; set; } = "Medium";

        [Range(0, 1000000000, ErrorMessage = "Expected value must be a non-negative number.")]
        [Display(Name = "Expected Value (₹)")]
        public decimal ExpectedValue { get; set; } = 0;

        [Display(Name = "Assigned Sales Executive")]
        public string? AssignedTo { get; set; }
    }

    public class LeadConvertViewModel
    {
        public int LeadId { get; set; }
        public string LeadCode { get; set; } = string.Empty;
        public string LeadName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? CompanyName { get; set; }

        [Display(Name = "Create Sales Opportunity Immediately?")]
        public bool CreateOpportunity { get; set; } = true;

        [Display(Name = "Opportunity Deal Amount (₹)")]
        [Range(1, double.MaxValue, ErrorMessage = "Opportunity Amount must be greater than 0.")]
        public decimal OpportunityAmount { get; set; } = 50000;

        [Display(Name = "Initial Pipeline Stage")]
        public string OpportunityStage { get; set; } = "Qualification";
    }
}
