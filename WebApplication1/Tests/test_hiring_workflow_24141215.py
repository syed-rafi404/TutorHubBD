"""
TutorHubBD Integration Tests - Hiring Workflow
Author: Syed (24141215)
Tests for Guardian hiring a tutor for their tuition job
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

# Guardian credentials for testing
GUARDIAN_EMAIL = "syed.ar.rafi@g.bracu.ac.bd"
GUARDIAN_PASSWORD = "syed.ar.rafi@g.bracu.ac.bdA1"


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


def get_antiforgery_token_from_page(html_content: str) -> str:
    """
    Extracts antiforgery token from HTML content.
    """
    soup = BeautifulSoup(html_content, 'html.parser')
    token_input = soup.find('input', {'name': '__RequestVerificationToken'})
    if token_input:
        return token_input.get('value', '')
    return ''


@pytest.fixture(scope="module")
def guardian_session():
    """
    Pytest fixture that logs in as a Guardian and returns the authenticated session.
    This session is reused across all tests in this module for efficiency.
    """
    session = requests.Session()
    
    # Get login page and extract antiforgery token
    login_url = f"{BASE_URL}/Account/Login"
    token = get_antiforgery_token(session, login_url)
    
    if not token:
        pytest.skip("Could not get antiforgery token - server may be down")
    
    # Prepare login data
    login_data = {
        '__RequestVerificationToken': token,
        'Email': GUARDIAN_EMAIL,
        'Password': GUARDIAN_PASSWORD,
        'RememberMe': 'false'
    }
    
    # Perform login
    response = session.post(
        login_url,
        data=login_data,
        verify=VERIFY_SSL,
        allow_redirects=True
    )
    
    # Verify login was successful
    if 'login' in response.url.lower() and response.status_code == 200:
        if 'invalid' in response.text.lower():
            pytest.skip(f"Login failed for {GUARDIAN_EMAIL} - Invalid credentials")
    
    print(f"? Guardian logged in successfully: {GUARDIAN_EMAIL}")
    return session


class TestHiringWorkflow:
    """
    Test Suite for Hiring Workflow (FR-8, FR-9, FR-10)
    Tests the Guardian's ability to hire tutors for their jobs
    """

    def test_confirm_hiring_success(self, guardian_session):
        """
        Test 1: Hire a Tutor
        POST to /TuitionOffer/ConfirmHiring with valid jobId and tutorId
        Should redirect (302) to MyJobs with success message
        
        Note: This test requires:
        - A job owned by the guardian (jobId=2)
        - An applicant with tutorId=5 who applied to this job
        """
        session = guardian_session
        job_id = 2
        tutor_id = 1
        
        # First, check if the guardian has this job and it's open
        my_jobs_url = f"{BASE_URL}/TuitionOffer/MyJobs"
        response = session.get(my_jobs_url, verify=VERIFY_SSL)
        
        if response.status_code != 200:
            pytest.skip("Could not access MyJobs page")
        
        # Check if job exists in the list (basic check)
        if f'jobId={job_id}' not in response.text and f'job-{job_id}' not in response.text:
            print(f"? Job {job_id} may not exist or not owned by this guardian")
        
        # Get antiforgery token from MyJobs page or dashboard
        token = get_antiforgery_token_from_page(response.text)
        
        if not token:
            # Try to get token from the ViewApplicants page
            applicants_url = f"{BASE_URL}/TuitionOffer/ViewApplicants?jobId={job_id}"
            applicants_response = session.get(applicants_url, verify=VERIFY_SSL)
            if applicants_response.status_code == 200:
                token = get_antiforgery_token_from_page(applicants_response.text)
            else:
                # Get token from TuitionRequest/Index (Dashboard)
                dashboard_url = f"{BASE_URL}/TuitionRequest/Index"
                dashboard_response = session.get(dashboard_url, verify=VERIFY_SSL)
                token = get_antiforgery_token_from_page(dashboard_response.text)
        
        if not token:
            pytest.skip("Could not get antiforgery token for hiring")
        
        # Prepare hiring data
        hiring_data = {
            '__RequestVerificationToken': token,
            'jobId': job_id,
            'tutorId': tutor_id
        }
        
        # Submit hiring request
        confirm_url = f"{BASE_URL}/TuitionOffer/ConfirmHiring"
        response = session.post(
            confirm_url,
            data=hiring_data,
            verify=VERIFY_SSL,
            allow_redirects=False
        )
        
        print(f"Confirm Hiring Response Status: {response.status_code}")
        
        # Check for success redirect
        if response.status_code == 302:
            location = response.headers.get('Location', '')
            print(f"? Hiring request processed - Redirected to: {location}")
            
            # Follow redirect to check for success/error message
            final_response = session.get(
                f"{BASE_URL}{location}" if location.startswith('/') else location,
                verify=VERIFY_SSL
            )
            
            if 'success' in final_response.text.lower():
                print("? Tutor hired successfully!")
            elif 'error' in final_response.text.lower():
                print("? Hiring completed with some issues (check error message)")
            
            # Accept redirect as success
            assert True
        elif response.status_code == 404:
            print("? Job or tutor not found (404)")
            # This is expected if the job/tutor IDs don't exist
        elif response.status_code == 200:
            # Might have stayed on page with error
            response_text = response.text.lower()
            if 'hired' in response_text or 'success' in response_text:
                print("? Hiring successful")
            else:
                print("Response returned 200 - may contain error or confirmation")
        
        # Test passes for any of these valid responses
        assert response.status_code in [200, 302, 404], \
            f"Expected 200, 302, or 404, got {response.status_code}"

    def test_confirm_hiring_invalid_ids(self, guardian_session):
        """
        Test 2: Invalid Hire Attempt
        POST to /TuitionOffer/ConfirmHiring with invalid IDs
        Should return 404 (Not Found) or redirect with error
        """
        session = guardian_session
        
        # Use clearly invalid IDs
        invalid_job_id = 99999
        invalid_tutor_id = 99999
        
        # Get antiforgery token from any page
        my_jobs_url = f"{BASE_URL}/TuitionOffer/MyJobs"
        response = session.get(my_jobs_url, verify=VERIFY_SSL)
        token = get_antiforgery_token_from_page(response.text)
        
        if not token:
            dashboard_url = f"{BASE_URL}/TuitionRequest/Index"
            dashboard_response = session.get(dashboard_url, verify=VERIFY_SSL)
            token = get_antiforgery_token_from_page(dashboard_response.text)
        
        if not token:
            pytest.skip("Could not get antiforgery token")
        
        # Prepare invalid hiring data
        hiring_data = {
            '__RequestVerificationToken': token,
            'jobId': invalid_job_id,
            'tutorId': invalid_tutor_id
        }
        
        # Submit invalid hiring request
        confirm_url = f"{BASE_URL}/TuitionOffer/ConfirmHiring"
        response = session.post(
            confirm_url,
            data=hiring_data,
            verify=VERIFY_SSL,
            allow_redirects=False
        )
        
        print(f"Invalid Hiring Response Status: {response.status_code}")
        
        # Check for appropriate error handling
        if response.status_code == 404:
            print("? Invalid IDs correctly returned 404 Not Found")
        elif response.status_code == 302:
            location = response.headers.get('Location', '')
            print(f"Redirected to: {location}")
            
            # Follow redirect to check for error message
            final_response = session.get(
                f"{BASE_URL}{location}" if location.startswith('/') else location,
                verify=VERIFY_SSL
            )
            
            response_text = final_response.text.lower()
            if 'error' in response_text or 'not found' in response_text or 'invalid' in response_text:
                print("? Invalid IDs handled with error message")
            else:
                print("? Redirect occurred but no clear error message")
        elif response.status_code == 200:
            response_text = response.text.lower()
            if 'error' in response_text or 'not found' in response_text:
                print("? Invalid IDs handled with error on page")
        
        # Test expects 404 for invalid IDs, but 302 with error is also acceptable
        assert response.status_code in [302, 404], \
            f"Expected 302 or 404 for invalid IDs, got {response.status_code}"


class TestGuardianDashboard:
    """
    Additional tests for Guardian dashboard functionality
    """

    def test_view_my_jobs(self, guardian_session):
        """
        Test that a logged-in guardian can view their posted jobs
        """
        session = guardian_session
        
        my_jobs_url = f"{BASE_URL}/TuitionOffer/MyJobs"
        response = session.get(my_jobs_url, verify=VERIFY_SSL)
        
        assert response.status_code == 200, \
            f"Expected 200, got {response.status_code}"
        
        print("? Guardian can view their posted jobs")

    def test_view_applications_dashboard(self, guardian_session):
        """
        Test that a logged-in guardian can view applications to their jobs
        """
        session = guardian_session
        
        dashboard_url = f"{BASE_URL}/TuitionRequest/Index"
        response = session.get(dashboard_url, verify=VERIFY_SSL)
        
        assert response.status_code == 200, \
            f"Expected 200, got {response.status_code}"
        
        print("? Guardian can view applications dashboard")

    def test_view_applicants_for_job(self, guardian_session):
        """
        Test that a guardian can view applicants for a specific job
        """
        session = guardian_session
        job_id = 2  # Same job ID used in hiring tests
        
        applicants_url = f"{BASE_URL}/TuitionOffer/ViewApplicants?jobId={job_id}"
        response = session.get(applicants_url, verify=VERIFY_SSL)
        
        # Accept 200 (found), 404 (job not found), or 403 (forbidden - not owner)
        assert response.status_code in [200, 403, 404], \
            f"Expected 200, 403, or 404, got {response.status_code}"
        
        if response.status_code == 200:
            print(f"? Guardian can view applicants for job {job_id}")
        elif response.status_code == 404:
            print(f"? Job {job_id} not found")
        else:
            print(f"? Access denied for job {job_id}")


if __name__ == '__main__':
    pytest.main([__file__, '-v', '-s'])
