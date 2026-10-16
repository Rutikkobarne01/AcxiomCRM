using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.DTOs
{
    public class LoginDto
    {
        [Required]
        public string EmailOrUsername { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class CustomerDto
    {
        public int CustomerId { get; set; }
        public string CustomerCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string Status { get; set; } = "Active";
        public DateTime CreatedDate { get; set; }
        public string? OwnerName { get; set; }
    }

    public class CreateCustomerDto
    {
        [Required]
        [StringLength(100)]
        public string CustomerName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Phone { get; set; } = string.Empty;

        public string? CompanyName { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string Status { get; set; } = "Active";
    }

    public class LeadDto
    {
        public int LeadId { get; set; }
        public string LeadCode { get; set; } = string.Empty;
        public string LeadName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string Source { get; set; } = "Website";
        public string Status { get; set; } = "New";
        public string Priority { get; set; } = "Medium";
        public decimal ExpectedValue { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? AssignedUserName { get; set; }
    }

    public class CreateLeadDto
    {
        [Required]
        [StringLength(100)]
        public string LeadName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Phone { get; set; } = string.Empty;

        public string? CompanyName { get; set; }
        public string Source { get; set; } = "Website";
        public string Status { get; set; } = "New";
        public string Priority { get; set; } = "Medium";
        public decimal ExpectedValue { get; set; }
    }

    public class OpportunityDto
    {
        public int OpportunityId { get; set; }
        public string OpportunityName { get; set; } = string.Empty;
        public int? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public int? LeadId { get; set; }
        public decimal Amount { get; set; }
        public string Stage { get; set; } = "Qualification";
        public int Probability { get; set; }
        public decimal WeightedValue { get; set; }
        public DateTime ExpectedCloseDate { get; set; }
        public string Status { get; set; } = "Open";
        public DateTime CreatedDate { get; set; }
        public string? AssignedUserName { get; set; }
        public string? Notes { get; set; }
    }

    public class CreateOpportunityDto
    {
        [Required]
        [StringLength(150)]
        public string OpportunityName { get; set; } = string.Empty;

        public int? CustomerId { get; set; }
        public int? LeadId { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Opportunity Amount must be greater than 0.")]
        public decimal Amount { get; set; }

        [Required]
        public string Stage { get; set; } = "Qualification";

        [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
        public int Probability { get; set; } = 20;

        [Required]
        public DateTime ExpectedCloseDate { get; set; }

        public string? Notes { get; set; }
    }

    public class FollowUpDto
    {
        public int FollowUpId { get; set; }
        public int? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public int? LeadId { get; set; }
        public string? LeadName { get; set; }
        public DateTime FollowUpDate { get; set; }
        public string FollowUpType { get; set; } = "Call";
        public string Subject { get; set; } = string.Empty;
        public string? Remarks { get; set; }
        public string Status { get; set; } = "Planned";
        public string? AssignedUserName { get; set; }
    }

    public class CreateFollowUpDto
    {
        public int? CustomerId { get; set; }
        public int? LeadId { get; set; }

        [Required]
        public DateTime FollowUpDate { get; set; }

        [Required]
        public string FollowUpType { get; set; } = "Call";

        [Required]
        public string Subject { get; set; } = string.Empty;

        public string? Remarks { get; set; }
        public string Status { get; set; } = "Planned";
    }
}
