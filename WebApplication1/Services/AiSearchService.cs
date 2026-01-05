using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TutorHubBD.Web.Data;
using TutorHubBD.Web.Models;
using TutorHubBD.Web.Models.ViewModels;

namespace TutorHubBD.Web.Services
{
    public class AiSearchService : IAiSearchService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AiSearchService> _logger;
        private readonly HttpClient _httpClient;

        public AiSearchService(
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<AiSearchService> logger,
            IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
        }

        #region Tutor Search (For Guardians)

        public async Task<TutorSearchCriteria> ExtractSearchCriteriaAsync(string userPrompt)
        {
            var criteria = new TutorSearchCriteria { OriginalPrompt = userPrompt };

            try
            {
                var apiKey = _configuration["GeminiApi:ApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                {
                    _logger.LogWarning("Gemini API key not configured. Using fallback search.");
                    return ExtractCriteriaFallback(userPrompt);
                }

                var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={apiKey}";

                var systemPrompt = @"You are an entity extractor for a tutor matching system in Bangladesh. 
Extract the following fields from the user's text and return ONLY a valid JSON object (no markdown, no code blocks):
{
    ""Subject"": ""extracted subject like Math, English, Physics, Chemistry, Biology, Bangla, ICT, Accounting, etc."",
    ""ClassLevel"": ""extracted class level like Nursery, KG, Class 1-10, HSC, A-Level, O-Level, University"",
    ""Location"": ""extracted location in Bangladesh like Dhaka, Mirpur, Uttara, Dhanmondi, Gulshan, Chittagong, etc."",
    ""GenderPreference"": ""Male or Female if mentioned, otherwise null"",
    ""Keywords"": [""array of adjectives/qualities like patient, experienced, friendly, strict, professional""]
}

Rules:
- Return ONLY the JSON object, no explanation
- Use null for fields not mentioned
- Keywords should be an array of strings
- Be flexible with spelling variations";

                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = $"{systemPrompt}\n\nUser query: {userPrompt}" }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.1,
                        maxOutputTokens = 500
                    }
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(endpoint, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Gemini API error: {response.StatusCode} - {responseContent}");
                    return ExtractCriteriaFallback(userPrompt);
                }

                // Parse Gemini response
                var geminiResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                var generatedText = geminiResponse
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                if (string.IsNullOrEmpty(generatedText))
                {
                    return ExtractCriteriaFallback(userPrompt);
                }

                // Clean up the response (remove markdown code blocks if present)
                generatedText = generatedText
                    .Replace("```json", "")
                    .Replace("```", "")
                    .Trim();

                // Parse the extracted JSON
                var extractedData = JsonSerializer.Deserialize<JsonElement>(generatedText);

                criteria.Subject = GetJsonStringOrNull(extractedData, "Subject");
                criteria.ClassLevel = GetJsonStringOrNull(extractedData, "ClassLevel");
                criteria.Location = GetJsonStringOrNull(extractedData, "Location");
                criteria.GenderPreference = GetJsonStringOrNull(extractedData, "GenderPreference");

                if (extractedData.TryGetProperty("Keywords", out var keywordsElement) && 
                    keywordsElement.ValueKind == JsonValueKind.Array)
                {
                    criteria.Keywords = keywordsElement.EnumerateArray()
                        .Where(k => k.ValueKind == JsonValueKind.String)
                        .Select(k => k.GetString()!)
                        .Where(k => !string.IsNullOrEmpty(k))
                        .ToList();
                }

                _logger.LogInformation($"AI extracted criteria: Subject={criteria.Subject}, Class={criteria.ClassLevel}, Location={criteria.Location}");
                return criteria;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Gemini API. Using fallback.");
                return ExtractCriteriaFallback(userPrompt);
            }
        }

        private TutorSearchCriteria ExtractCriteriaFallback(string userPrompt)
        {
            var criteria = new TutorSearchCriteria { OriginalPrompt = userPrompt };
            var promptLower = userPrompt.ToLower();

            // Subject detection
            var subjects = new[] { "math", "mathematics", "english", "physics", "chemistry", "biology", 
                                   "bangla", "bengali", "ict", "computer", "accounting", "economics", 
                                   "science", "social science", "history", "geography" };
            criteria.Subject = subjects.FirstOrDefault(s => promptLower.Contains(s));

            // Class level detection
            var classLevels = new Dictionary<string, string>
            {
                { "nursery", "Nursery" }, { "kg", "KG" }, { "kindergarten", "KG" },
                { "class 1", "Class 1" }, { "class 2", "Class 2" }, { "class 3", "Class 3" },
                { "class 4", "Class 4" }, { "class 5", "Class 5" }, { "class 6", "Class 6" },
                { "class 7", "Class 7" }, { "class 8", "Class 8" }, { "class 9", "Class 9" },
                { "class 10", "Class 10" }, { "hsc", "HSC" }, { "a-level", "A-Level" },
                { "a level", "A-Level" }, { "o-level", "O-Level" }, { "o level", "O-Level" }
            };
            foreach (var kvp in classLevels)
            {
                if (promptLower.Contains(kvp.Key))
                {
                    criteria.ClassLevel = kvp.Value;
                    break;
                }
            }

            // Location detection (common Bangladesh locations)
            var locations = new[] { "dhaka", "mirpur", "uttara", "dhanmondi", "gulshan", "banani",
                                    "mohammadpur", "motijheel", "chittagong", "sylhet", "khulna",
                                    "rajshahi", "rangpur", "barisal", "online" };
            criteria.Location = locations.FirstOrDefault(l => promptLower.Contains(l));

            // Gender preference
            if (promptLower.Contains("female") || promptLower.Contains("woman") || promptLower.Contains("lady"))
                criteria.GenderPreference = "Female";
            else if (promptLower.Contains("male") || promptLower.Contains("man") || promptLower.Contains("sir"))
                criteria.GenderPreference = "Male";

            // Keywords extraction
            var keywords = new[] { "patient", "experienced", "friendly", "strict", "professional",
                                   "caring", "dedicated", "qualified", "expert", "good", "best" };
            criteria.Keywords = keywords.Where(k => promptLower.Contains(k)).ToList();

            return criteria;
        }

        public async Task<List<TutorSearchResultViewModel>> SearchTutorsAsync(TutorSearchCriteria criteria)
        {
            var query = _context.Tutors
                .Include(t => t.User)
                .Where(t => t.IsVerified && t.IsProfileComplete)
                .AsQueryable();

            // Apply filters based on extracted criteria
            if (!string.IsNullOrEmpty(criteria.Subject))
            {
                query = query.Where(t => t.Subjects != null && 
                    t.Subjects.ToLower().Contains(criteria.Subject.ToLower()));
            }

            if (!string.IsNullOrEmpty(criteria.ClassLevel))
            {
                query = query.Where(t => t.PreferredClasses != null && 
                    t.PreferredClasses.ToLower().Contains(criteria.ClassLevel.ToLower()));
            }

            if (!string.IsNullOrEmpty(criteria.Location))
            {
                query = query.Where(t => t.PreferredLocations != null && 
                    t.PreferredLocations.ToLower().Contains(criteria.Location.ToLower()));
            }

            var tutors = await query.ToListAsync();

            // Calculate match scores and map to view model
            var results = tutors.Select(t => new TutorSearchResultViewModel
            {
                TutorId = t.TutorID,
                FullName = t.User?.FullName ?? "Unknown",
                ProfilePictureUrl = t.ProfilePictureUrl ?? t.User?.ProfilePictureUrl,
                Education = t.Education,
                Subjects = t.Subjects,
                PreferredClasses = t.PreferredClasses,
                PreferredLocations = t.PreferredLocations,
                Bio = t.Bio,
                Experience = t.Experience,
                Rating = t.Rating,
                IsVerified = t.IsVerified,
                MatchScore = CalculateTutorMatchScore(t, criteria)
            })
            .OrderByDescending(t => t.MatchScore)
            .ThenByDescending(t => t.Rating)
            .ToList();

            // If no results with filters, try a broader search
            if (!results.Any() && !string.IsNullOrEmpty(criteria.OriginalPrompt))
            {
                results = await BroadTutorSearchAsync(criteria.OriginalPrompt);
            }

            return results;
        }

        private int CalculateTutorMatchScore(Tutor tutor, TutorSearchCriteria criteria)
        {
            int score = 0;

            // Subject match
            if (!string.IsNullOrEmpty(criteria.Subject) && 
                tutor.Subjects?.ToLower().Contains(criteria.Subject.ToLower()) == true)
                score += 30;

            // Class level match
            if (!string.IsNullOrEmpty(criteria.ClassLevel) && 
                tutor.PreferredClasses?.ToLower().Contains(criteria.ClassLevel.ToLower()) == true)
                score += 25;

            // Location match
            if (!string.IsNullOrEmpty(criteria.Location) && 
                tutor.PreferredLocations?.ToLower().Contains(criteria.Location.ToLower()) == true)
                score += 20;

            // Keyword matches in Bio
            if (criteria.Keywords.Any() && !string.IsNullOrEmpty(tutor.Bio))
            {
                var bioLower = tutor.Bio.ToLower();
                var keywordMatches = criteria.Keywords.Count(k => bioLower.Contains(k.ToLower()));
                score += keywordMatches * 5;
            }

            // Bonus for verified and high rating
            if (tutor.IsVerified) score += 10;
            score += (int)(tutor.Rating * 2);

            // Experience bonus
            if (tutor.Experience.HasValue)
                score += Math.Min(tutor.Experience.Value, 10);

            return score;
        }

        private async Task<List<TutorSearchResultViewModel>> BroadTutorSearchAsync(string searchText)
        {
            var words = searchText.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 2).ToList();

            var tutors = await _context.Tutors
                .Include(t => t.User)
                .Where(t => t.IsVerified && t.IsProfileComplete)
                .ToListAsync();

            return tutors
                .Select(t => new TutorSearchResultViewModel
                {
                    TutorId = t.TutorID,
                    FullName = t.User?.FullName ?? "Unknown",
                    ProfilePictureUrl = t.ProfilePictureUrl ?? t.User?.ProfilePictureUrl,
                    Education = t.Education,
                    Subjects = t.Subjects,
                    PreferredClasses = t.PreferredClasses,
                    PreferredLocations = t.PreferredLocations,
                    Bio = t.Bio,
                    Experience = t.Experience,
                    Rating = t.Rating,
                    IsVerified = t.IsVerified,
                    MatchScore = CalculateBroadTutorScore(t, words)
                })
                .Where(t => t.MatchScore > 0)
                .OrderByDescending(t => t.MatchScore)
                .Take(20)
                .ToList();
        }

        private int CalculateBroadTutorScore(Tutor tutor, List<string> words)
        {
            var searchableText = $"{tutor.Subjects} {tutor.PreferredClasses} {tutor.PreferredLocations} {tutor.Bio} {tutor.Education}".ToLower();
            return words.Count(word => searchableText.Contains(word)) * 5;
        }

        #endregion

        #region Job Search (For Teachers)

        public async Task<JobSearchCriteria> ExtractJobSearchCriteriaAsync(string userPrompt)
        {
            var criteria = new JobSearchCriteria { OriginalPrompt = userPrompt };

            try
            {
                var apiKey = _configuration["GeminiApi:ApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                {
                    _logger.LogWarning("Gemini API key not configured. Using fallback search.");
                    return ExtractJobCriteriaFallback(userPrompt);
                }

                var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={apiKey}";

                var systemPrompt = @"You are an entity extractor for a tuition job matching system in Bangladesh.
A tutor is looking for tuition jobs. Extract the following fields from the tutor's text and return ONLY a valid JSON object:
{
    ""Subject"": ""extracted subject like Math, English, Physics, Chemistry, Biology, Bangla, ICT, Accounting, etc."",
    ""ClassLevel"": ""extracted class level like Nursery, KG, Class 1-10, HSC, A-Level, O-Level"",
    ""City"": ""extracted city like Dhaka, Chittagong, Sylhet, Khulna, Rajshahi, etc."",
    ""Location"": ""specific area like Mirpur, Uttara, Dhanmondi, Gulshan, Banani, etc."",
    ""Medium"": ""Bangla or English if mentioned, otherwise null"",
    ""MinSalary"": number if minimum salary mentioned (e.g., 5000), otherwise null,
    ""MaxSalary"": number if maximum salary mentioned, otherwise null,
    ""Keywords"": [""array of preferences like flexible, nearby, experienced, part-time, full-time""]
}

Rules:
- Return ONLY the JSON object, no explanation
- Use null for fields not mentioned
- MinSalary and MaxSalary should be numbers without currency symbols
- Keywords should be an array of strings";

                var requestBody = new
                {
                    contents = new[]
                    {
                        new { parts = new[] { new { text = $"{systemPrompt}\n\nTutor's query: {userPrompt}" } } }
                    },
                    generationConfig = new { temperature = 0.1, maxOutputTokens = 500 }
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(endpoint, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Gemini API error: {response.StatusCode} - {responseContent}");
                    return ExtractJobCriteriaFallback(userPrompt);
                }

                var geminiResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                var generatedText = geminiResponse
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                if (string.IsNullOrEmpty(generatedText))
                    return ExtractJobCriteriaFallback(userPrompt);

                generatedText = generatedText.Replace("```json", "").Replace("```", "").Trim();
                var extractedData = JsonSerializer.Deserialize<JsonElement>(generatedText);

                criteria.Subject = GetJsonStringOrNull(extractedData, "Subject");
                criteria.ClassLevel = GetJsonStringOrNull(extractedData, "ClassLevel");
                criteria.City = GetJsonStringOrNull(extractedData, "City");
                criteria.Location = GetJsonStringOrNull(extractedData, "Location");
                criteria.Medium = GetJsonStringOrNull(extractedData, "Medium");
                criteria.MinSalary = GetJsonIntOrNull(extractedData, "MinSalary");
                criteria.MaxSalary = GetJsonIntOrNull(extractedData, "MaxSalary");

                if (extractedData.TryGetProperty("Keywords", out var keywordsElement) && 
                    keywordsElement.ValueKind == JsonValueKind.Array)
                {
                    criteria.Keywords = keywordsElement.EnumerateArray()
                        .Where(k => k.ValueKind == JsonValueKind.String)
                        .Select(k => k.GetString()!)
                        .Where(k => !string.IsNullOrEmpty(k))
                        .ToList();
                }

                _logger.LogInformation($"AI extracted job criteria: Subject={criteria.Subject}, Class={criteria.ClassLevel}, City={criteria.City}");
                return criteria;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Gemini API for job search. Using fallback.");
                return ExtractJobCriteriaFallback(userPrompt);
            }
        }

        private JobSearchCriteria ExtractJobCriteriaFallback(string userPrompt)
        {
            var criteria = new JobSearchCriteria { OriginalPrompt = userPrompt };
            var promptLower = userPrompt.ToLower();

            // Subject detection
            var subjects = new[] { "math", "mathematics", "english", "physics", "chemistry", "biology", 
                                   "bangla", "bengali", "ict", "computer", "accounting", "economics", 
                                   "science", "social science", "history", "geography" };
            criteria.Subject = subjects.FirstOrDefault(s => promptLower.Contains(s));

            // Class level detection
            var classLevels = new Dictionary<string, string>
            {
                { "nursery", "Nursery" }, { "kg", "KG" }, { "kindergarten", "KG" },
                { "class 1", "Class 1" }, { "class 2", "Class 2" }, { "class 3", "Class 3" },
                { "class 4", "Class 4" }, { "class 5", "Class 5" }, { "class 6", "Class 6" },
                { "class 7", "Class 7" }, { "class 8", "Class 8" }, { "class 9", "Class 9" },
                { "class 10", "Class 10" }, { "hsc", "HSC" }, { "a-level", "A-Level" }
            };
            foreach (var kvp in classLevels)
            {
                if (promptLower.Contains(kvp.Key))
                {
                    criteria.ClassLevel = kvp.Value;
                    break;
                }
            }

            // City detection
            var cities = new[] { "dhaka", "chittagong", "sylhet", "khulna", "rajshahi", "rangpur", "barisal", "comilla" };
            criteria.City = cities.FirstOrDefault(c => promptLower.Contains(c));

            // Location detection
            var locations = new[] { "mirpur", "uttara", "dhanmondi", "gulshan", "banani", "mohammadpur", 
                                    "motijheel", "bashundhara", "badda", "rampura", "farmgate" };
            criteria.Location = locations.FirstOrDefault(l => promptLower.Contains(l));

            // Medium detection
            if (promptLower.Contains("english medium"))
                criteria.Medium = "English";
            else if (promptLower.Contains("bangla medium") || promptLower.Contains("bengali medium"))
                criteria.Medium = "Bangla";

            // Salary detection (simple pattern matching)
            var salaryMatch = System.Text.RegularExpressions.Regex.Match(promptLower, @"(\d{4,5})\s*(tk|taka|bdt)?");
            if (salaryMatch.Success && int.TryParse(salaryMatch.Groups[1].Value, out int salary))
            {
                criteria.MinSalary = salary;
            }

            // Keywords
            var keywords = new[] { "flexible", "nearby", "part-time", "full-time", "weekend", "online", "home" };
            criteria.Keywords = keywords.Where(k => promptLower.Contains(k)).ToList();

            return criteria;
        }

        public async Task<List<JobSearchResultViewModel>> SearchJobsAsync(JobSearchCriteria criteria)
        {
            var query = _context.TuitionOffers
                .Include(j => j.Guardian)
                .Where(j => j.Status == JobStatus.Open)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(criteria.City))
            {
                query = query.Where(j => j.City != null && 
                    j.City.ToLower().Contains(criteria.City.ToLower()));
            }

            if (!string.IsNullOrEmpty(criteria.Location))
            {
                query = query.Where(j => j.Location != null && 
                    j.Location.ToLower().Contains(criteria.Location.ToLower()));
            }

            if (!string.IsNullOrEmpty(criteria.ClassLevel))
            {
                query = query.Where(j => j.StudentClass != null && 
                    j.StudentClass.ToLower().Contains(criteria.ClassLevel.ToLower()));
            }

            if (!string.IsNullOrEmpty(criteria.Medium))
            {
                query = query.Where(j => j.Medium != null && 
                    j.Medium.ToLower().Contains(criteria.Medium.ToLower()));
            }

            if (criteria.MinSalary.HasValue)
            {
                query = query.Where(j => j.Salary >= criteria.MinSalary.Value);
            }

            var jobs = await query.OrderByDescending(j => j.CreatedAt).ToListAsync();

            var results = jobs.Select(j => new JobSearchResultViewModel
            {
                JobId = j.Id,
                Title = j.Title,
                Description = j.Description,
                Salary = j.Salary,
                City = j.City,
                Location = j.Location,
                Medium = j.Medium,
                StudentClass = j.StudentClass,
                Subject = j.Subject,
                DaysPerWeek = j.DaysPerWeek,
                CreatedAt = j.CreatedAt,
                GuardianName = j.Guardian?.FullName ?? j.Guardian?.UserName ?? "Guardian",
                MatchScore = CalculateJobMatchScore(j, criteria)
            })
            .OrderByDescending(j => j.MatchScore)
            .ThenByDescending(j => j.Salary)
            .ToList();

            // If no results with filters, try broad search
            if (!results.Any() && !string.IsNullOrEmpty(criteria.OriginalPrompt))
            {
                results = await BroadJobSearchAsync(criteria.OriginalPrompt);
            }

            return results;
        }

        private int CalculateJobMatchScore(TuitionOffer job, JobSearchCriteria criteria)
        {
            int score = 0;

            if (!string.IsNullOrEmpty(criteria.Subject))
            {
                var searchableText = $"{job.Title} {job.Description} {job.Subject}".ToLower();
                if (searchableText.Contains(criteria.Subject.ToLower()))
                    score += 30;
            }

            if (!string.IsNullOrEmpty(criteria.ClassLevel) && 
                job.StudentClass?.ToLower().Contains(criteria.ClassLevel.ToLower()) == true)
                score += 25;

            if (!string.IsNullOrEmpty(criteria.City) && 
                job.City?.ToLower().Contains(criteria.City.ToLower()) == true)
                score += 20;

            if (!string.IsNullOrEmpty(criteria.Location) && 
                job.Location?.ToLower().Contains(criteria.Location.ToLower()) == true)
                score += 15;

            if (!string.IsNullOrEmpty(criteria.Medium) && 
                job.Medium?.ToLower().Contains(criteria.Medium.ToLower()) == true)
                score += 10;

            // Salary bonus (higher salary = higher score)
            score += job.Salary / 500;

            // Recency bonus (newer jobs score higher)
            var daysSincePosted = (DateTime.Now - job.CreatedAt).Days;
            if (daysSincePosted <= 7) score += 10;
            else if (daysSincePosted <= 14) score += 5;

            // Keyword matches in description
            if (criteria.Keywords.Any() && !string.IsNullOrEmpty(job.Description))
            {
                var descLower = job.Description.ToLower();
                var keywordMatches = criteria.Keywords.Count(k => descLower.Contains(k.ToLower()));
                score += keywordMatches * 5;
            }

            return score;
        }

        private async Task<List<JobSearchResultViewModel>> BroadJobSearchAsync(string searchText)
        {
            var words = searchText.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 2).ToList();

            var jobs = await _context.TuitionOffers
                .Include(j => j.Guardian)
                .Where(j => j.Status == JobStatus.Open)
                .OrderByDescending(j => j.CreatedAt)
                .ToListAsync();

            return jobs
                .Select(j => new JobSearchResultViewModel
                {
                    JobId = j.Id,
                    Title = j.Title,
                    Description = j.Description,
                    Salary = j.Salary,
                    City = j.City,
                    Location = j.Location,
                    Medium = j.Medium,
                    StudentClass = j.StudentClass,
                    Subject = j.Subject,
                    DaysPerWeek = j.DaysPerWeek,
                    CreatedAt = j.CreatedAt,
                    GuardianName = j.Guardian?.FullName ?? "Guardian",
                    MatchScore = CalculateBroadJobScore(j, words)
                })
                .Where(j => j.MatchScore > 0)
                .OrderByDescending(j => j.MatchScore)
                .ThenByDescending(j => j.Salary)
                .Take(20)
                .ToList();
        }

        private int CalculateBroadJobScore(TuitionOffer job, List<string> words)
        {
            var searchableText = $"{job.Title} {job.Description} {job.Subject} {job.City} {job.Location} {job.StudentClass}".ToLower();
            return words.Count(word => searchableText.Contains(word)) * 5;
        }

        #endregion

        #region Helper Methods

        private string? GetJsonStringOrNull(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.String)
                {
                    var value = prop.GetString();
                    return string.IsNullOrWhiteSpace(value) || value.Equals("null", StringComparison.OrdinalIgnoreCase) 
                        ? null : value;
                }
            }
            return null;
        }

        private int? GetJsonIntOrNull(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.Number)
                    return prop.GetInt32();
                if (prop.ValueKind == JsonValueKind.String && int.TryParse(prop.GetString(), out int val))
                    return val;
            }
            return null;
        }

        #endregion
    }
}
