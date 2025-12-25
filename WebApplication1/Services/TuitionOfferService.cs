using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TutorHubBD.Web.Data;
using TutorHubBD.Web.Models;

namespace TutorHubBD.Web.Services
{
    public class TuitionOfferService : ITuitionOfferService
    {
        private readonly ApplicationDbContext _context;

        public TuitionOfferService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<TuitionOffer>> SearchOffersAsync(string city, string medium, string studentClass)
        {
            var jobs = from j in _context.TuitionOffers
                       select j;

            if (!string.IsNullOrEmpty(city))
            {
                jobs = jobs.Where(s => s.City.Contains(city));
            }

            if (!string.IsNullOrEmpty(medium))
            {
                jobs = jobs.Where(s => s.Medium.Contains(medium));
            }

            if (!string.IsNullOrEmpty(studentClass))
            {
                jobs = jobs.Where(s => s.StudentClass.Contains(studentClass));
            }

            return await jobs.ToListAsync();
        }

        public async Task CreateOfferAsync(TuitionOffer offer)
        {
            _context.Add(offer);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteOfferAsync(int id)
        {
            var offer = await _context.TuitionOffers.FindAsync(id);
            if (offer != null)
            {
                _context.TuitionOffers.Remove(offer);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<TuitionOffer?> GetOfferByIdAsync(int id)
        {
            return await _context.TuitionOffers.FindAsync(id);
        }
    }
}
