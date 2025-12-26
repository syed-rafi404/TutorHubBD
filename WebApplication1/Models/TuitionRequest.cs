using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TutorHubBD.Web.Models
{
    public class TuitionRequest
    {
        [Key]
        public int RequestID { get; set; }

        [Required(ErrorMessage = "Please enter your name")]
        public string StudentName { get; set; }

        [Required]
        [EmailAddress]
        public string StudentEmail { get; set; }

        [Required]
        public string Message { get; set; } // e.g., "I am interested in this job"

        public string Status { get; set; } = "Pending"; // Pending, Accepted, Rejected

        public DateTime RequestDate { get; set; } = DateTime.Now;

        // Link this request to a specific Job Post
        public int TuitionOfferId { get; set; }
        public TuitionOffer TuitionOffer { get; set; }

        public int? TutorId { get; set; }
        [ForeignKey("TutorId")]
        public Tutor Tutor { get; set; }
    }
}
