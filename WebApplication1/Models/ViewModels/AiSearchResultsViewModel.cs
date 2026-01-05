using TutorHubBD.Web.Services;

namespace TutorHubBD.Web.Models.ViewModels
{
    public class TutorSearchResultViewModel
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
        public int MatchScore { get; set; } // For relevance ranking
    }

    public class AiSearchResultsViewModel
    {
        public string OriginalQuery { get; set; } = string.Empty;
        public TutorSearchCriteria? ExtractedCriteria { get; set; }
        public List<TutorSearchResultViewModel> Results { get; set; } = new List<TutorSearchResultViewModel>();
        public bool UsedFallback { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class AiJobSearchResultsViewModel
    {
        public string OriginalQuery { get; set; } = string.Empty;
        public JobSearchCriteria? ExtractedCriteria { get; set; }
        public List<JobSearchResultViewModel> Results { get; set; } = new List<JobSearchResultViewModel>();
        public bool UsedFallback { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
