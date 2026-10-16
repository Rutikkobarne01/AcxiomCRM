using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models.Entities;

namespace AcxiomCRM.ViewModels
{
    public class FollowUpListViewModel
    {
        public List<FollowUp> FollowUps { get; set; } = new();
        public string? StatusFilter { get; set; }
        public string? TypeFilter { get; set; }
        public string? TimeFilter { get; set; } // Upcoming, Overdue, All
        public int PageNumber { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalCount { get; set; }
    }

    public class FollowUpCreateEditViewModel
    {
        public int FollowUpId { get; set; }

        [Display(Name = "Related Customer")]
        public int? CustomerId { get; set; }

        [Display(Name = "Related Lead")]
        public int? LeadId { get; set; }

        [Required(ErrorMessage = "Follow-up date and time is mandatory.")]
        [Display(Name = "Follow-Up Date & Time *")]
        public DateTime FollowUpDate { get; set; } = DateTime.UtcNow.AddHours(2);

        [Required(ErrorMessage = "Follow-up type is mandatory.")]
        [Display(Name = "Activity Type *")]
        public string FollowUpType { get; set; } = "Call"; // Call, Meeting, Email, Task

        [Required(ErrorMessage = "Subject is mandatory.")]
        [StringLength(200, ErrorMessage = "Subject cannot exceed 200 characters.")]
        [Display(Name = "Subject *")]
        public string Subject { get; set; } = string.Empty;

        [StringLength(1000)]
        [Display(Name = "Discussion Agenda / Remarks")]
        public string? Remarks { get; set; }

        [Required]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Planned"; // Planned, Completed, Missed, Cancelled

        [Display(Name = "Assigned Team Member")]
        public string? AssignedTo { get; set; }
    }
}
