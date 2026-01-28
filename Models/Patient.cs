using System.ComponentModel.DataAnnotations;

namespace XRayDiagnosticSystem.Models
{
    public class Patient
    {
        public int PatientId { get; set; }

        [Required(ErrorMessage = "Full Name is required")]
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Name should contain only letters")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [Range(1, 120, ErrorMessage = "Please enter a valid age")]
        public int Age { get; set; }

        public string? Gender { get; set; }

        [Required(ErrorMessage = "Phone number is mandatory")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Phone number must be exactly 10 digits")]
        public string? ContactNumber { get; set; }

        public string? Address { get; set; }

        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MinLength(5, ErrorMessage = "Password must be at least 5 characters")]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*\d).+$", ErrorMessage = "Password must contain at least one capital letter and one number")]
        public string Password { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}
