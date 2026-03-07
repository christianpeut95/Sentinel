using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.DTOs;
using Sentinel.Models;

namespace Sentinel.Services;

public interface IDuplicateDetectionService
{
    Task<List<DuplicateDetectionResult>> AnalyzeBulkContactsAsync(List<BulkContactDto> contacts);
}

public class DuplicateDetectionService : IDuplicateDetectionService
{
    private readonly ApplicationDbContext _context;

    public DuplicateDetectionService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<DuplicateDetectionResult>> AnalyzeBulkContactsAsync(List<BulkContactDto> contacts)
    {
        var results = new List<DuplicateDetectionResult>();
        int rowNum = 1;

        foreach (var contact in contacts)
        {
            var matches = await FindPotentialMatchesAsync(contact);
            
            results.Add(new DuplicateDetectionResult
            {
                RowNumber = rowNum++,
                ProposedContact = contact,
                IsPotentialDuplicate = matches.Any(),
                PossibleMatches = matches,
                Confidence = CalculateMatchConfidence(contact, matches)
            });
        }

        return results;
    }

    private async Task<List<Patient>> FindPotentialMatchesAsync(BulkContactDto contact)
    {
        var matches = new List<Patient>();

        // Match 1: Exact name + DOB match
        if (contact.DateOfBirth.HasValue)
        {
            var nameAndDobMatches = await _context.Patients
                .Where(p => p.GivenName == contact.FirstName 
                         && p.FamilyName == contact.LastName 
                         && p.DateOfBirth == contact.DateOfBirth)
                .Take(5)
                .ToListAsync();
            
            matches.AddRange(nameAndDobMatches);
        }

        // Match 2: Phone number match (if provided and unique)
        if (!string.IsNullOrWhiteSpace(contact.ContactPhone))
        {
            var phoneMatches = await _context.Patients
                .Where(p => p.MobilePhone == contact.ContactPhone || p.HomePhone == contact.ContactPhone)
                .Take(5)
                .ToListAsync();
            
            foreach (var match in phoneMatches)
            {
                if (!matches.Any(m => m.Id == match.Id))
                    matches.Add(match);
            }
        }

        // Match 3: Email match
        if (!string.IsNullOrWhiteSpace(contact.Email))
        {
            var emailMatches = await _context.Patients
                .Where(p => p.EmailAddress == contact.Email)
                .Take(5)
                .ToListAsync();
            
            foreach (var match in emailMatches)
            {
                if (!matches.Any(m => m.Id == match.Id))
                    matches.Add(match);
            }
        }

        // Match 4: Similar name (same last name, similar first name)
        if (contact.DateOfBirth.HasValue)
        {
            var similarNameMatches = await _context.Patients
                .Where(p => p.FamilyName == contact.LastName
                         && p.DateOfBirth == contact.DateOfBirth
                         && p.GivenName.StartsWith(contact.FirstName.Substring(0, Math.Min(3, contact.FirstName.Length))))
                .Take(5)
                .ToListAsync();
            
            foreach (var match in similarNameMatches)
            {
                if (!matches.Any(m => m.Id == match.Id))
                    matches.Add(match);
            }
        }

        return matches.Take(5).ToList();
    }

    private MatchConfidence CalculateMatchConfidence(BulkContactDto contact, List<Patient> matches)
    {
        if (!matches.Any())
            return MatchConfidence.None;

        // High confidence: Exact name + DOB + phone/email match
        var exactMatch = matches.FirstOrDefault(m => 
            m.GivenName == contact.FirstName 
            && m.FamilyName == contact.LastName 
            && m.DateOfBirth == contact.DateOfBirth
            && (m.MobilePhone == contact.ContactPhone || m.HomePhone == contact.ContactPhone || m.EmailAddress == contact.Email));

        if (exactMatch != null)
            return MatchConfidence.High;

        // Medium confidence: Name + DOB match OR phone match
        var nameDobMatch = matches.FirstOrDefault(m => 
            m.GivenName == contact.FirstName 
            && m.FamilyName == contact.LastName 
            && m.DateOfBirth == contact.DateOfBirth);

        var phoneMatch = !string.IsNullOrWhiteSpace(contact.ContactPhone) 
            && matches.Any(m => m.MobilePhone == contact.ContactPhone || m.HomePhone == contact.ContactPhone);

        if (nameDobMatch != null || phoneMatch)
            return MatchConfidence.Medium;

        // Low confidence: Similar name or other partial matches
        return MatchConfidence.Low;
    }
}

public class DuplicateDetectionResult
{
    public int RowNumber { get; set; }
    public BulkContactDto ProposedContact { get; set; } = new();
    public bool IsPotentialDuplicate { get; set; }
    public List<Patient> PossibleMatches { get; set; } = new();
    public MatchConfidence Confidence { get; set; }
}

public enum MatchConfidence
{
    None = 0,
    Low = 1,
    Medium = 2,
    High = 3
}
