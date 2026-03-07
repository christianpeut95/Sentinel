# Demo Environment Setup

This branch contains demo-specific configuration for showcasing Sentinel with pre-configured test accounts.

## Demo Users

The following demo accounts are automatically created when running in Demo mode:

### 1. Surveillance Manager
- **Email:** manager@sentinel-demo.com
- **Password:** Demo123!@#Manager
- **User:** Sarah Chen
- **Permissions:** Full system access - all modules including Settings

### 2. Surveillance Officer
- **Email:** officer@sentinel-demo.com
- **Password:** Demo123!@#Officer
- **User:** Michael Thompson
- **Permissions:** Full operational access (all modules except Settings)

### 3. Contact Tracer
- **Email:** tracer@sentinel-demo.com
- **Password:** Demo123!@#Tracer
- **User:** Emma Rodriguez
- **Permissions:** Interview queue, task completion, survey completion only

### 4. Contact Tracing Supervisor
- **Email:** supervisor@sentinel-demo.com
- **Password:** Demo123!@#Supervisor
- **User:** James Wilson
- **Permissions:** Contact Tracer + task reassignment + case/patient/contact management

### 5. STI/BBV Surveillance Officer
- **Email:** stiofficer@sentinel-demo.com
- **Password:** Demo123!@#STI
- **User:** Lisa Patel
- **Permissions:** Same as Surveillance Officer (will have STI/BBV disease-specific access)

## Running Demo Mode

### Local Development
```powershell
# Set environment to Demo
$env:ASPNETCORE_ENVIRONMENT="Demo"
dotnet run
```

### Azure Deployment
Set the following in Azure App Service Configuration:
- **ASPNETCORE_ENVIRONMENT:** `Demo`
- **Demo__EnableDemoUsers:** `true`

## Features

- ? Demo banner displayed at top of login page
- ? Test credentials shown on login page
- ? 5 pre-configured users with different permission levels
- ? Roles automatically created on startup
- ? Uses separate demo database (aspnet-Sentinel-DEMO)

## Security

**?? IMPORTANT:** Never use demo accounts in production!
- Demo users are only created when `ASPNETCORE_ENVIRONMENT=Demo`
- Uses separate database from production
- Demo banner clearly identifies environment

## Deployment to Azure

1. Create a new Azure App Service for demo
2. Deploy from the `demo` branch
3. Set environment variable: `ASPNETCORE_ENVIRONMENT=Demo`
4. Configure connection string in Azure
5. Demo users will be automatically created on first run

## Keeping Demo Updated

```powershell
# Switch to demo branch
git checkout demo

# Merge latest changes from master
git merge master

# Push to GitHub
git push origin demo
```

**Remember:** Changes made in the demo branch should NEVER be merged back to master!
