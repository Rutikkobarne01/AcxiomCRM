using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models.Entities
{
    public class FollowUp
    {
        [Key]
        public int FollowUpId { get; set; }

        [Display(Name = "Related Customer")]
        public int? CustomerId { get; set; }

        [ForeignKey("CustomerId")]
        public virtual Customer? Customer { get; set; }

        [Display(Name = "Related Lead")]
        public int? LeadId { get; set; }

        [ForeignKey("LeadId")]
        public virtual Lead? Lead { get; set; }

        [Required(ErrorMessage = "Follow-up date is required.")]
        [Display(Name = "Follow-Up Date & Time")]
        public DateTime FollowUpDate { get; set; } = DateTime.UtcNow.AddHours(2);

        [Required(ErrorMessage = "Follow-up type is required.")]
        [StringLength(50)]
        [Display(Name = "Type")]
        public string FollowUpType { get; set; } = "Call"; // Call, Meeting, Email, Task

        [Required(ErrorMessage = "Subject is required.")]
        [StringLength(200)]
        public string Subject { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Remarks { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Planned"; // Planned, Completed, Missed, Cancelled

        [Display(Name = "Assigned To")]
        public string? AssignedTo { get; set; }

        [ForeignKey("AssignedTo")]
        public virtual ApplicationUser? AssignedUser { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
