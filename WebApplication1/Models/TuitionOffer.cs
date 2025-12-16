using System.ComponentModel.DataAnnotations;

namespace TutorHubBD.Web.Models

{
    public class TuitionOffer
    {
        [Key]
        public int OfferID { get; set; }
        public string Subject { get; set; }
        public string Location { get; set; }
        public string Time { get; set; }
        public decimal Fee { get; set; } // Money should always be decimal, not float

        // Relationship: An offer belongs to a Tutor
        public int? TutorID { get; set; }
        public Tutor? Tutor { get; set; }
    }
}
