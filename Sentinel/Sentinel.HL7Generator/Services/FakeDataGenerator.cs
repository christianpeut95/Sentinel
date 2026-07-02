using Sentinel.HL7Generator.Models;

namespace Sentinel.HL7Generator.Services;

/// <summary>
/// Generates realistic fake data for testing
/// </summary>
public class FakeDataGenerator
{
    private readonly Random _random = new();

    private static readonly string[] FamilyNames = 
    {
        "SMITH", "JOHNSON", "WILLIAMS", "BROWN", "JONES", "GARCIA", "MARTINEZ", 
        "RODRIGUEZ", "DAVIS", "MILLER", "WILSON", "MOORE", "TAYLOR", "ANDERSON",
        "THOMAS", "JACKSON", "WHITE", "HARRIS", "MARTIN", "THOMPSON", "ROBINSON",
        "CLARK", "LEWIS", "LEE", "WALKER", "HALL", "ALLEN", "YOUNG", "KING", "WRIGHT"
    };

    private static readonly string[] GivenNames =
    {
        "JAMES", "MARY", "JOHN", "PATRICIA", "ROBERT", "JENNIFER", "MICHAEL", "LINDA",
        "WILLIAM", "ELIZABETH", "DAVID", "BARBARA", "RICHARD", "SUSAN", "JOSEPH", "JESSICA",
        "THOMAS", "SARAH", "CHARLES", "KAREN", "CHRISTOPHER", "NANCY", "DANIEL", "LISA",
        "MATTHEW", "BETTY", "ANTHONY", "MARGARET", "MARK", "SANDRA", "DONALD", "ASHLEY"
    };

    private static readonly string[] StreetNames =
    {
        "MAIN", "OAK", "PINE", "MAPLE", "ELM", "CEDAR", "PARK", "WASHINGTON",
        "LAKE", "HILL", "FIRST", "SECOND", "THIRD", "CHURCH", "MARKET"
    };

    private static readonly string[] Cities =
    {
        "SPRINGFIELD", "RIVERSIDE", "FAIRVIEW", "CLINTON", "MADISON", "GEORGETOWN",
        "SALEM", "FRANKLIN", "ARLINGTON", "CENTERVILLE", "LEBANON", "KINGSTON"
    };

    private static readonly string[] States =
    {
        "CA", "TX", "FL", "NY", "PA", "IL", "OH", "GA", "NC", "MI",
        "NJ", "VA", "WA", "AZ", "MA", "TN", "IN", "MO", "MD", "WI"
    };

    public PatientInfo GeneratePatient()
    {
        return new PatientInfo
        {
            MRN = GenerateMRN(),
            FamilyName = GetRandomElement(FamilyNames),
            GivenName = GetRandomElement(GivenNames),
            MiddleName = GetRandomElement(GivenNames).Substring(0, 1),
            DateOfBirth = GenerateDateOfBirth(),
            Gender = GenerateGender(),
            AddressLine1 = GenerateAddress(),
            City = GetRandomElement(Cities),
            State = GetRandomElement(States),
            ZipCode = GenerateZipCode(),
            PhoneNumber = GeneratePhoneNumber()
        };
    }

    public ProviderInfo GenerateProvider()
    {
        return new ProviderInfo
        {
            FamilyName = GetRandomElement(FamilyNames),
            GivenName = GetRandomElement(GivenNames),
            NPI = GenerateNPI(),
            Organization = $"{GetRandomElement(Cities)} MEDICAL GROUP"
        };
    }

    public string GenerateMRN()
    {
        return _random.Next(10000000, 99999999).ToString();
    }

    public string GenerateAccessionNumber()
    {
        return $"ACC{DateTime.Now:yyyyMMdd}{_random.Next(10000, 99999)}";
    }

    public string GenerateMessageControlId()
    {
        return $"MSG{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
    }

    private string GenerateGender()
    {
        var genders = new[] { "M", "F", "F", "M" }; // Roughly equal distribution
        return GetRandomElement(genders);
    }

    private DateTime GenerateDateOfBirth()
    {
        var yearsOld = _random.Next(18, 80);
        var daysOffset = _random.Next(0, 365);
        return DateTime.Now.AddYears(-yearsOld).AddDays(-daysOffset).Date;
    }

    private string GenerateAddress()
    {
        var streetNumber = _random.Next(100, 9999);
        var streetName = GetRandomElement(StreetNames);
        var streetType = GetRandomElement(new[] { "ST", "AVE", "DR", "LN", "CT" });
        return $"{streetNumber} {streetName} {streetType}";
    }

    private string GenerateZipCode()
    {
        return _random.Next(10000, 99999).ToString();
    }

    private string GeneratePhoneNumber()
    {
        return $"555-{_random.Next(1000, 9999)}";
    }

    private string GenerateNPI()
    {
        return $"1{_random.Next(100000000, 999999999)}";
    }

    private T GetRandomElement<T>(T[] array)
    {
        return array[_random.Next(array.Length)];
    }
}
