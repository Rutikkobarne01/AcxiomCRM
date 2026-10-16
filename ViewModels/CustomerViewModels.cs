using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models.Entities;

namespace AcxiomCRM.ViewModels
{
    public class CustomerListViewModel
    {
        public List<Customer> Customers { get; set; } = new();
        public string? SearchTerm { get; set; }
        public string? StatusFilter { get; set; }
        public int PageNumber { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalCount { get; set; }
    }

    public class CustomerCreateEditViewModel
    {
        public int CustomerId { get; set; }

        [Display(Name = "Customer Code")]
        public string? CustomerCode { get; set; }

        [Required(ErrorMessage = "Customer name is mandatory.")]
        [StringLength(100, ErrorMessage = "Customer name cannot exceed 100 characters.")]
        [Display(Name = "Customer Name *")]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is mandatory.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(150)]
        [Display(Name = "Email Address *")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is mandatory.")]
        [RegularExpression(@"^[0-9+\-\s()]{7,20}$", ErrorMessage = "Enter a valid phone number (e.g. 9876543210).")]
        [StringLength(20)]
        [Display(Name = "Phone Number *")]
        public string Phone { get; set; } = string.Empty;

        [StringLength(150)]
        [Display(Name = "Company Name")]
        public string? CompanyName { get; set; }

        [StringLength(250)]
        [Display(Name = "Address")]
        public string? Address { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(100)]
        public string? State { get; set; }

        [Required]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Active";

        [Display(Name = "Assigned Sales Executive")]
        public string? CreatedBy { get; set; }
    }

    public class CustomerDetailsViewModel
    {
        public Customer Customer { get; set; } = null!;
        public List<Opportunity> Opportunities { get; set; } = new();
        public List<FollowUp> FollowUps { get; set; } = new();
        public List<Activity> Activities { get; set; } = new();
        public List<AuditLog> AuditHistory { get; set; } = new();
    }
}
