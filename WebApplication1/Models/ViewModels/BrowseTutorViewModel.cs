namespace TutorHubBD.Web.Models.ViewModels
{
    public class BrowseTutorViewModel
    {
        public int TutorId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
        public string? Education { get; set; }
        public string? Subjects { get; set; }
        public string? PreferredClasses { get; set; }
        public string? PreferredLocations { get; set; }
        public string? Bio { get; set; }
        public int? Experience { get; set; }
        public float Rating { get; set; }
        public bool IsVerified { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
    }
}
