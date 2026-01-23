# TutorHubBD Integration Tests

This folder contains Python integration tests for the TutorHubBD ASP.NET Core application.

## Authors
- **24141216** - Adiba (OTP Authentication Tests)
- **23241080** - Sidrat (Tuition Job Application Tests)
- **24141215** - Syed (Hiring Workflow Tests)

## Prerequisites

1. **Python 3.8+** installed on your system
2. **TutorHubBD application** running on `https://localhost:7005`
3. **Test accounts** created in the database:
   - Teacher: `superdcmarvel420@gmail.com` / `superdcmarvel420@gmail.comA1`
   - Guardian: `syed.ar.rafi@g.bracu.ac.bd` / `syed.ar.rafi@g.bracu.ac.bdA1`

## Setup Instructions

### Step 1: Navigate to the tests folder
```bash
cd WebApplication1/tests
```

### Step 2: Create a virtual environment (recommended)
```bash
# Windows
python -m venv venv
venv\Scripts\activate

# Linux/Mac
python3 -m venv venv
source venv/bin/activate
```

### Step 3: Install dependencies
```bash
pip install -r requirements.txt
```

### Step 4: Start the ASP.NET Core application
Make sure your TutorHubBD application is running:
```bash
# From the WebApplication1 directory
dotnet run
```
The application should be available at `https://localhost:7005`

## Running the Tests

### Run all tests
```bash
pytest -v -s
```

### Run specific test files

**OTP Authentication Tests (Adiba):**
```bash
pytest 24141216_otp_auth.test.py -v -s
```

**Tuition Application Tests (Sidrat):**
```bash
pytest 23241080_tuition_apply.test.py -v -s
```

**Hiring Workflow Tests (Syed):**
```bash
pytest 24141215_hiring_workflow.test.py -v -s
```

### Run tests with detailed output
```bash
pytest -v -s --tb=short
```

### Run tests and generate HTML report
```bash
pip install pytest-html
pytest --html=report.html -v
```

## Test Descriptions

### File 1: `24141216_otp_auth.test.py` (Adiba)
| Test | Description | Expected Result |
|------|-------------|-----------------|
| `test_register_triggers_otp_send` | POST to /Account/Register with valid data | 302 redirect to VerifyEmail or 200 |
| `test_verify_otp_positive` | POST to /Account/VerifyEmail with code 123456 | 302 redirect to Dashboard (dev mode) |
| `test_verify_otp_negative` | POST to /Account/VerifyEmail with code 000000 | 200 stay on page with error |

### File 2: `23241080_tuition_apply.test.py` (Sidrat)
| Test | Description | Expected Result |
|------|-------------|-----------------|
| `test_apply_to_job_success` | POST to /TuitionRequest/Apply with jobId=1 | 302 redirect to Success or 200 |
| `test_duplicate_application_rejected` | POST to /TuitionRequest/Apply again | Validation error in response |
| `test_browse_available_jobs` | GET /TuitionOffer/Index | 200 OK |

### File 3: `24141215_hiring_workflow.test.py` (Syed)
| Test | Description | Expected Result |
|------|-------------|-----------------|
| `test_confirm_hiring_success` | POST to /TuitionOffer/ConfirmHiring with valid IDs | 302 redirect to MyJobs |
| `test_confirm_hiring_invalid_ids` | POST with invalid jobId=99999, tutorId=99999 | 404 Not Found |
| `test_view_my_jobs` | GET /TuitionOffer/MyJobs | 200 OK |
| `test_view_applications_dashboard` | GET /TuitionRequest/Index | 200 OK |

## Important Notes

1. **SSL Certificates**: Tests use `verify=False` to ignore self-signed certificate errors on localhost.

2. **Antiforgery Tokens**: All POST requests include the `__RequestVerificationToken` which is automatically extracted from the page.

3. **Session Management**: Tests use `requests.Session()` to maintain cookies and authentication state.

4. **Test Data**: 
   - Job IDs and Tutor IDs in tests (1, 2, 5) should exist in your database
   - If they don't exist, tests will skip or handle gracefully

5. **Dev Mode OTP**: Test `test_verify_otp_positive` assumes OTP code `123456` works in development mode. In production, this test may fail (expected behavior).

## Troubleshooting

### "Could not get antiforgery token"
- Ensure the application is running on `https://localhost:7005`
- Check if the application is accessible in your browser

### "Login failed - Invalid credentials"
- Verify the test accounts exist in the database
- Check password requirements match

### Connection refused
- Start the ASP.NET Core application first
- Verify the port number (7005) is correct

### SSL Certificate errors
- Tests already disable SSL verification with `verify=False`
- If issues persist, check your Python/requests version

## File Structure
```
tests/
??? requirements.txt              # Python dependencies
??? README.md                     # This file
??? 24141216_otp_auth.test.py    # Adiba's OTP tests
??? 23241080_tuition_apply.test.py # Sidrat's application tests
??? 24141215_hiring_workflow.test.py # Syed's hiring tests
```
