# Quick Start: Configuring Organization Locale

## 5-Minute Setup Guide

### What This Does
Configures your organization's location so that address searches automatically prioritize your region. For example, if you're in Australia, searching for "Sydney" will show Sydney, Australia first instead of Sydney, Canada.

### Steps

#### 1. Navigate to Organization Settings
1. Log in as an **Admin** user
2. Go to **Settings** (gear icon in navigation)
3. Click **Organization Settings** under "System Configuration"

#### 2. Fill in Basic Information
- **Organization Name**: Your health district or company name (e.g., "Sydney Health District")
- **Country**: Your primary country (e.g., "Australia")

#### 3. Set Country Code (IMPORTANT!)
- **Country Code**: Two-letter code for your country
  - Australia: **AU**
  - United States: **US**
  - United Kingdom: **GB**
  - New Zealand: **NZ**
  - Canada: **CA**

#### 4. Optional: Add Additional Details
- **State/Province**: e.g., "New South Wales"
- **City/Region**: e.g., "Sydney"
- **Postal Code**: e.g., "2000"
- **Timezone**: e.g., "Australia/Sydney"

#### 5. Save
Click **"Save Organization Settings"**

### Test It Out

#### Before Configuration:
1. Go to **Patients > Create New Patient**
2. Start typing "Sydney" in the address field
3. You might see Sydney, Canada first

#### After Configuration (with CountryCode = AU):
1. Go to **Patients > Create New Patient**
2. Start typing "Sydney" in the address field
3. You'll see Sydney, Australia results prioritized

### Common Country Codes

| Country | Code |
|---------|------|
| Australia | AU |
| United States | US |
| United Kingdom | GB |
| New Zealand | NZ |
| Canada | CA |
| Ireland | IE |
| Singapore | SG |

### Example Configuration

**For Australian Health District:**
```
Organization Name: Sydney Local Health District
Country: Australia
State/Province: New South Wales
City/Region: Sydney
Postal Code: 2000
Country Code: AU
Timezone: Australia/Sydney
```

**For US Health System:**
```
Organization Name: Memorial Health System
Country: United States
State/Province: California
City/Region: Los Angeles
Postal Code: 90001
Country Code: US
Timezone: America/Los_Angeles
```

### Need Help?

- **Country codes must be exactly 2 uppercase letters**
- If it's not working, check that the Country Code is set
- Restart the application if you manually edited config files
- See full documentation: `Docs\Organization-Settings-Locale.md`

### What Gets Better?

? Address autocomplete suggestions match your region  
? Geocoding results prioritize your country  
? Less typing - users don't need to specify country every time  
? Fewer mistakes from selecting wrong city/state  
? Better data quality overall  

That's it! Your address lookups are now localized to your region.
