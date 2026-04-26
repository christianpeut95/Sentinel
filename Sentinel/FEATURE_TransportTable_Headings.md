# Transport Form Table Headings

## Overview
Added organized table-style headings to the transport details form for better visual clarity and user experience.

## Changes Made

### Transport Details Form Layout (`entity-quick-add.js`)

**Before:** Individual stacked fields with labels
- Departed From
- Departed At  
- Arrived At

**After:** Table-style grid layout with column headings

```
┌─────────────────────────────────────────┐
│ ⏱️ DEPARTURE    │    🏁 ARRIVAL         │
├─────────────────────────────────────────┤
│ [Time input]    │    [Time input]       │
│ (e.g., 9:00 AM) │    (e.g., 10:00 AM)   │
├─────────────────────────────────────────┤
│ [From input]    │    [To input]         │
│ (Central Stn)   │    (Airport T2)       │
└─────────────────────────────────────────┘
```

## Form Structure

### Header Section
- Transport type (e.g., "Bus", "Train", "Flight")
- Context label ("Complete transport details" or "Edit transport details")

### Details Field
- Route number, flight number, vehicle ID, etc.
- Single full-width field above the table

### Travel Times Table
**Visual Table with Borders:**
- **Column Headers:**
  - ⏱️ Departure (left column)
  - 🏁 Arrival (right column)

- **Time Row:**
  - Departed At time (e.g., "9:00 AM")
  - Arrived At time (e.g., "10:00 AM")

- **Location Row:**
  - Departed From location (e.g., "Central Station")
  - Arrived To location (e.g., "Airport Terminal 2")

## Metadata Captured

```javascript
{
    transportType: 'bus',      // bus, train, car, taxi, flight, walk
    details: 'Route 123',       // Optional route/flight number
    departedFrom: 'Central Station',    // Departure location
    departedAt: '9:00 AM',              // Departure time
    arrivedAt: '10:00 AM',              // Arrival time
    arrivedTo: 'Airport Terminal 2'     // Arrival location
}
```

## Benefits

1. **Visual Hierarchy:** Clear separation between departure and arrival information
2. **Reduced Cognitive Load:** Users immediately understand the left/right relationship
3. **Compact Layout:** Table format uses space efficiently
4. **Professional Appearance:** Matches contact tracing workflow expectations
5. **Keyboard Friendly:** Tab navigation flows naturally left-to-right, top-to-bottom

## Field Placeholders

- **Departed At:** "Time (e.g., 9:00 AM)"
- **Arrived At:** "Time (e.g., 10:00 AM)"
- **Departed From:** "From (e.g., Central Station)"
- **Arrived To:** "To (e.g., Airport Terminal 2)"

## Styling

- Grid layout with borders (2 columns)
- Header row with icons and uppercase labels
- Input fields fill each cell
- Consistent slate/graphite color scheme
- Border-right separates departure/arrival columns

## Future Enhancements

Consider adding:
- Duration calculation (arrival time - departure time)
- Location autocomplete with Google Places/address lookup
- Time picker widgets for easier time entry
- Validation (arrival time must be after departure time)
- Recent transport routes from EntityMemoryService

## Related Files

- `wwwroot/js/timeline/entity-quick-add.js` (lines 983-1070)
- Transport metadata stored in timeline entities
- Rendered in timeline entry sidebar/display
