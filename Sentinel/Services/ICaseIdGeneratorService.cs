using System.Threading.Tasks;

namespace Sentinel.Services
{
    public interface ICaseIdGeneratorService
    {
        Task<string> GenerateNextCaseIdAsync();
    }
}
