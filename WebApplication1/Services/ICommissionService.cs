using System.Threading.Tasks;

namespace TutorHubBD.Web.Services
{
    public interface ICommissionService
    {
        Task CreateInvoiceAsync(int jobId, decimal salary);
    }
}
