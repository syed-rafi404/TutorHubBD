using System;
using System.ComponentModel.DataAnnotations;

namespace TutorHubBD.Web.Models
{
    public class TuitionOffer
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Please enter a title")]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        [Range(1000, 50000, ErrorMessage = "Salary must be between 1000 and 50000")]
        public int Salary { get; set; }

        [Required]
        public string City { get; set; } = "Dhaka";

        [Required]
        public string Location { get; set; }

        [Required]
        public string Medium { get; set; }

        [Required]
        public string StudentClass { get; set; }

        public string Subject { get; set; }
        public string DaysPerWeek { get; set; }
        public string GenderPreference { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
