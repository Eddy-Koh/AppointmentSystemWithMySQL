using System;
using System.ComponentModel.DataAnnotations;

namespace AppointmentBookingSystem.Models
{
    public class Appointment
    {
        [Required]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string RequesterName { get; set; }

        [Required]
        public DateTime AppointmentDate { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        public string Reason { get; set; }

        [Required]
        public string Status { get; set; } // "Pending", "Accepted", "Rejected"
    }
}
