using TutorHubBD.Web.Models;
using TutorHubBD.Web.Models.ViewModels;

namespace TutorHubBD.Web.Services
{
    public interface IAiSearchService
    {
        // For Guardian - Find tutors
        Task<TutorSearchCriteria> ExtractSearchCriteriaAsync(string userPrompt);
        Task<List<TutorSearchResultViewModel>> SearchTutorsAsync(TutorSearchCriteria criteria);
        
        // For Teacher - Find jobs
        Task<JobSearchCriteria> ExtractJobSearchCriteriaAsync(string userPrompt);
        Task<List<JobSearchResultViewModel>> SearchJobsAsync(JobSearchCriteria criteria);
    }

    public class TutorSearchCriteria
    {
        public string? Subject { get; set; }
        public string? ClassLevel { get; set; }
        public string? Location { get; set; }
        public string? GenderPreference { get; set; }
        public List<string> Keywords { get; set; } = new List<string>();
        public string? OriginalPrompt { get; set; }
        public bool IsValid => !string.IsNullOrEmpty(Subject) || 
                               !string.IsNullOrEmpty(ClassLevel) || 
                               !string.IsNullOrEmpty(Location) ||
                               Keywords.Any();
    }

    public class JobSearchCriteria
    {
        public string? Subject { get; set; }
        public string? ClassLevel { get; set; }
        public string? Location { get; set; }
        public string? City { get; set; }
        public string? Medium { get; set; }
        public int? MinSalary { get; set; }
        public int? MaxSalary { get; set; }
        public List<string> Keywords { get; set; } = new List<string>();
        public string? OriginalPrompt { get; set; }
        public bool IsValid => !string.IsNullOrEmpty(Subject) || 
                               !string.IsNullOrEmpty(ClassLevel) || 
                               !string.IsNullOrEmpty(Location) ||
                               !string.IsNullOrEmpty(City) ||
                               MinSalary.HasValue ||
                               Keywords.Any();
    }

    public class JobSearchResultViewModel
    {
        public int JobId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Salary { get; set; }
        public string? City { get; set; }
        public string? Location { get; set; }
        public string? Medium { get; set; }
        public string? StudentClass { get; set; }
        public string? Subject { get; set; }
        public string? DaysPerWeek { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? GuardianName { get; set; }
        public int MatchScore { get; set; }
    }
}
