using System.ComponentModel.DataAnnotations;

namespace TaskTrackerBLL.DTOs.Auth
{
    public class SignupDto
    {
        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(200, MinimumLength = 3)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(200)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Username is required.")]
        [StringLength(100, MinimumLength = 3)]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your password.")]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Company is required.")]
        public int CompanyId { get; set; }
    }
}
