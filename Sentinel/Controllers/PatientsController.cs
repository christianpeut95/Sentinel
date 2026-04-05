using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Sentinel.Controllers
{
    // DTO for patient update request
    public class UpdatePatientRequest
    {
        public Guid Id { get; set; }
        public string GivenName { get; set; } = string.Empty;
        public string FamilyName { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public int? GenderId { get; set; }
        public int? SexAtBirthId { get; set; }
        public int? OccupationId { get; set; }
        public string? EmailAddress { get; set; }
        public string? MobilePhone { get; set; }
        public string? HomePhone { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public int? StateId { get; set; }
        public string? Postcode { get; set; }
        public int? CountryOfBirthId { get; set; }
        public int? LanguageSpokenAtHomeId { get; set; }
        public int? AncestryId { get; set; }
        public int? AtsiStatusId { get; set; }
    }

    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [EnableRateLimiting("sensitive-data")]
    public class PatientsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IPatientDuplicateCheckService _duplicateCheckService;
        private readonly UserManager<ApplicationUser> _userManager;

        public PatientsController(ApplicationDbContext context, IPatientDuplicateCheckService duplicateCheckService, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _duplicateCheckService = duplicateCheckService;
            _userManager = userManager;
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            {
                return Ok(new object[] { });
            }

            var queryLower = query.ToLower();

            // Search patients by name or ID (DOB search done client-side)
            var patients = await _context.Patients
                .Where(p => 
                    p.GivenName.ToLower().Contains(queryLower) ||
                    p.FamilyName.ToLower().Contains(queryLower) ||
                    p.FriendlyId.ToLower().Contains(queryLower)
                )
                .OrderBy(p => p.FamilyName)
                .ThenBy(p => p.GivenName)
                .Take(50) // Get more results to filter client-side
                .ToListAsync();

            // Filter by date of birth client-side if needed
            if (DateTime.TryParse(query, out var searchDate))
            {
                patients = patients
                    .Where(p => p.DateOfBirth.HasValue && p.DateOfBirth.Value.Date == searchDate.Date)
                    .ToList();
            }

            // Take only top 20 after client-side filtering
            patients = patients.Take(20).ToList();

            // Check for duplicates using existing service
            var results = new List<object>();
            foreach (var patient in patients)
            {
                var duplicates = await _duplicateCheckService.FindPotentialDuplicatesAsync(patient);
                var hasDuplicate = duplicates.Any(d => d.MatchScore >= 50); // High confidence match

                results.Add(new
                {
                    id = patient.Id,
                    name = $"{patient.GivenName} {patient.FamilyName}",
                    dateOfBirth = patient.DateOfBirth.HasValue ? patient.DateOfBirth.Value.ToString("yyyy-MM-dd") : null,
                    friendlyId = patient.FriendlyId,
                    possibleDuplicate = hasDuplicate,
                    duplicateCount = duplicates.Count(d => d.MatchScore >= 30)
                });
            }

            return Ok(results);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var patient = await _context.Patients
                .Include(p => p.Gender)
                .Include(p => p.SexAtBirth)
                .Include(p => p.Occupation)
                .Include(p => p.CountryOfBirth)
                .Include(p => p.LanguageSpokenAtHome)
                .Include(p => p.Ancestry)
                .Include(p => p.AtsiStatus)
                .Where(p => p.Id == id)
                .Select(p => new
                {
                    id = p.Id,
                    name = $"{p.GivenName} {p.FamilyName}",
                    givenName = p.GivenName,
                    familyName = p.FamilyName,
                    dateOfBirth = p.DateOfBirth.HasValue ? p.DateOfBirth.Value.ToString("yyyy-MM-dd") : null,
                    friendlyId = p.FriendlyId,
                    genderId = p.GenderId,
                    genderName = p.Gender != null ? p.Gender.Name : null,
                    sexAtBirthId = p.SexAtBirthId,
                    sexAtBirthName = p.SexAtBirth != null ? p.SexAtBirth.Name : null,
                    occupationId = p.OccupationId,
                    occupationName = p.Occupation != null ? p.Occupation.Name : null,
                    emailAddress = p.EmailAddress,
                    mobilePhone = p.MobilePhone,
                    homePhone = p.HomePhone,
                    address = p.AddressLine,
                    city = p.City,
                    state = p.State,
                    postcode = p.PostalCode,
                    countryOfBirthId = p.CountryOfBirthId,
                    countryOfBirthName = p.CountryOfBirth != null ? p.CountryOfBirth.Name : null,
                    languageSpokenAtHomeId = p.LanguageSpokenAtHomeId,
                    languageSpokenAtHomeName = p.LanguageSpokenAtHome != null ? p.LanguageSpokenAtHome.Name : null,
                    AncestryId = p.AncestryId,
                    AncestryName = p.Ancestry != null ? p.Ancestry.Name : null,
                    atsiStatusId = p.AtsiStatusId,
                    atsiStatusName = p.AtsiStatus != null ? p.AtsiStatus.Name : null
                })
                .FirstOrDefaultAsync();

            if (patient == null)
            {
                return NotFound();
            }

            return Ok(patient);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "Permission.Patient.Edit")]
        public async Task<IActionResult> UpdatePatient(Guid id, [FromBody] UpdatePatientRequest request)
        {
            if (id != request.Id)
            {
                return BadRequest("ID mismatch");
            }

            var patient = await _context.Patients.FindAsync(id);
            if (patient == null)
            {
                return NotFound();
            }

            // Update fields
            patient.GivenName = request.GivenName;
            patient.FamilyName = request.FamilyName;
            patient.DateOfBirth = request.DateOfBirth;
            patient.GenderId = request.GenderId;
            patient.SexAtBirthId = request.SexAtBirthId;
            patient.OccupationId = request.OccupationId;
            patient.EmailAddress = request.EmailAddress;
            patient.MobilePhone = request.MobilePhone;
            patient.HomePhone = request.HomePhone;
            patient.AddressLine = request.Address;
            patient.City = request.City;
            patient.StateId = request.StateId;
            patient.PostalCode = request.Postcode;
            patient.CountryOfBirthId = request.CountryOfBirthId;
            patient.LanguageSpokenAtHomeId = request.LanguageSpokenAtHomeId;
            patient.AncestryId = request.AncestryId;
            patient.AtsiStatusId = request.AtsiStatusId;

            try
            {
                await _context.SaveChangesAsync();

                // Return updated patient data
                var updatedPatient = await _context.Patients
                    .Include(p => p.Gender)
                    .Include(p => p.SexAtBirth)
                    .Include(p => p.Occupation)
                    .Include(p => p.CountryOfBirth)
                    .Include(p => p.LanguageSpokenAtHome)
                    .Include(p => p.Ancestry)
                    .Include(p => p.AtsiStatus)
                    .Where(p => p.Id == id)
                    .Select(p => new
                    {
                        id = p.Id,
                        name = $"{p.GivenName} {p.FamilyName}",
                        givenName = p.GivenName,
                        familyName = p.FamilyName,
                        dateOfBirth = p.DateOfBirth.HasValue ? p.DateOfBirth.Value.ToString("yyyy-MM-dd") : null,
                        friendlyId = p.FriendlyId,
                        genderId = p.GenderId,
                        genderName = p.Gender != null ? p.Gender.Name : null,
                        sexAtBirthId = p.SexAtBirthId,
                        sexAtBirthName = p.SexAtBirth != null ? p.SexAtBirth.Name : null,
                        occupationId = p.OccupationId,
                        occupationName = p.Occupation != null ? p.Occupation.Name : null,
                        emailAddress = p.EmailAddress,
                        mobilePhone = p.MobilePhone,
                        homePhone = p.HomePhone,
                        address = p.AddressLine,
                        city = p.City,
                        state = p.State,
                        postcode = p.PostalCode,
                        countryOfBirthId = p.CountryOfBirthId,
                        countryOfBirthName = p.CountryOfBirth != null ? p.CountryOfBirth.Name : null,
                        languageSpokenAtHomeId = p.LanguageSpokenAtHomeId,
                        languageSpokenAtHomeName = p.LanguageSpokenAtHome != null ? p.LanguageSpokenAtHome.Name : null,
                        AncestryId = p.AncestryId,
                        AncestryName = p.Ancestry != null ? p.Ancestry.Name : null,
                        atsiStatusId = p.AtsiStatusId,
                        atsiStatusName = p.AtsiStatus != null ? p.AtsiStatus.Name : null
                    })
                    .FirstOrDefaultAsync();

                return Ok(updatedPatient);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error updating patient: " + ex.Message);
            }
        }

        [HttpGet("{id}/duplicates")]
        public async Task<IActionResult> GetDuplicates(Guid id)
        {
            var patient = await _context.Patients.FindAsync(id);
            if (patient == null)
            {
                return NotFound();
            }

            var duplicates = await _duplicateCheckService.FindPotentialDuplicatesAsync(patient);

            var results = duplicates.Select(d => new
            {
                id = d.Patient.Id,
                name = $"{d.Patient.GivenName} {d.Patient.FamilyName}",
                dateOfBirth = d.Patient.DateOfBirth.HasValue ? d.Patient.DateOfBirth.Value.ToString("yyyy-MM-dd") : null,
                friendlyId = d.Patient.FriendlyId,
                matchScore = d.MatchScore,
                matchReasons = d.MatchReasons
            });

            return Ok(results);
        }

        [HttpGet("SearchUsers")]
        public async Task<IActionResult> SearchUsers([FromQuery] string term)
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
            {
                return Ok(new object[] { });
            }

            var termLower = term.ToLower();

            // Search users by username or email
            var users = await _userManager.Users
                .Where(u => (u.UserName != null && u.UserName.ToLower().Contains(termLower)) ||
                           (u.Email != null && u.Email.ToLower().Contains(termLower)))
                .OrderBy(u => u.UserName)
                .Take(20)
                .Select(u => new
                {
                    id = u.Id,
                    text = u.UserName ?? u.Email ?? "Unknown User"
                })
                .ToListAsync();

            return Ok(users);
        }
    }
}

