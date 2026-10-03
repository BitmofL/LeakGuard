"""
LocalScanner - Module for scanning local system for personal information leaks.
Scans files, browser data, installed applications, registry, and more.
"""

import os
import re
import json
import glob
import hashlib
import subprocess
from pathlib import Path
from datetime import datetime
from typing import Dict, List, Any, Optional
from dataclasses import dataclass, field


@dataclass
class ScanResult:
    category: str
    severity: str  # "low", "medium", "high", "critical"
    title: str
    description: str
    data: Any
    timestamp: str = field(default_factory=lambda: datetime.now().isoformat())


class LocalScanner:
    """Scans local system for personal information exposure."""

    def __init__(self):
        self.results: List[ScanResult] = []
        self.scan_progress = 0
        self.scan_categories = []
        self.total_categories = 0
        self._current_category = ""

    def _update_progress(self, category: str, progress: int):
        self.scan_progress = progress
        self.scan_categories = [category]
        self._current_category = category

    def _add_result(self, category: str, severity: str, title: str, description: str, data: Any):
        self.results.append(ScanResult(
            category=category,
            severity=severity,
            title=title,
            description=description,
            data=data
        ))

    def scan_all(self, options: Dict[str, bool] = None) -> List[ScanResult]:
        """Run all enabled scans."""
        self.results.clear()
        self.scan_progress = 0
        if options is None:
            options = {
                "personal_info": True,
                "emails": True,
                "phone_numbers": True,
                "social_accounts": True,
                "documents": True,
                "passwords_tokens": True,
                "geolocation_history": True,
                "digital_fingerprints": True,
                "nicknames": True,
                "birth_year": True,
                "games_installed": True,
                "applications_installed": True
            }

        self.total_categories = sum(options.values())
        completed = 0

        if options.get("digital_fingerprints"):
            self._current_category = "Digital Fingerprints"
            self.scan_digital_fingerprints()
            completed += 1
            self.scan_progress = int((completed / self.total_categories) * 100)

        if options.get("personal_info"):
            self._current_category = "Personal Information"
            self.scan_personal_info()
            completed += 1
            self.scan_progress = int((completed / self.total_categories) * 100)

        if options.get("emails"):
            self._current_category = "Email Addresses"
            self.scan_emails()
            completed += 1
            self.scan_progress = int((completed / self.total_categories) * 100)

        if options.get("phone_numbers"):
            self._current_category = "Phone Numbers"
            self.scan_phone_numbers()
            completed += 1
            self.scan_progress = int((completed / self.total_categories) * 100)

        if options.get("nicknames"):
            self._current_category = "Nicknames & Usernames"
            self.scan_nicknames()
            completed += 1
            self.scan_progress = int((completed / self.total_categories) * 100)

        if options.get("birth_year"):
            self._current_category = "Birth Year & Age"
            self.scan_birth_year()
            completed += 1
            self.scan_progress = int((completed / self.total_categories) * 100)

        if options.get("social_accounts"):
            self._current_category = "Social Accounts"
            self.scan_social_accounts()
            completed += 1
            self.scan_progress = int((completed / self.total_categories) * 100)

        if options.get("documents"):
            self._current_category = "Documents & Files"
            self.scan_documents()
            completed += 1
            self.scan_progress = int((completed / self.total_categories) * 100)

        if options.get("passwords_tokens"):
            self._current_category = "Passwords & Tokens"
            self.scan_passwords_and_tokens()
            completed += 1
            self.scan_progress = int((completed / self.total_categories) * 100)

        if options.get("geolocation_history"):
            self._current_category = "Geolocation & History"
            self.scan_geolocation_history()
            completed += 1
            self.scan_progress = int((completed / self.total_categories) * 100)

        if options.get("games_installed"):
            self._current_category = "Installed Games"
            self.scan_games()
            completed += 1
            self.scan_progress = int((completed / self.total_categories) * 100)

        if options.get("applications_installed"):
            self._current_category = "Installed Applications"
            self.scan_applications()
            completed += 1
            self.scan_progress = 100

        return self.results

    def scan_digital_fingerprints(self):
        """Collect device digital fingerprints."""
        try:
            # Computer name and username
            computer_name = os.environ.get("COMPUTERNAME", "Unknown")
            username = os.environ.get("USERNAME", "Unknown")
            user_profile = os.environ.get("USERPROFILE", "")

            # Get MAC address
            mac_address = "Unknown"
            try:
                cmd = "getmac /fo CSV /nh"
                result = subprocess.run(cmd, shell=True, capture_output=True, text=True, timeout=5)
                if result.returncode == 0:
                    lines = result.stdout.strip().split("\n")
                    if lines:
                        mac_address = lines[0].split(",")[1].strip('"')
            except:
                pass

            # Get IP addresses
            ip_addresses = []
            try:
                hostname = socket.gethostname()
                ip_addresses.append(socket.gethostbyname(hostname))
            except:
                pass

            # Get system info
            system_info = {
                "computer_name": computer_name,
                "username": username,
                "user_profile": user_profile,
                "mac_address": mac_address,
                "ip_addresses": ip_addresses,
                "python_version": subprocess.getoutput("python --version") if os.name == "nt" else "N/A"
            }

            # Get Windows version
            try:
                import platform
                system_info["os_version"] = platform.platform()
                system_info["machine"] = platform.machine()
                system_info["processor"] = platform.processor()
            except:
                pass

            self._add_result(
                "digital_fingerprints",
                "medium",
                "Device Digital Fingerprint",
                "Collected device identifiers that can be used to identify your system",
                system_info
            )
        except Exception as e:
            self._add_result(
                "digital_fingerprints",
                "low",
                "Fingerprint Scan Error",
                f"Could not collect some fingerprint data: {str(e)}",
                {}
            )

    def scan_personal_info(self):
        """Scan for personal information in common locations."""
        personal_data = {
            "files_with_names": [],
            "registry_personal_info": {}
        }

        # Scan common document locations
        scan_paths = [
            os.path.join(os.environ.get("USERPROFILE", ""), "Documents"),
            os.path.join(os.environ.get("USERPROFILE", ""), "Desktop"),
            os.path.join(os.environ.get("USERPROFILE", ""), "Downloads"),
            os.path.join(os.environ.get("USERPROFILE", ""), "Pictures"),
        ]

        name_patterns = [
            r'\b[A-Z][a-z]+ [A-Z][a-z]+\b',  # Russian/English names
            r'\b[A-Z][a-z]+ [A-Z][a-z]+ [A-Z][a-z]+\b',  # Full names
        ]

        for scan_path in scan_paths:
            if os.path.exists(scan_path):
                for root, dirs, files in os.walk(scan_path):
                    for file in files[:100]:  # Limit to prevent slow scan
                        filepath = os.path.join(root, file)
                        try:
                            if file.lower().endswith(('.txt', '.doc', '.docx', '.pdf', '.xls', '.xlsx')):
                                personal_data["files_with_names"].append(filepath)
                        except:
                            pass

        # Get registry info
        try:
            import winreg
            try:
                key = winreg.OpenKey(winreg.HKEY_CURRENT_USER, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\ShellFolders")
                personal_data["registry_personal_info"] = "Collected"
            except:
                pass
        except:
            pass

        self._add_result(
            "personal_info",
            "medium",
            "Personal Information in Files",
            f"Found {len(personal_data['files_with_names'])} files that may contain personal information",
            personal_data
        )

    def scan_emails(self):
        """Scan for email addresses in various locations."""
        emails = set()
        email_pattern = r'[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}'

        # Scan browser data
        browser_paths = [
            os.path.join(os.environ.get("APPDATA", ""), "Mozilla\\Firefox\\Profiles"),
            os.path.join(os.environ.get("LOCALAPPDATA", ""), "Google\\Chrome\\User Data"),
            os.path.join(os.environ.get("LOCALAPPDATA", ""), "Microsoft\\Edge\\User Data"),
        ]

        for browser_path in browser_paths:
            if os.path.exists(browser_path):
                for root, dirs, files in os.walk(browser_path):
                    for file in files:
                        if file.endswith(('.json', '.sqlite', '.log', '.txt')):
                            filepath = os.path.join(root, file)
                            try:
                                with open(filepath, 'r', encoding='utf-8', errors='ignore') as f:
                                    content = f.read(10000)  # Read first 10KB
                                    found_emails = re.findall(email_pattern, content)
                                    emails.update(found_emails)
                            except:
                                pass

        # Scan common files
        scan_paths = [
            os.path.join(os.environ.get("USERPROFILE", ""), "Documents"),
            os.path.join(os.environ.get("USERPROFILE", ""), "Desktop"),
        ]

        for scan_path in scan_paths:
            if os.path.exists(scan_path):
                for root, dirs, files in os.walk(scan_path):
                    for file in files[:50]:
                        if file.endswith(('.txt', '.doc', '.docx', '.pdf', '.xls', '.xlsx', '.eml', '.msg')):
                            filepath = os.path.join(root, file)
                            try:
                                with open(filepath, 'r', encoding='utf-8', errors='ignore') as f:
                                    content = f.read(5000)
                                    found_emails = re.findall(email_pattern, content)
                                    emails.update(found_emails)
                            except:
                                pass

        self._add_result(
            "emails",
            "high" if len(emails) > 0 else "low",
            "Email Addresses Found",
            f"Found {len(emails)} email addresses in system files and browsers",
            list(emails) if emails else []
        )

    def scan_phone_numbers(self):
        """Scan for phone numbers in contacts and files."""
        phone_numbers = set()
        phone_patterns = [
            r'\+7?\s?[\(]?\d{3}[\)]?\s?\d{3}[-]?\d{2}[-]?\d{2}',  # Russian format
            r'\+?[1-9]\d{10,14}',  # International format
        ]

        # Scan contacts and common files
        scan_paths = [
            os.path.join(os.environ.get("USERPROFILE", ""), "Contacts"),
            os.path.join(os.environ.get("USERPROFILE", ""), "Documents"),
        ]

        for scan_path in scan_paths:
            if os.path.exists(scan_path):
                for root, dirs, files in os.walk(scan_path):
                    for file in files[:50]:
                        if file.endswith(('.txt', '.csv', '.vcf', '.json', '.xml')):
                            filepath = os.path.join(root, file)
                            try:
                                with open(filepath, 'r', encoding='utf-8', errors='ignore') as f:
                                    content = f.read(5000)
                                    for pattern in phone_patterns:
                                        found = re.findall(pattern, content)
                                        phone_numbers.update(found)
                            except:
                                pass

        self._add_result(
            "phone_numbers",
            "high" if len(phone_numbers) > 0 else "low",
            "Phone Numbers Found",
            f"Found {len(phone_numbers)} phone numbers in system files",
            list(phone_numbers) if phone_numbers else []
        )

    def scan_nicknames(self):
        """Scan for nicknames and usernames in various applications."""
        nicknames = set()
        
        # Check browser saved usernames
        browser_paths = [
            os.path.join(os.environ.get("APPDATA", ""), "Mozilla\\Firefox\\Profiles"),
            os.path.join(os.environ.get("LOCALAPPDATA", ""), "Google\\Chrome\\User Data"),
        ]

        for browser_path in browser_paths:
            if os.path.exists(browser_path):
                for root, dirs, files in os.walk(browser_path):
                    for file in files:
                        if file in ('logins.json', 'formhistory.sqlite', 'places.sqlite'):
                            filepath = os.path.join(root, file)
                            try:
                                with open(filepath, 'r', encoding='utf-8', errors='ignore') as f:
                                    content = f.read(5000)
                                    # Look for username patterns
                                    username_patterns = [
                                        r'"username"\s*:\s*"([^"]+)"',
                                        r'"name"\s*:\s*"([^"]+)"',
                                        r'"login"\s*:\s*"([^"]+)"',
                                    ]
                                    for pattern in username_patterns:
                                        found = re.findall(pattern, content)
                                        nicknames.update(found)
                            except:
                                pass

        # Check environment variables
        env_vars = {
            "USERNAME": os.environ.get("USERNAME", ""),
            "COMPUTERNAME": os.environ.get("COMPUTERNAME", ""),
        }

        self._add_result(
            "nicknames",
            "medium",
            "Nicknames & Usernames",
            f"Found {len(nicknames)} potential nicknames/usernames",
            {
                "nicknames": list(nicknames) if nicknames else [],
                "environment_usernames": env_vars
            }
        )

    def scan_birth_year(self):
        """Scan for birth year and age information."""
        birth_info = {
            "found_dates": [],
            "possible_birth_years": []
        }

        # Scan documents for dates
        scan_paths = [
            os.path.join(os.environ.get("USERPROFILE", ""), "Documents"),
            os.path.join(os.environ.get("USERPROFILE", ""), "Desktop"),
        ]

        date_pattern = r'\b(19|20)\d{2}\b'

        for scan_path in scan_paths:
            if os.path.exists(scan_path):
                for root, dirs, files in os.walk(scan_path):
                    for file in files[:30]:
                        if file.endswith(('.txt', '.doc', '.docx', '.pdf')):
                            filepath = os.path.join(root, file)
                            try:
                                with open(filepath, 'r', encoding='utf-8', errors='ignore') as f:
                                    content = f.read(3000)
                                    dates = re.findall(date_pattern, content)
                                    birth_info["found_dates"].extend(dates)
                            except:
                                pass

        # Extract possible birth years (1950-2010)
        for date in birth_info["found_dates"]:
            year = int(date) if date else 0
            if 1950 <= year <= 2010:
                birth_info["possible_birth_years"].append(year)

        self._add_result(
            "birth_year",
            "medium",
            "Birth Year Information",
            f"Found {len(set(birth_info['possible_birth_years']))} possible birth years",
            birth_info
        )

    def scan_social_accounts(self):
        """Scan for social media accounts and traces."""
        social_platforms = {
            "VK": ["vk.com", "vk.me"],
            "Telegram": ["t.me", "telegram.org"],
            "Discord": ["discord.com", "discord.gg"],
            "GitHub": ["github.com"],
            "Twitter": ["twitter.com", "x.com"],
            "Facebook": ["facebook.com"],
            "Instagram": ["instagram.com"],
        }

        accounts_found = {}
        
        # Check browser history and data
        browser_paths = [
            os.path.join(os.environ.get("LOCALAPPDATA", ""), "Google\\Chrome\\User Data\\Default"),
            os.path.join(os.environ.get("LOCALAPPDATA", ""), "Microsoft\\Edge\\User Data\\Default"),
        ]

        for browser_path in browser_path
