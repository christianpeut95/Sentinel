# Case Definition Criteria Builder - API Reference

## API Endpoints

### 1. Get Custom Fields for Case Definitions

**Endpoint:** `GET /api/CustomFields/ForCaseDefinitions`

**Description:** Returns all active custom fields that are enabled for case forms.

**Authorization:** Required (Bearer token)

**Response:**
```json
[
  {
    "id": 1,
    "name": "smoking_status",
    "label": "Smoking Status",
    "category": "Risk Factors",
    "fieldType": "Dropdown",
    "hasLookupTable": true
  },
  {
    "id": 2,
    "name": "age",
    "label": "Age",
    "category": "Demographics",
    "fieldType": "Number",
    "hasLookupTable": false
  },
  {
    "id": 3,
    "name": "fever",
    "label": "Fever Present",
    "category": "Symptoms",
    "fieldType": "Boolean",
    "hasLookupTable": false
  }
]
```

**Status Codes:**
- `200 OK`: Success
- `401 Unauthorized`: Not authenticated
- `500 Internal Server Error`: Server error

---

### 2. Get Custom Field Details

**Endpoint:** `GET /api/CustomFields/{id}`

**Description:** Returns detailed information about a specific custom field, including lookup values if applicable.

**Parameters:**
- `id` (path, integer, required): The custom field ID

**Authorization:** Required (Bearer token)

**Response (with lookup table):**
```json
{
  "id": 1,
  "name": "smoking_status",
  "label": "Smoking Status",
  "fieldType": "Dropdown",
  "lookupTableId": 5,
  "hasLookupTable": true,
  "lookupValues": [
    {
      "id": 10,
      "value": "current",
      "displayText": "Current Smoker"
    },
    {
      "id": 11,
      "value": "former",
      "displayText": "Former Smoker"
    },
    {
      "id": 12,
      "value": "never",
      "displayText": "Never Smoked"
    },
    {
      "id": 13,
      "value": "unknown",
      "displayText": "Unknown"
    }
  ]
}
```

**Response (without lookup table):**
```json
{
  "id": 2,
  "name": "age",
  "label": "Age",
  "fieldType": "Number",
  "lookupTableId": null,
  "hasLookupTable": false,
  "lookupValues": null
}
```

**Status Codes:**
- `200 OK`: Success
- `401 Unauthorized`: Not authenticated
- `404 Not Found`: Custom field not found or inactive
- `500 Internal Server Error`: Server error

---

### 3. Get Available Fields (Legacy)

**Endpoint:** `GET /api/CustomFields/GetAvailableFields`

**Description:** Returns custom fields associated with active diseases. This is a legacy endpoint that may be deprecated.

**Authorization:** Required (Bearer token)

**Response:**
```json
[
  {
    "name": "smoking_status",
    "description": "Smoking Status",
    "diseaseName": "COVID-19",
    "fieldType": "Dropdown"
  },
  {
    "name": "contact_with_case",
    "description": "Contact with Confirmed Case",
    "diseaseName": "COVID-19",
    "fieldType": "Boolean"
  }
]
```

**Status Codes:**
- `200 OK`: Success
- `401 Unauthorized`: Not authenticated
- `500 Internal Server Error`: Server error

---

## Rate Limiting

All endpoints in the CustomFieldsController are rate-limited:
- **Policy:** `lookup-api`
- **Limit:** 200 requests per minute per user

**Response when rate limit exceeded:**
```json
{
  "error": "Too many requests",
  "retryAfter": 60
}
```

---

## Example Usage

### JavaScript (Fetch API)

#### Get all custom fields:
```javascript
async function loadCustomFields() {
  try {
    const response = await fetch('/api/CustomFields/ForCaseDefinitions', {
      headers: {
        'Authorization': 'Bearer ' + token
      }
    });

    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }

    const customFields = await response.json();
    console.log(customFields);
    return customFields;
  } catch (error) {
    console.error('Error loading custom fields:', error);
    return [];
  }
}
```

#### Get specific custom field with lookup values:
```javascript
async function getCustomFieldDetails(customFieldId) {
  try {
    const response = await fetch(`/api/CustomFields/${customFieldId}`, {
      headers: {
        'Authorization': 'Bearer ' + token
      }
    });

    if (!response.ok) {
      if (response.status === 404) {
        throw new Error('Custom field not found');
      }
      throw new Error(`HTTP error! status: ${response.status}`);
    }

    const customField = await response.json();

    // Check if it has lookup values
    if (customField.hasLookupTable && customField.lookupValues) {
      console.log('Lookup values:', customField.lookupValues);
    }

    return customField;
  } catch (error) {
    console.error('Error loading custom field:', error);
    return null;
  }
}
```

---

### C# (HttpClient)

#### Get all custom fields:
```csharp
public async Task<List<CustomFieldDto>> GetCustomFieldsAsync()
{
    using var httpClient = _httpClientFactory.CreateClient();
    httpClient.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", token);

    var response = await httpClient.GetAsync("/api/CustomFields/ForCaseDefinitions");

    if (response.IsSuccessStatusCode)
    {
        var customFields = await response.Content.ReadFromJsonAsync<List<CustomFieldDto>>();
        return customFields ?? new List<CustomFieldDto>();
    }

    throw new HttpRequestException($"Error: {response.StatusCode}");
}
```

#### Get specific custom field:
```csharp
public async Task<CustomFieldDetailDto?> GetCustomFieldDetailsAsync(int customFieldId)
{
    using var httpClient = _httpClientFactory.CreateClient();
    httpClient.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", token);

    var response = await httpClient.GetAsync($"/api/CustomFields/{customFieldId}");

    if (response.IsSuccessStatusCode)
    {
        return await response.Content.ReadFromJsonAsync<CustomFieldDetailDto>();
    }
    else if (response.StatusCode == HttpStatusCode.NotFound)
    {
        return null;
    }

    throw new HttpRequestException($"Error: {response.StatusCode}");
}
```

---

## DTOs (Data Transfer Objects)

### CustomFieldDto
```csharp
public class CustomFieldDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string FieldType { get; set; } = string.Empty;
    public bool HasLookupTable { get; set; }
}
```

### CustomFieldDetailDto
```csharp
public class CustomFieldDetailDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string FieldType { get; set; } = string.Empty;
    public int? LookupTableId { get; set; }
    public bool HasLookupTable { get; set; }
    public List<LookupValueDto>? LookupValues { get; set; }
}
```

### LookupValueDto
```csharp
public class LookupValueDto
{
    public int Id { get; set; }
    public string Value { get; set; } = string.Empty;
    public string DisplayText { get; set; } = string.Empty;
}
```

---

## Testing with cURL

### Get all custom fields:
```bash
curl -X GET "https://localhost:5001/api/CustomFields/ForCaseDefinitions" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -H "Content-Type: application/json"
```

### Get specific custom field:
```bash
curl -X GET "https://localhost:5001/api/CustomFields/1" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -H "Content-Type: application/json"
```

---

## Testing with Postman

### Setup:
1. Create a new request
2. Set method to GET
3. Enter URL: `{{baseUrl}}/api/CustomFields/ForCaseDefinitions`
4. Add header: `Authorization: Bearer {{token}}`
5. Send request

### Environment Variables:
```json
{
  "baseUrl": "https://localhost:5001",
  "token": "your-jwt-token-here"
}
```

---

## Error Responses

### 401 Unauthorized:
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.2",
  "title": "Unauthorized",
  "status": 401
}
```

### 404 Not Found:
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Not Found",
  "status": 404,
  "detail": "Custom field with ID 999 not found"
}
```

### 500 Internal Server Error:
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.6.1",
  "title": "An error occurred while processing your request.",
  "status": 500,
  "traceId": "00-ed3886305347ce0082756acfdb787a52-60c71d22ab59b4cd-00"
}
```

---

## Performance Considerations

### Response Times (typical):
- `ForCaseDefinitions`: 50-150ms
- `GetCustomField (no lookup)`: 20-50ms
- `GetCustomField (with lookup)`: 50-200ms

### Caching Recommendations:
- Client-side: Cache for 5-10 minutes
- Server-side: No caching (data changes infrequently)

### Database Queries:
- All endpoints use eager loading (`Include`/`ThenInclude`)
- Filtered by `IsActive` status
- Indexed on `Id`, `IsActive`, `ShowOnCaseForm`

---

## Security Notes

1. **Authentication Required**: All endpoints require a valid JWT token
2. **Authorization**: User must have appropriate role/permissions
3. **Rate Limiting**: 200 requests per minute per user
4. **SQL Injection Protection**: Entity Framework parameterized queries
5. **XSS Protection**: JSON responses are automatically encoded
6. **No PII**: Responses don't contain personally identifiable information

---

**Version:** 1.0  
**Last Updated:** 2026-04-27  
**API Base URL:** `/api/CustomFields`
