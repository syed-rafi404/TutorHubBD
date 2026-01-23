# TutorHubBD - Production Deployment Guide

## ?? Table of Contents
1. [Prerequisites](#prerequisites)
2. [Deployment Options](#deployment-options)
3. [Azure App Service Deployment](#azure-app-service-deployment)
4. [Environment Variables](#environment-variables)
5. [Database Setup](#database-setup)
6. [Post-Deployment Checklist](#post-deployment-checklist)
7. [Monitoring & Maintenance](#monitoring--maintenance)

---

## Prerequisites

Before deploying, ensure you have:

- [ ] .NET 8 SDK installed
- [ ] Azure account (for Azure deployment) or other hosting provider
- [ ] SQL Server database (Azure SQL or other)
- [ ] Domain name (optional but recommended)
- [ ] SSL certificate (usually provided by hosting)

---

## Deployment Options

### Option 1: Azure App Service (Recommended)
- Easy deployment via Visual Studio or GitHub Actions
- Built-in SSL, scaling, and monitoring
- Azure SQL Database integration

### Option 2: IIS on Windows Server
- Traditional hosting
- Requires manual server configuration

### Option 3: Docker Container
- Containerized deployment
- Works with any container platform (Azure Container Apps, AWS ECS, etc.)

---

## Azure App Service Deployment

### Step 1: Create Azure Resources

```bash
# Login to Azure CLI
az login

# Create a resource group
az group create --name TutorHubBD-RG --location "Southeast Asia"

# Create an App Service Plan
az appservice plan create \
    --name TutorHubBD-Plan \
    --resource-group TutorHubBD-RG \
    --sku B1 \
    --is-linux false

# Create the Web App
az webapp create \
    --name tutorhubbd \
    --resource-group TutorHubBD-RG \
    --plan TutorHubBD-Plan \
    --runtime "dotnet:8"

# Create Azure SQL Database
az sql server create \
    --name tutorhubbd-sql \
    --resource-group TutorHubBD-RG \
    --location "Southeast Asia" \
    --admin-user sqladmin \
    --admin-password "YourStrongPassword123!"

az sql db create \
    --name TutorHubBD \
    --server tutorhubbd-sql \
    --resource-group TutorHubBD-RG \
    --service-objective S0
```

### Step 2: Configure App Settings in Azure Portal

Go to Azure Portal > App Service > Configuration > Application settings

Add the following settings:

| Name | Value |
|------|-------|
| `ConnectionStrings__DefaultConnection` | `Server=tcp:tutorhubbd-sql.database.windows.net,1433;Database=TutorHubBD;User ID=sqladmin;Password=YourPassword;Encrypt=True;` |
| `EmailSettings__MailServer` | `smtp.gmail.com` |
| `EmailSettings__MailPort` | `587` |
| `EmailSettings__SenderName` | `TutorHubBD Support` |
| `EmailSettings__SenderEmail` | `your-email@gmail.com` |
| `EmailSettings__SenderPassword` | `your-app-password` |
| `StripeSettings__SecretKey` | `sk_live_xxxxx` (use live key!) |
| `StripeSettings__PublishableKey` | `pk_live_xxxxx` (use live key!) |
| `GeminiApi__ApiKey` | `your-gemini-api-key` |
| `AdminSettings__Email` | `admin@yourdomain.com` |
| `AdminSettings__Password` | `SecureAdminPassword123!` |
| `ASPNETCORE_ENVIRONMENT` | `Production` |

### Step 3: Deploy via Visual Studio

1. Right-click the project in Solution Explorer
2. Select "Publish"
3. Choose "Azure" > "Azure App Service (Windows)"
4. Select your subscription and app service
5. Click "Publish"

### Step 4: Deploy via GitHub Actions

Create `.github/workflows/azure-deploy.yml`:

```yaml
name: Deploy to Azure

on:
  push:
    branches: [ main ]
  workflow_dispatch:

env:
  AZURE_WEBAPP_NAME: tutorhubbd
  DOTNET_VERSION: '8.0.x'

jobs:
  build-and-deploy:
    runs-on: windows-latest

    steps:
    - uses: actions/checkout@v4

    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}

    - name: Restore dependencies
      run: dotnet restore WebApplication1/TutorHubBD.Web.csproj

    - name: Build
      run: dotnet build WebApplication1/TutorHubBD.Web.csproj --configuration Release --no-restore

    - name: Publish
      run: dotnet publish WebApplication1/TutorHubBD.Web.csproj --configuration Release --no-build --output ./publish

    - name: Deploy to Azure
      uses: azure/webapps-deploy@v2
      with:
        app-name: ${{ env.AZURE_WEBAPP_NAME }}
        publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
        package: ./publish
```

---

## Environment Variables

### Required for Production

| Variable | Description | Example |
|----------|-------------|---------|
| `ConnectionStrings__DefaultConnection` | SQL Server connection string | See above |
| `EmailSettings__SenderEmail` | SMTP sender email | `noreply@tutorhubbd.com` |
| `EmailSettings__SenderPassword` | SMTP password/app password | `xxxx xxxx xxxx xxxx` |
| `StripeSettings__SecretKey` | Stripe secret key (LIVE) | `sk_live_...` |
| `StripeSettings__PublishableKey` | Stripe publishable key (LIVE) | `pk_live_...` |
| `GeminiApi__ApiKey` | Google Gemini API key | `AIzaSy...` |
| `AdminSettings__Email` | Initial admin email | `admin@tutorhubbd.com` |
| `AdminSettings__Password` | Initial admin password | `SecurePass123!` |

### Getting API Keys

1. **Stripe (Live Keys)**
   - Go to https://dashboard.stripe.com/apikeys
   - Toggle to "Live" mode
   - Copy the keys

2. **Google Gemini**
   - Go to https://makersuite.google.com/app/apikey
   - Create a new API key

3. **Gmail App Password**
   - Go to Google Account > Security
   - Enable 2-Step Verification
   - Generate App Password for "Mail"

---

## Database Setup

### Run Migrations

After deployment, run migrations:

```bash
# Using Azure CLI
az webapp ssh --name tutorhubbd --resource-group TutorHubBD-RG

# Or use Package Manager Console locally
dotnet ef database update --connection "YourProductionConnectionString"
```

### Or Apply Migrations on Startup

The application automatically applies migrations. You can also add this to Program.cs:

```csharp
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}
```

---

## Post-Deployment Checklist

### Security
- [ ] SSL certificate is active (HTTPS only)
- [ ] All API keys are production/live keys
- [ ] Admin password has been changed
- [ ] Database firewall rules are configured
- [ ] CORS is properly configured (if needed)

### Functionality
- [ ] Home page loads correctly
- [ ] User registration works (OTP email received)
- [ ] Login works for all roles
- [ ] Stripe payment processes (test with small amount)
- [ ] AI search returns results
- [ ] File uploads work (profile pictures, documents)
- [ ] Notifications appear correctly

### Performance
- [ ] Application Insights configured (optional)
- [ ] Health check endpoint responds: `/health`
- [ ] Static files are cached properly

---

## Monitoring & Maintenance

### Health Check
The application exposes a health check at `/health`. Use this for:
- Azure App Service health probes
- Uptime monitoring services
- Load balancer health checks

### Recommended Monitoring

1. **Azure Application Insights**
   - Add to project: `dotnet add package Microsoft.ApplicationInsights.AspNetCore`
   - Configure in Azure Portal

2. **Uptime Monitoring**
   - Use services like UptimeRobot, Pingdom, or Azure Monitor
   - Monitor `/health` endpoint

3. **Log Analysis**
   - Enable diagnostic logs in Azure
   - Stream to Log Analytics workspace

### Backup Strategy

1. **Database**
   - Enable Azure SQL automated backups
   - Configure point-in-time restore retention

2. **User Uploads**
   - Consider Azure Blob Storage for production file uploads
   - Enable soft delete and versioning

---

## Troubleshooting

### Common Issues

**1. 500 Internal Server Error**
- Check Application Logs in Azure Portal
- Verify connection string is correct
- Ensure database is accessible

**2. Email Not Sending**
- Verify Gmail app password (not regular password)
- Check if "Less secure apps" is needed
- Verify SMTP settings

**3. Stripe Payment Failing**
- Ensure using LIVE keys, not TEST keys
- Verify Stripe account is activated
- Check webhook configuration

**4. AI Search Not Working**
- Verify Gemini API key is valid
- Check API quota limits
- Review logs for specific errors

---

## Support

For deployment support:
- Email: support@tutorhubbd.com
- GitHub Issues: https://github.com/syed-rafi404/TutorHubBD/issues

---

## Quick Start Commands

```bash
# Clone repository
git clone https://github.com/syed-rafi404/TutorHubBD.git
cd TutorHubBD

# Restore and build
dotnet restore
dotnet build --configuration Release

# Run locally
dotnet run --project WebApplication1/TutorHubBD.Web.csproj

# Publish for deployment
dotnet publish -c Release -o ./publish
```

---

**Last Updated:** January 2025
**Version:** 1.0.0
