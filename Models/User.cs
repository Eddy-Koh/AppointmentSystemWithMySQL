using System.ComponentModel.DataAnnotations;

namespace AppointmentBookingSystem.Models
{
    public class User
    {
        [Key]
        [Required(ErrorMessage = "Username is required.")]
        [StringLength(50)]
        public string Username { get; set; } // Primary key

        [Required(ErrorMessage = "Mobile number is required.")]
        [RegularExpression(@"^\d{10,11}$", ErrorMessage = "Mobile number must be 10 or 11 digits.")]
        public string MobilePhone { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }


        [Required(ErrorMessage = "Password is required.")]
        [MinLength(3, ErrorMessage = "Password must be at least 3 characters.")]
        public string Password { get; set; } // Hashed password

        [Required(ErrorMessage = "Role is required.")]
        [StringLength(20)]
        public string Role { get; set; } // "Requester" or "Approver"

        [StringLength(20)]
        public string ApprovalStatus { get; set; } // "Pending", "Accepted", "Rejected"
    }
}
