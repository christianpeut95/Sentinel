using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Services;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Sentinel.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [EnableRateLimiting("sensitive-data")]
    public class CasesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IDiseaseAccessService _diseaseAccessService;

        public CasesController(ApplicationDbContext context, IDiseaseAccessService diseaseAccessService)
        {
            _context = context;
            _diseaseAccessService = diseaseAccessService;
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search(
            [FromQuery] string? query = null, 
            [FromQuery] string? term = null,
            [FromQuery] string? type = null,
            [FromQuery] Guid? excludeCaseId = null,
            [FromQuery] Guid? diseaseId = null,
            [FromQuery] int? contactType = null)
        {
            try
            {
                // Support both 'query' and 'term' parameter names for backward compatibility
                var searchTerm = !string.IsNullOrWhiteSpace(term) ? term : query;
                
                if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length < 2)
                {
                    return Ok(new object[] { });
                }

                var queryLower = searchTerm.ToLower();

                // Get accessible diseases for the current user
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                List<Guid> accessibleDiseaseIds = new List<Guid>();
                
                if (!string.IsNullOrEmpty(userId))
                {
                    try
                    {
                        accessibleDiseaseIds = await _diseaseAccessService.GetAccessibleDiseaseIdsAsync(userId);
                    }
                    catch (Exception)
                    {
                        // If disease access check fails, allow all diseases (fallback to no filtering)
                        accessibleDiseaseIds = new List<Guid>();
                    }
                }

                // Build the base query
                var casesQuery = _context.Cases
                    .Include(c => c.Patient)
                    .Include(c => c.Disease)
                    .Include(c => c.ConfirmationStatus)
                    .AsQueryable();

                // Apply disease access filter only if there are specific diseases to filter
                if (accessibleDiseaseIds != null && accessibleDiseaseIds.Any())
                {
                    casesQuery = casesQuery.Where(c => c.DiseaseId == null || accessibleDiseaseIds.Contains(c.DiseaseId.Value));
                }

                // Exclude specific case (when searching for related cases)
                if (excludeCaseId.HasValue)
                {
                    casesQuery = casesQuery.Where(c => c.Id != excludeCaseId.Value);
                }

                // Filter by disease if specified
                if (diseaseId.HasValue)
                {
                    casesQuery = casesQuery.Where(c => c.DiseaseId == diseaseId.Value);
                }

                // Filter by contactType (1 = Case, 2 = Contact)
                if (contactType.HasValue)
                {
                    if (contactType.Value == 1)
                    {
                        casesQuery = casesQuery.Where(c => c.Type == CaseType.Case);
                    }
                    else if (contactType.Value == 2)
                    {
                        casesQuery = casesQuery.Where(c => c.Type == CaseType.Contact);
                    }
                }

                // Filter by type if specified (e.g., only "Case" types when searching for source cases)
                if (!string.IsNullOrWhiteSpace(type))
                {
                    if (Enum.TryParse<CaseType>(type, out var caseType))
                    {
                        casesQuery = casesQuery.Where(c => c.Type == caseType);
                    }
                }

                // Search by friendly ID, patient name, or disease name
                var cases = await casesQuery
                    .Where(c => 
                        c.FriendlyId.ToLower().Contains(queryLower) ||
                        (c.Patient != null && (c.Patient.GivenName.ToLower().Contains(queryLower) || 
                                              c.Patient.FamilyName.ToLower().Contains(queryLower))) ||
                        (c.Disease != null && c.Disease.Name.ToLower().Contains(queryLower))
                    )
                    .OrderByDescending(c => c.DateOfNotification)
                    .Take(20)
                    .Select(c => new
                    {
                        id = c.Id,
                        friendlyId = c.FriendlyId,
                        patientName = c.Patient != null ? $"{c.Patient.GivenName} {c.Patient.FamilyName}" : "Unknown",
                        diseaseName = c.Disease != null ? c.Disease.Name : "Not specified",
                        onsetDate = c.DateOfOnset.HasValue ? c.DateOfOnset.Value.ToString("dd MMM yyyy") : null,
                        dateOfNotification = c.DateOfNotification.HasValue ? c.DateOfNotification.Value.ToString("dd MMM yyyy") : "N/A",
                        status = c.ConfirmationStatus != null ? c.ConfirmationStatus.Name : null,
                        type = c.Type.ToString()
                    })
                    .ToListAsync();

                return Ok(cases);
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Error in case search: {ex.Message}");
                
                // Return empty result instead of 500 error to avoid breaking UI
                return Ok(new object[] { });
            }
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var caseItem = await _context.Cases
                .Include(c => c.Patient)
                    .ThenInclude(p => p.Gender)
                .Include(c => c.Patient)
                    .ThenInclude(p => p.SexAtBirth)
                .Include(c => c.Disease)
                .Include(c => c.ConfirmationStatus)
                .Where(c => c.Id == id)
                .Select(c => new
                {
                    id = c.Id,
                    friendlyId = c.FriendlyId,
                    type = c.Type.ToString(),
                    patientId = c.PatientId,
                    patientName = c.Patient != null ? $"{c.Patient.GivenName} {c.Patient.FamilyName}" : "Unknown",
                    diseaseId = c.DiseaseId,
                    diseaseName = c.Disease != null ? c.Disease.Name : null,
                    confirmationStatusId = c.ConfirmationStatusId,
                    confirmationStatusName = c.ConfirmationStatus != null ? c.ConfirmationStatus.Name : null,
                    dateOfOnset = c.DateOfOnset.HasValue ? c.DateOfOnset.Value.ToString("yyyy-MM-dd") : null,
                    dateOfNotification = c.DateOfNotification.HasValue ? c.DateOfNotification.Value.ToString("yyyy-MM-dd") : null
                })
                .FirstOrDefaultAsync();

            if (caseItem == null)
            {
                return NotFound();
            }

            return Ok(caseItem);
        }
    }
}
