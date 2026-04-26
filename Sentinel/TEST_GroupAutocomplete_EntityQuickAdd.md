# Quick Test: Entity Group Autocomplete

**Feature**: Groups now appear in Entity Quick-Add dropdown (`..` trigger)  
**Status**: ✅ Complete - Ready for testing  
**Hot Reload**: Available

---

## 🧪 Quick Test (2 minutes)

### Setup
1. Open timeline entry page
2. Open browser console (F12)

### Step 1: Create a Group

**Type**:
```
#Siblings( John Cathy)
```

**Expected**:
- Console shows: `[TimelineEntry] Created group "Siblings" with 2 entities`
- Text changes to: `+John +Cathy`
- Group appears in right sidebar under "Entity Groups"

---

### Step 2: Use Group Autocomplete

**On a new line, type**:
```
..
```

**Expected Dropdown**:
```
👥 Siblings (2 entities)
👤 John
👤 Cathy
─── or choose type ───
👤 Person
📍 Location
...
```

✅ **Verify**: Group appears **first** with 👥 icon and entity count

---

### Step 3: Select Group

**Use arrow keys** to highlight "👥 Siblings (2 entities)"  
**Press Enter**

**Expected**:
1. Text shows: `+#Siblings`
2. After ~100ms, changes to: `+John +Cathy`
3. Both entities highlighted

**Console Output**:
```
[EntityQuickAdd] Inserted group reference: +#Siblings
[TimelineEntry] Expanded +#Siblings to: +John +Cathy
```

✅ **Verify**: Auto-expansion works

---

### Step 4: Group in Relationship

**Type**:
```
.. went to @Hospital
```

**Select**: `👥 Siblings` from dropdown

**Final Text**:
```
+John +Cathy went to @Hospital
```

**Expected Relationships** (check sidebar):
- John AT_LOCATION Hospital
- Cathy AT_LOCATION Hospital

✅ **Verify**: Relationships created for both group members

---

## 🔍 Test: Group Filtering

### Step 1: Create Multiple Groups

**Type**:
```
#Alpha( Alice Ann)
#Beta( Bob Bill)
#Gamma( Gary Gina)
```

Press space after each to create groups

---

### Step 2: Filter by Name

**Type**:
```
..al
```

**Expected Dropdown**:
```
👥 Alpha (2 entities)
─── or choose type ───
👤 Person
...
```

✅ **Verify**: Only "Alpha" shown (case-insensitive filter)

---

### Step 3: Clear Filter

**Type**:
```
.. (just two dots, no text)
```

**Expected Dropdown**:
```
👥 Alpha (2 entities)
👥 Beta (2 entities)
👥 Gamma (2 entities)
👤 Alice
👤 Ann
👤 Bob
...
```

✅ **Verify**: All groups shown

---

## 🎨 Visual Test: Styling

### Check Group Item Appearance

**Type**: `..`

**Inspect Group Item**:
- Icon: 👥 (distinct from 👤 person, 📍 location)
- Name: **Bold text**
- Color: **Green** (var(--signal-dk))
- Count: **(2 entities)** in gray, right-aligned
- Hover: Background changes to light gray

✅ **Verify**: Visual styling matches design

---

## ❌ Failure Cases

### Test 1: No Groups Created Yet

**Setup**: Delete all groups or start fresh

**Type**: `..`

**Expected**:
```
👤 John (recent entities still shown)
📍 Hospital
─── or choose type ───
...
```

✅ **Verify**: No groups shown, no errors

---

### Test 2: Empty Group

**Type**:
```
#Empty()
```

**Then type**: `..`

**Expected**:
- Group "Empty" appears: `👥 Empty (0 entities)`
- Selectable but expands to nothing

⚠️ **Known limitation**: Empty groups should be filtered out (future enhancement)

---

### Test 3: Group Name with Spaces

**Type**:
```
#My Group( John Cathy)
```

**Expected**:
- ❌ Group NOT created (space not supported in name)
- Console shows: "No entities found in group definition"

**Workaround**: Use `#MyGroup` or `#My_Group`

---

## 🐛 Debugging

### Debug 1: Check Groups Loaded

```javascript
// In browser console:
const groups = await window.entityQuickAdd.loadEntityGroups();
console.table(groups);
```

**Expected**:
```
name     | entityCount | id
---------|-------------|---
Siblings | 2           | ...
Alpha    | 2           | ...
Beta     | 2           | ...
```

---

### Debug 2: Check TimelineEntry Groups

```javascript
// In browser console:
console.log(window.timelineEntry.entityGroups);
```

**Expected**:
```javascript
{
  "group_id_1": {
    id: "group_id_1",
    name: "Siblings",
    entityIds: ["entity_1", "entity_2"],
    caseId: "..."
  }
}
```

---

### Debug 3: Manual Expansion Test

```javascript
// In browser console:
const textarea = document.querySelector('.narrative-textarea');
textarea.value = '+#Siblings';
textarea.dispatchEvent(new Event('input', { bubbles: true }));

// Wait 1 second, then:
console.log(textarea.value);
// Expected: "+John +Cathy"
```

---

## ✅ Pass Criteria

**Feature works if**:

- [ ] Groups appear in `..` dropdown
- [ ] Groups show 👥 icon and entity count
- [ ] Group filtering by name works
- [ ] Selecting group inserts `+#GroupName`
- [ ] `+#GroupName` auto-expands to individual entities
- [ ] Relationships created for all group members
- [ ] No console errors
- [ ] Visual styling matches design

---

## 🆘 Troubleshooting

### Issue 1: Groups Don't Appear in Dropdown

**Check**:
1. Group created successfully? (check console for "Created group")
2. Group exists in sidebar?
3. Timeline reloaded groups? (refresh page)

**Fix**: Groups should load automatically. If not, check:
```javascript
console.log(window.timelineEntry.entityGroups);
// Should show groups object
```

---

### Issue 2: Group Selection Does Nothing

**Check Console**: Look for errors

**Possible causes**:
- Tribute.js not capturing selection
- `selectTemplate` not handling `isGroup`
- `setTimeout` not firing

**Fix**: Check console for `[EntityQuickAdd] Inserted group reference` log

---

### Issue 3: Group Doesn't Expand

**Check**:
1. `+#GroupName` inserted correctly?
2. Group name matches exactly (case-insensitive)?
3. `expandGroupReferences` method called?

**Debug**:
```javascript
// Force expansion:
const textarea = document.querySelector('.narrative-textarea');
window.timelineEntry.expandGroupReferences(textarea.value);
```

---

### Issue 4: Visual Styling Wrong

**Check**:
- CSS file loaded? (look for `.entity-quick-group` in DevTools)
- Hot reload worked? (Ctrl+Shift+R to hard refresh)

**Fix**: Hard refresh browser or restart debugging session

---

## 📊 Test Results Template

### Test Environment
- Browser: ____________
- Date: ____________

### Test 1: Basic Group Autocomplete
- [ ] Pass - Groups appear in dropdown
- [ ] Fail - Details: _______

### Test 2: Group Selection
- [ ] Pass - Inserts `+#GroupName` and expands
- [ ] Fail - Details: _______

### Test 3: Group Filtering
- [ ] Pass - Filter by name works
- [ ] Fail - Details: _______

### Test 4: Visual Styling
- [ ] Pass - 👥 icon, entity count, green color
- [ ] Fail - Details: _______

### Console Logs
```
Paste relevant logs here
```

### Screenshots
1. **Dropdown with groups**
2. **Group expanded to entities**

---

## 🚀 Next Steps After Testing

If all tests pass:
1. Test with larger groups (5+ entities)
2. Test with many groups (10+ groups)
3. Test filtering edge cases
4. Deploy to production

If tests fail:
1. Document failure details
2. Paste console logs
3. Include screenshots
4. Report to developer
