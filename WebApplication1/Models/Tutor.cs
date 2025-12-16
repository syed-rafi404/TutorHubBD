namespace TutorHubBD.Web.Models
{
    public class Tutor
    {
        public int TutorID { get; set; }
        public string Education { get; set; }
        public string Subjects { get; set; }
        public float Rating { get; set; }
        public bool IsVerified { get; set; }
        // We will add the User relationship later
    }
}
