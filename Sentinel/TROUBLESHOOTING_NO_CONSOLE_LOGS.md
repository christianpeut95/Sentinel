# Troubleshooting: No Console Logs Appearing

## Steps to Debug

### 1. **Stop and Restart the Application**
   - **Stop debugging completely** (Shift+F5)
   - **Clean the solution** (Build → Clean Solution)
   - **Rebuild the solution** (Build → Rebuild Solution)
   - **Start debugging again** (F5)

Hot reload doesn't always pick up JavaScript changes reliably.

### 2. **Clear Browser Cache**
   After restarting the app:
   - Press **Ctrl+Shift+Delete** (or Cmd+Shift+Delete on Mac)
   - Select "Cached images and files"
   - Click "Clear data"
   - **OR** do a hard refresh: **Ctrl+F5** (Cmd+Shift+R on Mac)

### 3. **Check Console for Errors**
   Open the browser console (F12) and look for:
   
   **Expected logs (in order):**
   ```
   [Builder.cshtml] Script section executing...
   [report-builder.js] Loading...
   [report-builder-collections.js] Loading...
   [report-builder-actions.js] Loading...
   [Builder.cshtml] DOMContentLoaded event fired
   [Builder.cshtml] Saved report data: {reportId: null, entityType: 'Case', ...}
   [Builder.cshtml] About to call ReportBuilder.init()
   [ReportBuilder.init] Called with savedReport: {...}
   [ReportBuilder.init] Initialization complete
   [Builder.cshtml] ReportBuilder.init() called successfully
   ```

   **If you see errors like:**
   - `Uncaught ReferenceError: ReportBuilder is not defined` → JS files not loading
   - `Uncaught SyntaxError` → There's a syntax error in one of the JS files
   - `Failed to load resource: 404` → Check that JS files are in the correct location

### 4. **Verify JavaScript Files Are Loading**
   
   In the browser:
   1. Open DevTools (F12)
   2. Go to **Network tab**
   3. Reload the page (F5)
   4. Filter by "JS"
   5. Look for:
      - `report-builder.js` (should be 200 OK, ~40-50 KB)
      - `report-builder-collections.js` (should be 200 OK, ~15-20 KB)
      - `report-builder-actions.js` (should be 200 OK, ~20-30 KB)

   If any show **404** or **not found**, the files aren't being served.

### 5. **Check File Locations**
   
   Verify the files exist at:
   ```
   C:\Users\Christian\source\repos\Sentinel\Sentinel\wwwroot\js\report-builder.js
   C:\Users\Christian\source\repos\Sentinel\Sentinel\wwwroot\js\report-builder-collections.js
   C:\Users\Christian\source\repos\Sentinel\Sentinel\wwwroot\js\report-builder-actions.js
   ```

### 6. **Test Manual Execution**
   
   After the page loads, in the browser console, type:
   ```javascript
   ReportBuilder
   ```
   
   **Expected:** Should show the object with all its functions
   
   **If undefined:** The JS files aren't loading or there's a syntax error

### 7. **Check for Caching Issues**
   
   The browser might be caching old versions. Try:
   
   **Option A:** Disable cache while DevTools is open
   - Open DevTools (F12)
   - Go to Network tab
   - Check "Disable cache"
   - Reload page
   
   **Option B:** Use incognito/private browsing
   - Open a new incognito window (Ctrl+Shift+N)
   - Navigate to the page
   - Check console

### 8. **Add Collection Query Test**
   
   Once the page loads:
   1. Click "Add Query" button in Collection Queries section
   2. **Expected console output:**
      ```
      [addCollectionQuery] Called
      ```
   
   3. Select "Lab Results" from Collection dropdown
   4. Click "Add Sub-Filter"
   5. **Expected console output:**
      ```
      [addCollectionSubFilter] Fields: [...]
      [addCollectionSubFilter] Metadata: [...]
      [setupSubFilterSmartInput] Setting up smart input for query 1 subfilter ...
      ```
   
   6. Select "Result Date" from the field dropdown
   7. **Expected console output:**
      ```
      [SubFilter] Field changed to ResultDate DataType: DateTime Option dataset: {type: 'DateTime'}
      ```

## Common Issues & Solutions

### Issue: No logs appear at all
**Cause:** JavaScript files not loading or syntax error
**Solution:** 
1. Stop debugging
2. Clean solution
3. Rebuild solution
4. Hard refresh browser (Ctrl+F5)

### Issue: Logs appear but stop at a certain point
**Cause:** JavaScript error occurring at that point
**Solution:** Check the Console tab for error messages (usually shown in red)

### Issue: "ReportBuilder is not defined"
**Cause:** The report-builder.js file isn't loading before it's used
**Solution:** 
1. Check Network tab to see if the file loaded
2. Verify script tags order in Builder.cshtml
3. Make sure the file exists in wwwroot/js/

### Issue: Files load but functions don't work
**Cause:** Functions aren't being attached to ReportBuilder object correctly
**Solution:** 
1. Open report-builder-collections.js
2. Make sure it starts with `ReportBuilder.addCollectionQuery = function() {`
3. Make sure there are no syntax errors

## Quick Test Script

Paste this in the browser console after the page loads:

```javascript
// Test 1: Check if ReportBuilder exists
console.log('ReportBuilder exists:', typeof ReportBuilder !== 'undefined');

// Test 2: Check if key functions exist
console.log('addCollectionQuery exists:', typeof ReportBuilder.addCollectionQuery === 'function');
console.log('addCollectionSubFilter exists:', typeof ReportBuilder.addCollectionSubFilter === 'function');
console.log('setupSubFilterSmartInput exists:', typeof ReportBuilder.setupSubFilterSmartInput === 'function');

// Test 3: Check if metadata loading function exists
console.log('updateCollectionFields exists:', typeof ReportBuilder.updateCollectionFields === 'function');

// Test 4: Try to manually trigger debug
if (typeof ReportBuilder !== 'undefined') {
    console.log('Collection queries:', ReportBuilder.collectionQueries);
}
```

**Expected output:** All should show `true` or function name

## Next Steps

After you've completed these steps, please share:
1. What console logs you see (or don't see)
2. Any error messages in red
3. The result of the Quick Test Script
4. Network tab screenshot showing the JS files

This will help identify exactly where the issue is!
