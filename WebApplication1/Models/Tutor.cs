using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TutorHubBD.Web.Models
{
    public class Tutor
    {
        [Key]
        public int TutorID { get; set; }
        public string Education { get; set; }
        public string Subjects { get; set; }
        public float Rating { get; set; }
        public bool IsVerified { get; set; }
        
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }
    }
}
