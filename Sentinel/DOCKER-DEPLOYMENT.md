# Sentinel Docker Deployment Guide

## Prerequisites
- Docker Desktop installed and running
- Docker Hub account created
- Git repository cloned

## Step-by-Step Deployment to Docker Hub

### 1. Login to Docker Hub
```powershell
docker login
```
Enter your Docker Hub username and password when prompted.

### 2. Build and Publish Using Script
The easiest way is to use the automated script:

```powershell
# Replace 'yourusername' with your Docker Hub username
.\docker-publish.ps1 -DockerHubUsername "yourusername"
```

This will:
- ? Build the **main** version from master branch ? tagged as `latest` and `main`
- ? Build the **demo** version from demo branch ? tagged as `demo`
- ? Push both to Docker Hub
- ? Return you to your original branch

### 3. Manual Build (Alternative)

If you prefer manual control:

#### Build Main Version (Master Branch)
```powershell
git checkout master
cd Sentinel
docker build -t yourusername/sentinel:latest -f ../Dockerfile .
docker tag yourusername/sentinel:latest yourusername/sentinel:main
docker push yourusername/sentinel:latest
docker push yourusername/sentinel:main
```

#### Build Demo Version (Demo Branch)
```powershell
git checkout demo
docker build -t yourusername/sentinel:demo -f ../Dockerfile .
docker push yourusername/sentinel:demo
git checkout master
```

## Docker Image Tags

After publishing, you'll have three tags available:

| Tag | Branch | Purpose | Docker Pull Command |
|-----|--------|---------|---------------------|
| `latest` | master | Production/Main version | `docker pull yourusername/sentinel:latest` |
| `main` | master | Production/Main version (alias) | `docker pull yourusername/sentinel:main` |
| `demo` | demo | Demo version with test data | `docker pull yourusername/sentinel:demo` |

## Running the Container

### Quick Start (Standalone)
```powershell
# Run main version
docker run -d -p 8080:8080 `
  -e ConnectionStrings__DefaultConnection="YourConnectionString" `
  -e Geocoding__ApiKey="YourGoogleApiKey" `
  --name sentinel-app `
  yourusername/sentinel:latest

# Run demo version
docker run -d -p 8080:8080 `
  -e ConnectionStrings__DefaultConnection="YourConnectionString" `
  -e Geocoding__ApiKey="YourGoogleApiKey" `
  --name sentinel-demo `
  yourusername/sentinel:demo
```

### Using Docker Compose (Recommended)

1. **Copy the environment file:**
```powershell
copy .env.example .env
```

2. **Edit `.env` file** with your actual values:
```env
DOCKERHUB_USERNAME=yourusername
VERSION_TAG=latest
CONNECTION_STRING=Server=sentinel-db;Database=SentinelDb;...
GEOCODING_API_KEY=your-actual-api-key
```

3. **Start the stack** (includes SQL Server):
```powershell
# Start main version
docker-compose up -d

# Or start demo version
$env:VERSION_TAG="demo"
docker-compose up -d
```

4. **View logs:**
```powershell
docker-compose logs -f sentinel-web
```

5. **Stop:**
```powershell
docker-compose down
```

## Environment Variables Required

| Variable | Description | Example |
|----------|-------------|---------|
| `ConnectionStrings__DefaultConnection` | SQL Server connection string | `Server=db;Database=Sentinel;...` |
| `Geocoding__Provider` | Geocoding provider (google/nominatim) | `google` |
| `Geocoding__ApiKey` | Google Maps API key | `AIza...` |
| `Geocoding__Email` | Contact email for Nominatim | `admin@example.com` |
| `Geocoding__DefaultCountry` | Default country code | `AU` |

## Updating Images

When you make changes:

### Update Main Version
```powershell
git checkout master
git pull
.\docker-publish.ps1 -DockerHubUsername "yourusername"
```

### Update Demo Version Only
```powershell
git checkout demo
git pull
cd Sentinel
docker build -t yourusername/sentinel:demo -f ../Dockerfile .
docker push yourusername/sentinel:demo
```

## Deployment to Production

### Azure App Service
1. Go to Azure Portal
2. Create **Web App for Containers**
3. In **Deployment Center**, select **Docker Hub**
4. Enter: `yourusername/sentinel:latest`
5. Configure environment variables in **Configuration** > **Application Settings**

### AWS ECS/EKS
Use the Docker Hub image: `yourusername/sentinel:latest`

### Other Cloud Providers
Most support Docker Hub images directly - just reference `yourusername/sentinel:latest`

## Troubleshooting

### Build Fails
```powershell
# Clean Docker cache
docker system prune -a

# Rebuild without cache
docker build --no-cache -t yourusername/sentinel:latest -f ./Dockerfile ./Sentinel
```

### Permission Denied
```powershell
# Re-login to Docker Hub
docker logout
docker login
```

### Check Image Locally
```powershell
# List images
docker images | findstr sentinel

# Run locally to test
docker run -p 8080:8080 yourusername/sentinel:latest
```

## Security Notes

?? **Important:**
- Never commit `.env` file (already in `.gitignore`)
- Never hardcode API keys or passwords in Dockerfile
- Use Docker secrets or environment variables for sensitive data
- The published images do NOT contain secrets (they're loaded at runtime)

## Multi-Architecture Build (Optional)

To build for both AMD64 and ARM64 (for Apple Silicon, etc.):

```powershell
# Create and use buildx builder
docker buildx create --use

# Build and push multi-arch image
docker buildx build --platform linux/amd64,linux/arm64 `
  -t yourusername/sentinel:latest `
  -f ./Dockerfile `
  --push `
  ./Sentinel
```

## Resources
- Docker Hub: https://hub.docker.com/r/yourusername/sentinel
- Docker Docs: https://docs.docker.com/
