# Advanced Patient Search Feature

## Overview
The Advanced Patient Search feature provides a comprehensive way to search and filter patients using multiple criteria. The interface is designed to be compact with essential search fields always visible and additional filters available in an expandable section.

## How to Access

### Option 1: From the Sidebar
- Click **"Search Patients"** in the main navigation sidebar (left side of the screen)

### Option 2: From the Patients List
1. Navigate to **Patients** ? **Index**
2. Click the **"Advanced Search"** button at the top right

## Search Interface Layout

### Always Visible Fields (Essential Search)
The following fields are always visible for quick searches:
- **First Name**: Partial match search
- **Last Name**: Partial match search
- **Phone**: Searches both home and mobile phone fields
- **Email**: Partial match search
- **DOB From/To**: Date of birth range search
- **Suburb**: Location search
- **Postcode**: Postcode search

### Expandable Additional Filters
Click the **"More Filters"** button to reveal additional search criteria:
- **Sex at Birth**: Dropdown filter
- **Gender**: Dropdown filter
- **State**: Location filter
- **Country of Birth**: Dropdown filter
- **Language Spoken at Home**: Dropdown filter
- **Ethnicity**: Dropdown filter
- **ATSI Status**: Aboriginal and Torres Strait Islander Status dropdown filter
- **Occupation**: ANZSCO occupation dropdown filter (6-digit codes only)
- **Created From/To**: Filter by patient record creation date range

## How to Use

### Performing a Quick Search
1. Enter your search criteria in the always-visible fields
2. Click the **"Search"** button
3. Results will appear below the search form

### Using Additional Filters
1. Click **"More Filters"** to expand the additional criteria section
2. Select or enter your additional filter criteria
3. Click the **"Search"** button
4. The expanded section remains open to show which additional filters are active

### Search Features
- **Partial Matching**: Text fields support partial matches (e.g., "John" will find "John", "Johnny", "Johnson")
- **Case Insensitive**: All text searches are case-insensitive
- **Multiple Criteria**: Combine multiple filters to narrow down results
- **Empty Search**: Click "Search" without any criteria to see all patients

### Clearing Search
- Click the **"Clear"** button to reset all search criteria and collapse the additional filters section

## Search Results

### Results Display
The search results show:
- Patient name (with link to details page)
- Date of birth and calculated age
- Sex at birth
- Primary phone number (mobile preferred, then home)
- Email address
- Suburb and state
- Record creation date
- Quick action buttons (View and Edit)

### Result Count
- A badge displays the total number of patients found
- If more than 50 results are found, a warning suggests refining your search criteria

### No Results
If no patients match your criteria, an informative message is displayed with suggestions to adjust your filters.

## Navigation

### From Search Results
- **View Button** (eye icon): Opens the patient's detail page
- **Edit Button** (pencil icon): Opens the patient's edit page
- Click patient name: Also navigates to details page

### Breadcrumb Navigation
- Home ? Patients ? Advanced Search
- Click any breadcrumb to navigate back

## Tips for Effective Searching

### Quick Searches
- **By Name**: Use the always-visible first/last name fields
- **By Contact**: Use phone or email fields for quick contact lookups
- **By Location**: Enter suburb or postcode only
- **By Date Range**: Use DOB or Created date ranges

### Narrow Searches
- Combine multiple criteria (e.g., name + suburb + date of birth range)
- Use date ranges to find patients within specific age groups
- Use the "More Filters" section for demographic-specific searches

### Date Range Searches
- **DOB From/To**: Find patients born within a specific date range
- **Created From/To**: Find patients registered during a specific period
- Use both date ranges together for complex time-based queries

### Phone Number Searches
- Enter any part of a phone number
- Automatically searches both home and mobile phone fields
- No need to enter full numbers

### Created Date Searches
- **Created From**: Find patients registered on or after this date
- **Created To**: Find patients registered on or before this date (includes the entire day)
- Useful for tracking new patient registrations over time

## Technical Details

### Performance
- Results are ordered by last name, then first name
- Includes eager loading of related entities to prevent N+1 queries
- Large result sets (>50) trigger a performance warning
- Compact layout reduces page size and improves load times

### Data Loading
- All dropdown lists are populated on page load
- Only active occupations with 6-digit ANZSCO codes are shown
- Search executes only when criteria are provided (no automatic search on page load)
- Additional filters section uses Bootstrap collapse for clean UX

### Security
- Inherits authentication from Razor Pages
- Follows the same security model as other patient pages
- Requires user to be logged in

## Related Pages

- **Patient Index**: `/Patients/Index` - Full list of all patients
- **Patient Details**: `/Patients/Details/{id}` - View individual patient
- **Patient Edit**: `/Patients/Edit/{id}` - Edit patient information
- **Patient Create**: `/Patients/Create` - Add new patient

## Future Enhancements

Potential improvements:
- Add pagination for large result sets
- Export search results to Excel/CSV
- Save frequently used searches as templates
- Add age range search (e.g., "18-25 years old")
- Add address autocomplete with geocoding
- Advanced sorting options in results table
- Fuzzy name matching for better results
- Search history tracking
- Keyboard shortcuts for quick search
- Remember last expanded state of "More Filters"
