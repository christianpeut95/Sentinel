# Docker Deployment Instructions - SurveyJS Expressions Fix

## Changes to Deploy

### Files Modified (Master Branch)
1. `Controllers/Api/SurveyCompletionApiController.cs` - NEW API endpoint
2. `Pages/Tasks/CompleteSurvey.cshtml` - Updated to use API + expression fixes
3. `Pages/Tasks/CompleteSurvey.cshtml.cs` - Added IgnoreAntiforgeryToken
4. `Pages/Shared/_Layout.cshtml` - Enhanced SurveyJS custom functions

### Key Fixes
- ? SurveyJS expressions now evaluate correctly (today(), currentDate(), age())
- ? Survey submission works via new API endpoint
- ? Fixed JSON parse error
- ? Render survey before setting data (allows defaultValueExpression to run)

## Deployment Steps

### Step 1: Commit and Push Changes to Master
```powershell
git add .
git commit -m "Fix: SurveyJS expressions and API endpoint for survey completion"
git push origin master
```

### Step 2: Merge Changes to Demo Branch
```powershell
git checkout demo
git merge master -m "Merge survey expression fixes from master"
git push origin demo
git checkout master
```

### Step 3: Build and Publish to Docker Hub
```powershell
.\docker-publish.ps1 -DockerHubUsername "christianpeut"
```

This will:
- Build `christianpeut/sentinel:latest` from master branch
- Build `christianpeut/sentinel:main` (alias for latest)
- Build `christianpeut/sentinel:demo` from demo branch
- Push all three to Docker Hub

### Step 4: Verify Deployment
After pushing, the images will be available at:
- Main: `https://hub.docker.com/r/christianpeut/sentinel`
- Pull: `docker pull christianpeut/sentinel:latest`
- Demo: `docker pull christianpeut/sentinel:demo`

## Running the Updated Container

### Using Docker Compose (Recommended)
```powershell
# Update docker-compose.yml to use christianpeut/sentinel:latest
# Then restart the stack
docker-compose down
docker-compose pull
docker-compose up -d
```

### Standalone Container
```powershell
# Stop old container
docker stop sentinel-app
docker rm sentinel-app

# Pull and run new version
docker pull christianpeut/sentinel:latest
docker run -d -p 8080:8080 `
  -e ConnectionStrings__DefaultConnection="Your-Connection-String" `
  -e Geocoding__ApiKey="Your-API-Key" `
  --name sentinel-app `
  christianpeut/sentinel:latest
```

## Testing After Deployment

1. Open a survey with `defaultValueExpression: "today()"`
2. Verify the date field shows today's date automatically
3. Test survey submission - should save successfully
4. Check browser console - no JSON parse errors
5. Verify expressions: `age()`, `currentDate()`, `addDays()`, etc.

## Rollback (If Needed)

If something goes wrong:
```powershell
# Pull previous version (use a specific tag if available)
docker pull christianpeut/sentinel:previous-tag

# Or rebuild from a specific commit
git checkout <previous-commit-hash>
docker build -t christianpeut/sentinel:rollback -f ./Dockerfile ./Sentinel
docker push christianpeut/sentinel:rollback
```

## Build Time Estimate
- Each build: ~5-10 minutes
- Total for both main + demo: ~15-20 minutes

## Post-Deployment Verification

### Check Container Logs
```powershell
docker logs sentinel-app -f
```

Look for:
- "Registering SurveyJS custom functions..."
- "SurveyJS custom functions registered successfully"
- No errors about missing routes or controllers

### Test Survey Functionality
1. Navigate to a task with a survey
2. Open browser DevTools Console
3. Should see:
   - "Registering SurveyJS custom functions..."
   - "SurveyJS FunctionFactory is available"
   - List of registered functions including 'today', 'currentDate', 'age'
4. Date fields with `defaultValueExpression: "today()"` should auto-fill
5. Submit survey - should see success message
