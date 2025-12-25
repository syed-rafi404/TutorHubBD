using System.Collections.Generic;
using System.Threading.Tasks;
using TutorHubBD.Web.Models;

namespace TutorHubBD.Web.Services
{
    public interface ITuitionOfferService
    {
        Task<List<TuitionOffer>> SearchOffersAsync(string city, string medium, string studentClass);
        Task CreateOfferAsync(TuitionOffer offer);
        Task DeleteOfferAsync(int id);
        Task<TuitionOffer?> GetOfferByIdAsync(int id);
    }
}
