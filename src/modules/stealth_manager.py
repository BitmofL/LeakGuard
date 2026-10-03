"""
StealthManager - Module for stealth mode and autostart management.
Handles registry autostart, process hiding options, and secure operations.
"""

import os
import sys
import ctypes
import logging
from pathlib import Path
from typing import Optional
from datetime import datetime


class StealthManager:
    """Manages stealth operations and autostart functionality."""

    def __init__(self):
        self.is_admin = self._check_admin()
        self.logger = self._setup_logger()

    def _check_admin(self) -> bool:
        """Check if running with admin privileges."""
        try:
            return ctypes.windll.shell32.IsUserAnAdmin() != 0
        except:
            return False

    def _setup_logger(self) -> logging.Logger:
        """Setup silent logger."""
        logger = logging.getLogger("DataShieldScanner")
        logger.setLevel(logging.WARNING)
        # No handlers - silent operation
        return logger

    def enable_autostart(self, registry_key: str = "HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Run\\DataShieldScanner") -> bool:
        """Enable autostart via Windows Registry (current user only - no admin needed)."""
        try:
            import winreg

            # Get current executable path
            current_path = sys.executable

            # Open registry key
            key = winreg.OpenKey(
                winreg.HKEY_CURRENT_USER,
                "Software\\Microsoft\\Windows\\CurrentVersion\\Run",
                0,
                winreg.KEY_SET_VALUE
            )

            # Set value
            winreg.SetValueEx(key, "DataShieldScanner", 0, winreg.REG_SZ, current_path)
            winreg.CloseKey(key)

            self.logger.info("Autostart enabled successfully")
            return True

        except Exception as e:
            self.logger.error(f"Failed to enable autostart: {str(e)}")
            return False

    def disable_autostart(self, registry_key: str = "DataShieldScanner") -> bool:
        """Disable autostart via Windows Registry."""
        try:
            import winreg

            key = winreg.OpenKey(
                winreg.HKEY_CURRENT_USER,
                "Software\\Microsoft\\Windows\\CurrentVersion\\Run",
                0,
                winreg.KEY_SET_VALUE
            )

            winreg.DeleteValue(key, registry_key)
            winreg.CloseKey(key)

            self.logger.info("Autostart disabled successfully")
            return True

        except FileNotFoundError:
            # Key doesn't exist, consider it disabled
            return True
        except Exception as e:
            self.logger.error(f"Failed to disable autostart: {str(e)}")
            return False

    def is_autostart_enabled(self, registry_key: str = "DataShieldScanner") -> bool:
        """Check if autostart is enabled."""
        try:
            import winreg

            key = winreg.OpenKey(
                winreg.HKEY_CURRENT_USER,
                "Software\\Microsoft\\Windows\\CurrentVersion\\Run",
                0,
                winreg.KEY_READ
            )

            value, _ = winreg.QueryValueEx(key, registry_key)
            winreg.CloseKey(key)

            return True

        except FileNotFoundError:
            return False
        except Exception:
            return False

    def create_secure_temp_file(self, data: str, extension: str = ".enc") -> str:
        """Create a securely named temporary encrypted file."""
        import tempfile

        # Generate random filename in secure temp folder
        temp_dir = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "temp", "secure_store")
        os.makedirs(temp_dir, exist_ok=True)

        # Generate random filename
        timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
        random_suffix = os.urandom(8).hex()
        filename = f"ds_{timestamp}_{random_suffix}{extension}"
        filepath = os.path.join(temp_dir, filename)

        # Write encrypted data
        with open(filepath, 'w') as f:
            f.write(data)

        # Set hidden attribute
        try:
            os.system(f'attrib +h "{filepath}"')
        except:
            pass

        return filepath

    def secure_delete_file(self, filepath: str) -> bool:
        """Securely delete a file by overwriting before deletion."""
        try:
            if not os.path.exists(filepath):
                return True

            # Get file size
            file_size = os.path.getsize(filepath)

            # Overwrite with random data (3 passes)
            for _ in range(3):
                with open(filepath, 'wb') as f:
                    f.write(os.urandom(file_size))
                    f.flush()
                    os.fsync(f.fileno())

            # Remove hidden attribute and delete
            try:
                os.system(f'attrib -h "{filepath}"')
            except:
                pass

            os.remove(filepath)
            return True

        except Exception as e:
            self.logger.error(f"Failed to securely delete file: {str(e)}")
            return False

    def get_usb_drives(self) -> list:
        """Get list of available USB drives."""
        drives = []
        from ctypes import wintypes

        bitmask = ctypes.windll.kernel32.GetLogicalDrives()
        for letter in range(65, 91):  # A-Z
            if bitmask & 1:
                drive = f"{chr(letter)}:\\"
                try:
                    drive_type = ctypes.windll.kernel32.GetDriveTypeA(drive)
                    if drive_type == 2:  # REMOVABLE_DRIVE
                        drives.append(drive)
                except:
                    pass
            bitmask >>= 1
        return drives

    def copy_to_usb(self, source_path: str, usb_drive: str) -> bool:
        """Copy file to USB drive with user permission."""
        import shutil

        try:
            if not os.path.exists(source_path):
                return False

            # Ensure USB drive path exists
            dest_path = os.path.join(usb_drive, os.path.basename(source_path))
            shutil.copy2(source_path, dest_path)
            return True

        except Exception as e:
            self.logger.error(f"Failed to copy to USB: {str(e)}")
            return False

    def check_portable_mode(self) -> bool:
        """Check if running from portable location (USB)."""
        try:
            exe_path = sys.executable
            # Check if running from removable drive
            drive_type = ctypes.windll.kernel32.GetDriveTypeA(exe_path[:2] + "\\")
            return drive_type == 2  # REMOVABLE_DRIVE
        except:
            return False
