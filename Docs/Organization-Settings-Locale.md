# Organization Settings & Geocoding Locale

## Overview

The Organization Settings feature allows administrators to configure company information and default location settings. This information is used throughout the application to provide more relevant and localized address lookups and geocoding results.

## Key Features

### 1. Organization Information
Configure your organization's details:
- **Organization Name**: The name of your health district or organization
- **Country**: Primary country of operation
- **State/Province**: Optional state or province
- **City/Region**: Optional primary city or region
- **Postal Code**: Optional postal/ZIP code
- **Country Code**: ISO 3166-1 alpha-2 country code (e.g., AU, US, GB, NZ)
- **Timezone**: IANA timezone identifier (e.g., Australia/Sydney)

### 2. Geocoding Bias

When a **Country Code** is configured, the system automatically:
- **Biases address autocomplete suggestions** to prioritize results from your country
- **Filters geocoding results** to prefer locations in your configured region
- **Improves search relevance** without requiring users to specify the country every time

#### Example Scenarios

**Without Country Code**:
- Searching for "Sydney" returns results from Sydney, Australia AND Sydney, Canada
- User must manually specify "Sydney, Australia" to get the correct result

**With Country Code = "AU"**:
- Searching for "Sydney" automatically prioritizes Sydney, Australia
- All address lookups are biased to Australian locations
- Users can still override by specifying a different country explicitly

### 3. Integration Points

The locale settings are automatically used by:

1. **Google Geocoding Service**
   - Adds `components=country:{CountryCode}` parameter to geocoding requests
   - Filters results to the specified country

2. **Nominatim Geocoding Service**
   - Adds `countrycodes={countrycode}` parameter to search requests
   - Restricts results to the specified country

3. **Address Autocomplete API** (`/api/address-suggest`)
   - Adds `components=country:{CountryCode}` to Google Places Autocomplete
   - All address suggestions prioritize your configured region

## Configuration

### Via Web UI (Recommended)

1. Navigate to **Settings > Organization Settings**
2. Fill in your organization details
3. Set the **Country Code** (e.g., AU for Australia)
4. Click **Save Organization Settings**

### Via appsettings.json

You can also manually edit `appsettings.json`:

```json
{
  "Organization": {
    "Name": "Sydney Health District",
    "Country": "Australia",
    "State": "New South Wales",
    "City": "Sydney",
    "PostalCode": "2000",
    "CountryCode": "AU",
    "Timezone": "Australia/Sydney"
  }
}
```

## Country Codes Reference

Common ISO 3166-1 alpha-2 country codes:

| Country | Code |
|---------|------|
| Australia | AU |
| United States | US |
| United Kingdom | GB |
| New Zealand | NZ |
| Canada | CA |
| Ireland | IE |
| Singapore | SG |
| South Africa | ZA |

Full list: [ISO 3166-1 alpha-2 codes](https://en.wikipedia.org/wiki/ISO_3166-1_alpha-2)

## Technical Details

### Geocoding Services

Both geocoding services have been updated to use the organization country code:

**GoogleGeocodingService.cs**:
```csharp
// Reads Organization:CountryCode from configuration
private readonly string? _countryCode;

// Adds country bias to geocoding requests
if (!string.IsNullOrWhiteSpace(_countryCode))
{
    url += $"&components=country:{Uri.EscapeDataString(_countryCode)}";
}
```

**NominatimGeocodingService.cs**:
```csharp
// Reads Organization:CountryCode from configuration
private readonly string? _countryCode;

// Restricts results to specified country
if (!string.IsNullOrWhiteSpace(_countryCode))
{
    url += $"&countrycodes={Uri.EscapeDataString(_countryCode.ToLowerInvariant())}";
}
```

### Address Autocomplete API

The `/api/address-suggest` endpoint in `Program.cs` has been updated:

```csharp
// Get organization country code for biasing results
var countryCode = app.Configuration["Organization:CountryCode"];

// Add country code bias if configured
if (!string.IsNullOrWhiteSpace(countryCode))
{
    autoUrl += $"&components=country:{Uri.EscapeDataString(countryCode)}";
}
```

## Best Practices

1. **Always configure the Country Code** for your primary region to improve geocoding accuracy
2. **Use the full organization name** for proper identification in reports and exports
3. **Keep timezone settings up-to-date** for accurate timestamp displays
4. **Update settings** if your organization relocates or expands to new regions

## Override Behavior

The country code bias can be overridden by:
- **Explicitly specifying a country** in the address field (e.g., "123 Main St, Toronto, Canada")
- **Using full international addresses** that include country names
- The bias is a **preference, not a restriction** - results from other countries are still possible if explicitly specified

## Security

- Organization settings require **Admin role** to modify
- Settings are stored in `appsettings.json` and persisted across restarts
- No sensitive information should be stored in these fields

## Troubleshooting

### Address lookups still returning wrong country
- Verify Country Code is set correctly (2 uppercase letters)
- Ensure Country Code matches your region (e.g., AU for Australia, not US)
- Check that appsettings.json was saved properly after making changes

### Settings not taking effect
- Restart the application after manually editing appsettings.json
- Verify no JSON syntax errors in the configuration file
- Check that the geocoding service is properly configured

### Country Code validation error
- Country code must be exactly 2 uppercase letters
- Use ISO 3166-1 alpha-2 format (AU, not AUS)
- Check for typos or extra spaces

## Future Enhancements

Potential future improvements:
- Multi-region support for organizations operating in multiple countries
- Automatic timezone detection based on location
- Region-specific date/time formatting
- Localized content and translations
- Configurable address format preferences
