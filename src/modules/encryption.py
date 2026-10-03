"""
Encryption module for secure report storage and transmission.
Uses AES-256-GCM for encryption.
"""

import os
import hashlib
import secrets
from pathlib import Path
from typing import Optional, Tuple
from cryptography.hazmat.primitives.ciphers.aead import AESGCM


class SecureEncryption:
    """Handles encryption and decryption of sensitive data."""

    def __init__(self, key: Optional[bytes] = None):
        if key is None:
            # Generate a random key if not provided
            self.key = secrets.token_bytes(32)  # 256-bit key
        else:
            self.key = hashlib.sha256(key.encode()).digest()
        self.aesgcm = AESGCM(self.key)

    def encrypt_data(self, data: str, associated_data: Optional[bytes] = None) -> bytes:
        """Encrypt data and return nonce + ciphertext + tag."""
        nonce = secrets.token_bytes(12)  # 96-bit nonce for GCM
        if associated_data is None:
            associated_data = b""
        encrypted = self.aesgcm.encrypt(nonce, data.encode('utf-8'), associated_data)
        return nonce + encrypted  # Prepend nonce for decryption

    def decrypt_data(self, encrypted_data: bytes, associated_data: Optional[bytes] = None) -> str:
        """Decrypt data that was encrypted with encrypt_data()."""
        nonce = encrypted_data[:12]
        ciphertext = encrypted_data[12:]
        if associated_data is None:
            associated_data = b""
        decrypted = self.aesgcm.decrypt(nonce, ciphertext, associated_data)
        return decrypted.decode('utf-8')

    def generate_user_key(self, password: str, salt: Optional[bytes] = None) -> Tuple[bytes, bytes]:
        """Generate encryption key from user password using PBKDF2."""
        if salt is None:
            salt = secrets.token_bytes(16)
        # Use PBKDF2 for key derivation
        from cryptography.hazmat.primitives.kdf.pbkdf2 import PBKDF2HMAC
        from cryptography.hazmat.primitives import hashes
        kdf = PBKDF2HMAC(
            algorithm=hashes.SHA256(),
            length=32,
            salt=salt,
            iterations=480000,  # OWASP recommended for 2024
        )
        key = kdf.derive(password.encode('utf-8'))
        return key, salt

    def create_secure_report(self, report_data: str, password: str) -> Tuple[bytes, bytes]:
        """Create encrypted report with user-provided password."""
        key, salt = self.generate_user_key(password)
        encrypted = self.encrypt_data(report_data)
        return encrypted, salt

    def verify_password(self, password: str, salt: bytes, encrypted_data: bytes) -> bool:
        """Verify if password can decrypt the data."""
        try:
            key, _ = self.generate_user_key(password, salt)
            temp_aesgcm = AESGCM(key)
            nonce = encrypted_data[:12]
            ciphertext = encrypted_data[12:]
            temp_aesgcm.decrypt(nonce, ciphertext, None)
            return True
        except:
            return False
