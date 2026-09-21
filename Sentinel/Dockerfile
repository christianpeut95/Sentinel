# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# The Docker build context is the repository root. Keep the project folder in
# place so the published image can also carry the repository-level licence and
# third-party notices.
COPY ["Sentinel/Sentinel.csproj", "Sentinel/"]
COPY ["Sentinel.HL7Generator/Sentinel.HL7Generator.csproj", "Sentinel.HL7Generator/"]
RUN dotnet restore "Sentinel/Sentinel.csproj"

# Copy everything else and publish directly
COPY . .
RUN dotnet publish "Sentinel/Sentinel.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Install necessary packages for spatial data (NetTopologySuite)
RUN apt-get update && apt-get install -y \
    curl \
    libgdiplus \
    libc6-dev \
    && rm -rf /var/lib/apt/lists/*

# Copy published app
COPY --from=build /app/publish .

# A container image is a distribution of Sentinel. Include the GPL and the
# third-party notice inventory alongside the published application.
COPY LICENSE.md THIRD_PARTY_NOTICES.md ./

# The reverse proxy is the only public service. Kestrel listens on this
# private-network port for Caddy.
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:8080/ || exit 1

ENTRYPOINT ["dotnet", "Sentinel.dll"]

