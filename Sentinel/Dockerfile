# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY ["Sentinel.csproj", "./"]
RUN dotnet restore "Sentinel.csproj"

# Copy everything else and publish directly
COPY . .
RUN dotnet publish "Sentinel.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Install necessary packages for spatial data (NetTopologySuite)
RUN apt-get update && apt-get install -y \
    libgdiplus \
    libc6-dev \
    && rm -rf /var/lib/apt/lists/*

# Copy published app
COPY --from=build /app/publish .

# Expose ports
EXPOSE 8080
EXPOSE 8081

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:8080/ || exit 1

ENTRYPOINT ["dotnet", "Sentinel.dll"]

