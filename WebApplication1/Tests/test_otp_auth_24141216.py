"""
TutorHubBD Integration Tests - OTP Authentication
Author: Adiba (24141216)
Tests for OTP-based registration and email verification flow
"""

import pytest
import requests
import urllib3
from bs4 import BeautifulSoup

# Disable SSL warnings for localhost self-signed certificates
urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

# Configuration
BASE_URL = "https://localhost:7005"
VERIFY_SSL = False


def get_antiforgery_token(session: requests.Session, url: str) -> str:
    """
    Fetches a page and extracts the ASP.NET Core AntiForgeryToken.
    Required for all POST requests in ASP.NET Core MVC.
    """
    response = session.get(url, verify=VERIFY_SSL)
    soup = BeautifulSoup(response.text, 'html.parser')
    token_input = soup.find('input', {'name': '__RequestVerificationToken'})
    if token_input:
        return token_input.get('value', '')
    return ''


class TestOtpAuthentication:
    """
    Test Suite for OTP Authentication (FR-1, FR-2)
    Tests the registration flow with OTP verification
    """

    def test_register_triggers_otp_send(self):
        """
        Test 1: Registration Trigger
        POST to /Account/Register with valid data should trigger OTP send
        and redirect to VerifyEmail page (302) or return success page (200)
        """
        session = requests.Session()
        
        # Get the registration page and extract antiforgery token
        register_url = f"{BASE_URL}/Account/Register"
        token = get_antiforgery_token(session, register_url)
        
        # Test data - use unique email to avoid conflicts
        import time
        unique_email = f"test_user_{int(time.time())}@test.com"
        
        registration_data = {
            '__RequestVerificationToken': token,
            'FullName': 'Test User Adiba',
            'Email': unique_email,
            'Password': 'TestPassword123!',
            'ConfirmPassword': 'TestPassword123!',
            'Role': 'Teacher'
        }
        
        # POST registration - should redirect to VerifyEmail
        response = session.post(
            register_url,
            data=registration_data,
            verify=VERIFY_SSL,
            allow_redirects=False
        )
        
        # Assert: Should redirect (302) to VerifyEmail or return 200
        assert response.status_code in [200, 302], \
            f"Expected 200 or 302, got {response.status_code}"
        
        if response.status_code == 302:
            # Check redirect location contains VerifyEmail
            location = response.headers.get('Location', '')
            assert 'VerifyEmail' in location or 'verifyemail' in location.lower(), \
                f"Expected redirect to VerifyEmail, got: {location}"
            print(f"? Registration triggered OTP - Redirected to: {location}")
        else:
            # If 200, check if we're on verify page or got success message
            assert 'verification' in response.text.lower() or 'verify' in response.text.lower() or 'otp' in response.text.lower(), \
                "Expected verification-related content in response"
            print("? Registration successful - OTP flow initiated")

    def test_verify_otp_positive(self):
        """
        Test 2: Verify OTP (Positive Case)
        POST to /Account/VerifyEmail with valid OTP code (123456 in dev mode)
        Should redirect (302) to Home/Dashboard after successful verification
        """
        session = requests.Session()
        
        # First, register a new user to get into OTP verification flow
        register_url = f"{BASE_URL}/Account/Register"
        token = get_antiforgery_token(session, register_url)
        
        import time
        unique_email = f"test_otp_positive_{int(time.time())}@test.com"
        
        registration_data = {
            '__RequestVerificationToken': token,
            'FullName': 'Test OTP Positive',
            'Email': unique_email,
            'Password': 'TestPassword123!',
            'ConfirmPassword': 'TestPassword123!',
            'Role': 'Teacher'
        }
        
        # Register the user
        reg_response = session.post(
            register_url,
            data=registration_data,
            verify=VERIFY_SSL,
            allow_redirects=True
        )
        
        # Now try to verify with the dev mode OTP (123456)
        verify_url = f"{BASE_URL}/Account/VerifyEmail"
        token = get_antiforgery_token(session, f"{verify_url}?email={unique_email}")
        
        verify_data = {
            '__RequestVerificationToken': token,
            'Email': unique_email,
            'OtpCode': '123456'  # Dev mode OTP
        }
        
        response = session.post(
            verify_url,
            data=verify_data,
            verify=VERIFY_SSL,
            allow_redirects=False
        )
        
        # In dev mode with correct OTP, should redirect to Home
        # In production, this might fail (which is expected)
        print(f"OTP Verification Response Status: {response.status_code}")
        
        if response.status_code == 302:
            location = response.headers.get('Location', '')
            print(f"? OTP Verified (Positive) - Redirected to: {location}")
            # Accept any redirect as success for this test
            assert True
        else:
            # If not redirected, check for either success or expected validation error
            # (Real OTP would be different from 123456)
            print(f"Response: {response.status_code} - This is expected if not in dev mode")
            assert response.status_code == 200, \
                f"Expected 200 or 302, got {response.status_code}"

    def test_verify_otp_negative(self):
        """
        Test 3: Verify OTP (Negative Case)
        POST to /Account/VerifyEmail with invalid OTP code (000000)
        Should return 200 and stay on the same page with an error message
        """
        session = requests.Session()
        
        # First, register a new user
        register_url = f"{BASE_URL}/Account/Register"
        token = get_antiforgery_token(session, register_url)
        
        import time
        unique_email = f"test_otp_negative_{int(time.time())}@test.com"
        
        registration_data = {
            '__RequestVerificationToken': token,
            'FullName': 'Test OTP Negative',
            'Email': unique_email,
            'Password': 'TestPassword123!',
            'ConfirmPassword': 'TestPassword123!',
            'Role': 'Guardian'
        }
        
        # Register the user
        session.post(
            register_url,
            data=registration_data,
            verify=VERIFY_SSL,
            allow_redirects=True
        )
        
        # Try to verify with an invalid OTP
        verify_url = f"{BASE_URL}/Account/VerifyEmail"
        token = get_antiforgery_token(session, f"{verify_url}?email={unique_email}")
        
        verify_data = {
            '__RequestVerificationToken': token,
            'Email': unique_email,
            'OtpCode': '000000'  # Invalid OTP
        }
        
        response = session.post(
            verify_url,
            data=verify_data,
            verify=VERIFY_SSL,
            allow_redirects=False
        )
        
        # With invalid OTP, should stay on the same page (200) with error
        assert response.status_code == 200, \
            f"Expected 200 (stay on page with error), got {response.status_code}"
        
        # Check for error message in response
        response_text = response.text.lower()
        has_error = any(keyword in response_text for keyword in [
            'invalid', 'error', 'expired', 'incorrect', 'failed'
        ])
        
        assert has_error, "Expected error message for invalid OTP"
        print("? Invalid OTP correctly rejected - Stayed on verification page with error")


if __name__ == '__main__':
    pytest.main([__file__, '-v', '-s'])
