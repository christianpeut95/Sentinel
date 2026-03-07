using System.Threading.Tasks;

namespace Sentinel.Services
{
    public interface IPatientIdGeneratorService
    {
        Task<string> GenerateNextPatientIdAsync();
    }
}
