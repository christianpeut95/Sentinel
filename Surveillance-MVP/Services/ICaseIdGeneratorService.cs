using System.Threading.Tasks;

namespace Surveillance_MVP.Services
{
    public interface ICaseIdGeneratorService
    {
        Task<string> GenerateNextCaseIdAsync();
    }
}
