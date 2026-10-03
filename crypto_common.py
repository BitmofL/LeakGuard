#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Общий модуль шифрования для System Scanner v5.0
Используется scanner.py и decryptor.py
"""

import json
import os

# Пытаемся импортировать pycryptodome
try:
    from Crypto.Cipher import AES
    from Crypto.Protocol.KDF import PBKDF2
    HAS_CRYPTO = True
except ImportError:
    try:
        from Cryptodome.Cipher import AES
        from Cryptodome.Protocol.KDF import PBKDF2
        HAS_CRYPTO = True
    except ImportError:
        HAS_CRYPTO = False

# ====== Ключи шифрования v5 ======
SALT = b'Sc4nn3rV5_S4lt!2025#'
PWD = b'Sc4nn3rV5_Ultimate_K3y!@#'


def derive_key(password: bytes, salt: bytes) -> bytes:
    """ deriving key from password using PBKDF2 """
    return PBKDF2(password, salt, dkLen=32, count=200000)


def encrypt_report(data: dict, output_path: str = None) -> bytes | None:
    """
    Шифрует данные (JSON) и возвращает байты.
    Если output_path указан — сохраняет файл.
    Формат: nonce(16) + tag(16) + ciphertext
    """
    if not HAS_CRYPTO:
        return None

    key = derive_key(PWD, SALT)
    cipher = AES.new(key, AES.MODE_GCM)
    pt = json.dumps(data, ensure_ascii=False, indent=2).encode('utf-8')
    ct, tag = cipher.encrypt_and_digest(pt)
    encrypted = cipher.nonce + tag + ct

    if output_path:
        with open(output_path, 'wb') as f:
            f.write(encrypted)

    return encrypted


def decrypt_report(path: str) -> dict | None:
    """
    Расшифровывает .enc файл и возвращает JSON-словарь.
    """
    if not HAS_CRYPTO:
        return None

    try:
        with open(path, 'rb') as f:
            d = f.read()
        if len(d) < 32:
            return None

        nonce, tag, ct = d[:16], d[16:32], d[32:]
        key = derive_key(PWD, SALT)
        cipher = AES.new(key, AES.MODE_GCM, nonce=nonce)
        pt = cipher.decrypt_and_verify(ct, tag)
        return json.loads(pt.decode('utf-8'))
    except Exception:
        return None


# ====== XOR-шифрование для токенов ======
_XOR_KEY = b'\x4a\x7a\x2f\x8c\x1e\x3d\x6b\x9f'


def xor_decrypt(encrypted_hex: str) -> str:
    """Расшифровывает HEX-строку XOR-шифрования."""
    data = bytes.fromhex(encrypted_hex)
    result = bytearray(len(data))
    for i in range(len(data)):
        result[i] = data[i] ^ _XOR_KEY[i % len(_XOR_KEY)]
    return result.decode('utf-8')
