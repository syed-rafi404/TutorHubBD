# TutorHubBD ??

An AI-Powered Tuition Marketplace Platform for Bangladesh

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Deploy](https://github.com/syed-rafi404/TutorHubBD/actions/workflows/deploy.yml/badge.svg)](https://github.com/syed-rafi404/TutorHubBD/actions)

## ?? Overview

TutorHubBD is a comprehensive tuition marketplace platform that connects guardians with qualified tutors in Bangladesh. The platform features AI-powered search, secure payments via Stripe, and a complete hiring workflow.

## ? Features

### For Guardians
- ?? Post tuition job listings
- ?? AI-powered tutor search using natural language
- ?? Review and shortlist applicants (max 5)
- ? Hire tutors with automated commission system
- ? Rate and review tutors after hiring

### For Teachers
- ?? Browse available tuition jobs
- ?? AI-powered job search
- ?? Professional profile management
- ?? Document verification upload
- ?? Pay commissions via Stripe

### For Administrators
- ?? User management
- ?? Tutor verification approval
- ?? Financial reports and revenue tracking
- ?? System notifications management

### Technical Features
- ?? OTP-based email verification
- ?? Role-based access control (Guardian, Teacher, Admin)
- ?? Stripe payment integration
- ?? Google Gemini AI integration
- ?? Fully responsive design
- ?? Real-time notifications

## ?? Quick Start

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server](https://www.microsoft.com/sql-server) (LocalDB for development)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/)

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/syed-rafi404/TutorHubBD.git
   cd TutorHubBD
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore WebApplication1/TutorHubBD.Web.csproj
   ```

3. **Update database**
   ```bash
   cd WebApplication1
   dotnet ef database update
   ```

4. **Run the application**
   ```bash
   dotnet run
   ```

5. **Open in browser**
   ```
   https://localhost:7005
   ```

## ?? Configuration

### Development Configuration

The application uses `appsettings.json` for configuration. Key settings:

| Setting | Description |
|---------|-------------|
| `ConnectionStrings:DefaultConnection` | Database connection string |
| `EmailSettings` | SMTP configuration for emails |
| `StripeSettings` | Stripe API keys for payments |
| `GeminiApi:ApiKey` | Google Gemini API for AI search |
| `AdminSettings` | Initial admin account credentials |

### Production Configuration

For production, use environment variables. See [DEPLOYMENT.md](DEPLOYMENT.md) for details.

## ?? Testing

### Run Unit Tests
```bash
dotnet test WebApplication1/TutorHubBD.Web.csproj
```

### Run Integration Tests (Python)
```bash
cd WebApplication1/Tests
pip install -r requirements.txt
pytest -v -s
```

## ?? Docker Deployment

```bash
# Build and run with Docker Compose
cp .env.example .env
# Edit .env with your configuration
docker-compose up -d
```

## ?? Azure Deployment

See [DEPLOYMENT.md](DEPLOYMENT.md) for detailed Azure deployment instructions.

## ?? Project Structure

```
TutorHubBD/
??? WebApplication1/
?   ??? Controllers/          # MVC Controllers
?   ??? Models/               # Data models & ViewModels
?   ??? Views/                # Razor views
?   ??? Services/             # Business logic services
?   ??? Data/                 # DbContext & migrations
?   ??? wwwroot/              # Static files (CSS, JS, images)
?   ??? Tests/                # Python integration tests
??? .github/workflows/        # CI/CD pipelines
??? Dockerfile                # Container configuration
??? docker-compose.yml        # Multi-container setup
??? DEPLOYMENT.md             # Deployment guide
??? README.md                 # This file
```

## ?? Security Features

- ? Password hashing with ASP.NET Identity
- ? HTTPS enforced in production
- ? CSRF protection on all forms
- ? SQL injection prevention via Entity Framework
- ? XSS protection with Razor encoding
- ? Account lockout after failed attempts

## ??? Tech Stack

| Category | Technology |
|----------|------------|
| **Backend** | ASP.NET Core 8.0, Entity Framework Core |
| **Frontend** | Razor Views, Bootstrap 5, Bootstrap Icons |
| **Database** | SQL Server / Azure SQL |
| **Authentication** | ASP.NET Identity with OTP |
| **Payments** | Stripe |
| **AI** | Google Gemini API |
| **Email** | SMTP (Gmail) |
| **Hosting** | Azure App Service / Docker |

## ?? API Endpoints

| Endpoint | Method | Description | Auth Required |
|----------|--------|-------------|---------------|
| `/health` | GET | Health check | No |
| `/Account/Login` | POST | User login | No |
| `/Account/Register` | POST | User registration | No |
| `/TuitionOffer/Index` | GET | Browse jobs | Teacher/Admin |
| `/TuitionOffer/Create` | POST | Create job | Guardian |
| `/TuitionRequest/Apply` | POST | Apply for job | Teacher |
| `/TuitionOffer/ConfirmHiring` | POST | Hire tutor | Guardian |
| `/Admin/Index` | GET | Admin dashboard | Admin |

## ?? Development Team

| Name | ID | Contribution |
|------|-----|--------------|
| Syed Rafi | - | Lead Developer, Architecture |
| Adiba | 24141216 | OTP Authentication System |
| Sidrat | 23241080 | Job Application System |
| Syed | 24141215 | Hiring Workflow |

## ?? License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## ?? Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit changes (`git commit -m 'Add amazing feature'`)
4. Push to branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## ?? Support

- ?? Email: support@tutorhubbd.com
- ?? Website: https://tutorhubbd.com
- ?? Phone: +8801471-817161

---

<p align="center">Made with ?? in Bangladesh</p>
