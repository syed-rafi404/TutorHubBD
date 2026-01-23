# TutorHubBD Dockerfile
# Multi-stage build for optimized production image

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY ["WebApplication1/TutorHubBD.Web.csproj", "WebApplication1/"]
RUN dotnet restore "WebApplication1/TutorHubBD.Web.csproj"

# Copy all source files
COPY . .

# Build the application
WORKDIR "/src/WebApplication1"
RUN dotnet build "TutorHubBD.Web.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "TutorHubBD.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Create non-root user for security
RUN adduser --disabled-password --gecos '' appuser

# Copy published files
COPY --from=publish /app/publish .

# Create uploads directory with proper permissions
RUN mkdir -p /app/wwwroot/uploads/profiles \
    && mkdir -p /app/wwwroot/uploads/docs \
    && mkdir -p /app/wwwroot/uploads/tutor-profiles \
    && chown -R appuser:appuser /app/wwwroot/uploads

# Switch to non-root user
USER appuser

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Start the application
ENTRYPOINT ["dotnet", "TutorHubBD.Web.dll"]
