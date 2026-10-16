using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.ViewModels
{
    public class UserItemViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int AccessFailedCount { get; set; }
        public bool IsLockedOut { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class UserListViewModel
    {
        public List<UserItemViewModel> Users { get; set; } = new();
        public string? SearchTerm { get; set; }
        public string? RoleFilter { get; set; }
    }

    public class UserCreateViewModel
    {
        [Required(ErrorMessage = "Full Name is required.")]
        [StringLength(100)]
        [Display(Name = "Full Name *")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [Display(Name = "Email Address *")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [RegularExpression(@"^[0-9+\-\s()]{7,20}$", ErrorMessage = "Enter a valid phone number.")]
        [Display(Name = "Phone Number *")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$",
            ErrorMessage = "Password must contain uppercase, lowercase, number, and special character.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Role *")]
        public string Role { get; set; } = "SalesExecutive"; // Admin, Manager, SalesExecutive
    }

    public class UserEditViewModel
    {
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "Full Name is required.")]
        [StringLength(100)]
        [Display(Name = "Full Name *")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [Display(Name = "Email Address *")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [RegularExpression(@"^[0-9+\-\s()]{7,20}$", ErrorMessage = "Enter a valid phone number.")]
        [Display(Name = "Phone Number *")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Role *")]
        public string Role { get; set; } = "SalesExecutive";

        [Display(Name = "Active Account Status")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Reset Password (leave empty to keep current password)")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
        [DataType(DataType.Password)]
        public string? NewPassword { get; set; }
    }
}
