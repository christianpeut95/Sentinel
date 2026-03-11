# Collection Query Feature - Testing Guide

## ?? Quick Test Steps

### **Test 1: Display Earliest Lab Collection Date**

1. Navigate to **Reports ? Report Builder**
2. Set Entity Type: **Case**
3. Add Fields:
   - Case Number
   - Patient Name
   - Disease
4. Click **"Add Query"** under Related Data Queries
5. Configure Collection Query:
   - Collection: **Lab Results**
   - Operation: **Minimum**
   - **Aggregate Field:** Select **Specimen Collection Date**
   - Check ? **"Display as Column"**
   - Column Name: **"Earliest Collection"**
6. Click **Preview**

**Expected Result:**
- Each case shows a column with its earliest specimen collection date
- Cases without lab results show null/empty

---

### **Test 2: Display Latest Task Due Date**

1. Add Collection Query
2. Configure:
   - Collection: **Tasks** (change to CaseTasks)
   - Operation: **Maximum**
   - **Aggregate Field:** Select **Due Date**
   - Check ? **"Display as Column"**
   - Column Name: **"Latest Due"**
3. Click **Preview**

**Expected Result:**
- Each case shows its latest task due date
- Useful for workload planning

---

### **Test 3: Filter Cases by Lab Result Sum**

1. Add Collection Query
2. Configure:
   - Collection: **Lab Results**
   - Operation: **Sum**
   - **Aggregate Field:** Select **Quantitative Result**
   - Uncheck ? **"Display as Column"** (use as filter)
   - Compare: **Greater Than**
   - Value: **1000**
3. Click **Preview**

**Expected Result:**
- Only shows cases where total quantitative lab results > 1000
- Useful for identifying high viral load cases

---

### **Test 4: Average Lab Value (with Sub-Filter)**

1. Add Collection Query
2. Configure:
   - Collection: **Lab Results**
   - Operation: **Average**
   - **Aggregate Field:** Select **Quantitative Result**
   - Click **"Add Sub-Filter"**:
     - Field: **Test Type**
     - Operator: **Equals**
     - Value: **PCR** (or whatever test type you have)
   - Check ? **"Display as Column"**
   - Column Name: **"Avg PCR Value"**
3. Click **Preview**

**Expected Result:**
- Shows average quantitative result for PCR tests only
- Ignores other test types

---

### **Test 5: Earliest/Latest Exposure Dates**

1. Add TWO Collection Queries:

**Query 1:**
   - Collection: **Exposures** (ExposureEvents)
   - Operation: **Minimum**
   - **Aggregate Field:** **Exposure Start Date**
   - Display as Column: **"First Exposure"**

**Query 2:**
   - Collection: **Exposures**
   - Operation: **Maximum**
   - **Aggregate Field:** **Exposure End Date**
   - Display as Column: **"Last Exposure"**

3. Click **Preview**

**Expected Result:**
- Two columns showing exposure date range for each case
- Useful for incubation period analysis

---

## ?? What to Check in Browser Console

### **Successful Load:**
```
? Loaded metadata for LabResults: {aggregatableFields: {...}}
updateCollectionOperator for query 1, operation: "Min"
updateAggregateFieldOptions for query 1, operation: Min
Aggregatable fields: {SpecimenCollectionDate: {...}, ResultDate: {...}}
? Added field option: SpecimenCollectionDate - Specimen Collection Date
? Added field option: ResultDate - Result Date
?? SUCCESS! Showing aggregate field container with options!
```

### **Common Issues:**

**Issue 1: "Aggregate field elements not found"**
- **Cause:** DOM not ready yet
- **Fix:** Should auto-retry after 100ms
- **Status:** Should be resolved

**Issue 2: "No metadata or aggregatable fields available"**
- **Cause:** Wrong property name (PascalCase vs camelCase)
- **Fix:** Now using camelCase (aggregatableFields)
- **Status:** ? Fixed in latest commit

**Issue 3: "Unexpected token '<'"**
- **Cause:** Old bug from incorrect API route
- **Should:** Not affect functionality
- **Status:** Ignore for now

---

## ? Success Criteria

When everything works, you should be able to:

1. ? Select a collection (Lab Results, Tasks, Exposures)
2. ? Select Min/Max/Sum/Average operation
3. ? **See the "Aggregate Field" dropdown appear**
4. ? See field options in the dropdown
5. ? Select a field (e.g., "Specimen Collection Date")
6. ? Display as column or use as filter
7. ? Click Preview and see results
8. ? Save the report

---

## ?? Test Data Requirements

For meaningful tests, ensure you have:

- ? **Cases with Lab Results** (with specimen collection dates)
- ? **Cases with Tasks** (with due dates)
- ? **Cases with Exposures** (with exposure dates)
- ? **Some lab results with quantitative values**

Use the **Test Data Generator** if needed to create sample data.

---

## ?? Troubleshooting

### **Dropdown Still Not Showing?**

1. **Check Console Logs** (F12)
   - Look for "? Loaded metadata"
   - Look for "?? SUCCESS!"

2. **Hard Refresh** (Ctrl+F5)
   - Clears cached JavaScript

3. **Inspect Element** (F12 ? Elements)
   - Look for: `<div id="aggregate-field-container-1">`
   - Check if `style="display:none;"` or `display:block;`

4. **Check Network Tab** (F12 ? Network)
   - Look for: `/api/reports/collection-metadata/Case`
   - Status should be **200 OK**
   - Response should be JSON (not HTML)

### **API Returning HTML Instead of JSON?**

If you see `<!DOCTYPE` error:
- Check if you're logged in
- Verify `[Authorize]` on controller isn't redirecting
- Current fix: Endpoint uses controller-level auth only

---

## ?? Expected Visual Layout

When "Minimum" is selected, you should see:

```
Related Data Query #1
?? ? Display as Column
?  ?? Column Name: [________________]
?? Collection: [Lab Results ?]
?? Operation: [Minimum ?]
?? **Aggregate Field: [Specimen Collection Date ?]** ?? THIS IS NEW!
?? Sub-Filters: [Add Sub-Filter]
```

---

## ?? Tips

1. **Start simple:** Test with just "Count" first (doesn't need aggregate field)
2. **Then try Min/Max:** Should trigger aggregate field dropdown
3. **Check console:** Logs will tell you what's happening
4. **Use Display as Column:** Easier to see results than filters

---

## ?? Support

If dropdown still doesn't appear after:
- ? Restarting app
- ? Hard refresh (Ctrl+F5)
- ? Checking console logs

Please share:
1. Full console log (F12 ? Console ? copy all)
2. Network request to `/api/reports/collection-metadata/Case`
3. Screenshot of the UI

---

**Last Updated:** March 11, 2026 - Commit `e629dcc`
