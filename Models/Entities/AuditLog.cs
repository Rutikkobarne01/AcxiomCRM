using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Models.Entities
{
    public class AuditLog
    {
        [Key]
        public int AuditLogId { get; set; }

        public string? UserId { get; set; }

        [StringLength(150)]
        [Display(Name = "User Name")]
        public string? UserName { get; set; }

        [Required]
        [StringLength(100)]
        public string Action { get; set; } = string.Empty; // Login, Failed Login, Logout, Create, Update, Delete, Role Change, Security, Lead Conversion

        [Required]
        [StringLength(100)]
        [Display(Name = "Module / Entity")]
        public string EntityName { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Record ID")]
        public string? RecordId { get; set; }

        public string? OldValue { get; set; }
        public string? NewValue { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [StringLength(50)]
        [Display(Name = "IP Address")]
        public string? IpAddress { get; set; }

        [StringLength(500)]
        public string? Details { get; set; }
    }
}
