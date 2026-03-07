using System.Collections.Generic;
using System.Threading.Tasks;
using Sentinel.Models.Lookups;

namespace Sentinel.Services
{
    public interface IOccupationImportService
    {
        Task<ImportResult> ImportFromExcelAsync(Stream fileStream);
    }

    public class ImportResult
    {
        public bool Success { get; set; }
        public int RecordsImported { get; set; }
        public int RecordsSkipped { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }
}
