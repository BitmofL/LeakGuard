"""
OnlineScanner - Module for checking data leaks in online databases.
Uses HaveIBeenPwned API and optionally Shodan API.
"""

import requests
import time
import hashlib
from typing import Dict, List, Any, Optional
from dataclasses import dataclass, field
from datetime import datetime


@dataclass
class OnlineLeak:
    source: str
    leak_name: str
    description: str
    date: str
    data_classes: List[str]
    severity: str


class OnlineScanner:
    """Checks for data leaks using online APIs."""

    def __init__(self, api_key: str = "", hibp_enabled: bool = True, shodan_enabled: bool = False, shodan_api_key: str = ""):
        self.api_key = api_key
        self.hibp_enabled = hibp_enabled
        self.shodan_enabled = shodan_enabled
        self.shodan_api_key = shodan_api_key
        self.results: List[Dict[str, Any]] = []
        self.scan_progress = 0

    def check_email_hibp(self, email: str) -> Dict[str, Any]:
        """Check if email is found in HaveIBeenPwned database."""
        if not self.hibp_enabled:
            return {"status": "disabled", "email": email}

        result = {
            "email": email,
            "pwned": False,
            "breaches": [],
            "pastebins": [],
            "status": "checked"
        }

        try:
            # Check breaches
            headers = {
                "user-agent": "DataShieldScanner/1.0",
                "request-id": f"dss-{email}",
                "api-key": self.api_key
            }

            response = requests.get(
                f"https://haveibeenpwned.com/api/v3/breachedaccount/{email}",
                headers=headers,
                timeout=10
            )

            if response.status_code == 200:
                result["pwned"] = True
                for breach in response.json():
                    result["breaches"].append({
                        "name": breach.get("Name", "Unknown"),
                        "description": breach.get("Description", ""),
                        "date": breach.get("BreachedDate", "Unknown"),
                        "data_classes": breach.get("DataClasses", []),
                        "severity": "high" if any(d in str(breach.get("DataClasses", [])) for d in ["Passwords", "Credentials", "SSN"]) else "medium"
                    })
            elif response.status_code == 404:
                result["pwned"] = False
            elif response.status_code == 404:
                result["status"] = "rate_limited"
            else:
                result["status"] = f"error_{response.status_code}"

        except requests.exceptions.Timeout:
            result["status"] = "timeout"
        except Exception as e:
            result["status"] = f"error_{str(e)}"

        return result

    def check_email_pastebin(self, email: str) -> Dict[str, Any]:
        """Check if email appears in pastebin dumps."""
        result = {
            "email": email,
            "found_in_pastebin": False,
            "matches": []
        }

        try:
            headers = {
                "user-agent": "DataShieldScanner/1.0",
                "api-key": self.api_key
            }

            response = requests.get(
                f"https://haveibeenpwned.com/api/v3/pastebin/{email}",
                headers=headers,
                timeout=10
            )

            if response.status_code == 200:
                result["found_in_pastebin"] = True
                for paste in response.json():
                    result["matches"].append({
                        "site": "Pastebin",
                        "date": paste.get("PasteDate", "Unknown"),
                        "url": paste.get("Url", "")
                    })

        except Exception as e:
            result["status"] = f"error_{str(e)}"

        return result

    def check_password_hibp(self, password: str) -> Dict[str, Any]:
        """Check if password has been exposed (k-anonymity model)."""
        if not self.hibp_enabled:
            return {"status": "disabled"}

        result = {
            "status": "checked",
            "exposed": False,
            "count": 0
        }

        try:
            # Hash password (first 5 chars for API)
            sha1_hash = hashlib.sha1(password.encode('utf-8')).hexdigest().upper()
            prefix = sha1_hash[:5]
            suffix = sha1_hash[5:]

            response = requests.get(
                f"https://api.pwnedpasswords.com/range/{prefix}",
                headers={"user-agent": "DataShieldScanner/1.0"},
                timeout=10
            )

            if response.status_code == 200:
                # Check if our password hash is in the response
                for line in response.text.split('\n'):
                    hash_suffix = line.split(':')[0]
                    if hash_suffix == suffix:
                        result["exposed"] = True
                        result["count"] = int(line.split(':')[1])
                        break

        except Exception as e:
            result["status"] = f"error_{str(e)}"

        return result

    def check_device_shodan(self, ip_address: str) -> Dict[str, Any]:
        """Check if device is visible on Shodan (optional)."""
        if not self.shodan_enabled or not self.shodan_api_key:
            return {"status": "disabled"}

        result = {
            "ip": ip_address,
            "found": False,
            "data": {}
        }

        try:
            response = requests.get(
                f"https://api.shodan.io/shodan/host/{ip_address}",
                params={"key": self.shodan_api_key},
                timeout=10
            )

            if response.status_code == 200:
                data = response.json()
                result["found"] = True
                result["data"] = {
                    "ports": data.get("ports", []),
                    "vulns": data.get("vulns", []),
                    "os": data.get("os", "Unknown"),
                    "tags": data.get("tags", []),
                    "domains": data.get("domains", [])
                }

        except Exception as e:
            result["status"] = f"error_{str(e)}"

        return result

    def scan_all_online(self, emails: List[str], passwords: List[str] = None, ips: List[str] = None) -> List[Dict[str, Any]]:
        """Run all online checks."""
        self.results.clear()
        self.scan_progress = 0

        if passwords is None:
            passwords = []
        if ips is None:
            ips = []

        total_checks = len(emails) * 2 + len(passwords) + len(ips)
        completed = 0

        # Check emails
        for email in emails:
            # Check breaches
            breach_result = self.check_email_hibp(email)
            self.results.append(breach_result)
            completed += 1
            self.scan_progress = int((completed / max(total_checks, 1)) * 50)

            # Check pastebins
            paste_result = self.check_email_pastebin(email)
            self.results.append(paste_result)
            completed += 1
            self.scan_progress = int((completed / max(total_checks, 1)) * 50)

        # Check passwords
        for password in passwords:
            pwd_result = self.check_password_hibp(password)
            self.results.append(pwd_result)
            completed += 1
            self.scan_progress = 50 + int((completed / max(total_checks, 1)) * 30)

        # Check IPs with Shodan
        for ip in ips:
            shodan_result = self.check_device_shodan(ip)
            self.results.append(shodan_result)
            completed += 1
            self.scan_progress = 80 + int((completed / max(len(ips), 1)) * 20)

        return self.results
