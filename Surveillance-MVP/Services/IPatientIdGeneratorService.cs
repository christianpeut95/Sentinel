using System.Threading.Tasks;

namespace Surveillance_MVP.Services
{
    public interface IPatientIdGeneratorService
    {
        Task<string> GenerateNextPatientIdAsync();
    }
}
