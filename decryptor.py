#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Decryptor GUI - Расшифровка отчётов System Scanner v4.0
Просто перетащите .enc файл в окно или нажмите "Открыть"
"""

import os, sys, tkinter as tk
from tkinter import filedialog, messagebox, scrolledtext
from datetime import datetime

from crypto_common import HAS_CRYPTO, decrypt_report as decrypt_report_fn

def format_report(data):
    lines = []
    lines.append("═══════════════════════════════════════════════════════════")
    lines.append("  SYSTEM SCANNER v4.0 - ПОЛНЫЙ ОТЧЕТ (расшифрован)")
    lines.append(f"  Дата расшифровки: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
    lines.append("═══════════════════════════════════════════════════════════\n")
    
    for section, d in data.items():
        lines.append(f"\n[+] {section.upper()}")
        lines.append("─" * 50)
        if isinstance(d, dict):
            for k, v in d.items():
                if isinstance(v, list):
                    lines.append(f"{k}:")
                    for i in v:
                        if isinstance(i, tuple): lines.append(f"  {i[0]} ({i[1]})")
                        else: lines.append(f"  {i}")
                elif isinstance(v, dict):
                    lines.append(f"{k}:")
                    for k2, v2 in v.items():
                        if isinstance(v2, list):
                            lines.append(f"  {k2}:")
                            for i in v2[:10]: lines.append(f"    {i}")
                        else: lines.append(f"  {k2}: {v2}")
                else: lines.append(f"{k}: {v}")
        elif isinstance(d, list):
            for i in d:
                if isinstance(i, tuple): lines.append(f"{i[0]} ({i[1]})")
                else: lines.append(f"{i}")
    return '\n'.join(lines)

class DecryptorApp:
    def __init__(self, root):
        self.root = root
        root.title("System Scanner Decryptor v1.0")
        root.geometry("900x700")
        root.configure(bg='#1e1e1e')
        
        # Try to set icon
        try: root.iconbitmap(default='')
        except: pass
        
        # Style
        self.fg = '#ffffff'
        self.bg = '#1e1e1e'
        self.btn_bg = '#2d2d2d'
        self.btn_hover = '#3d3d3d'
        self.accent = '#00b894'
        
        # Title
        title = tk.Label(root, text="🔐 System Scanner Decryptor", 
                        font=('Segoe UI', 16, 'bold'), 
                        fg=self.accent, bg=self.bg)
        title.pack(pady=10)
        
        subtitle = tk.Label(root, text="Расшифровка .enc отчётов System Scanner v4.0",
                          font=('Segoe UI', 10), fg='#888888', bg=self.bg)
        subtitle.pack(pady=(0, 10))
        
        # Buttons frame
        btn_frame = tk.Frame(root, bg=self.bg)
        btn_frame.pack(pady=5)
        
        self.open_btn = tk.Button(btn_frame, text="📂 Открыть .enc файл", 
                                 command=self.open_file,
                                 font=('Segoe UI', 11), bg=self.btn_bg, fg=self.fg,
                                 padx=20, pady=8, relief='flat', cursor='hand2',
                                 activebackground=self.btn_hover)
        self.open_btn.pack(side=tk.LEFT, padx=5)
        
        self.save_btn = tk.Button(btn_frame, text="💾 Сохранить как .txt", 
                                 command=self.save_file,
                                 font=('Segoe UI', 11), bg=self.btn_bg, fg=self.fg,
                                 padx=20, pady=8, relief='flat', cursor='hand2',
                                 activebackground=self.btn_hover, state='disabled')
        self.save_btn.pack(side=tk.LEFT, padx=5)
        
        # Status
        self.status = tk.Label(root, text="Выберите .enc файл для расшифровки",
                              font=('Segoe UI', 9), fg='#888888', bg=self.bg)
        self.status.pack(pady=5)
        
        # Text area
        text_frame = tk.Frame(root, bg='#252526')
        text_frame.pack(fill=tk.BOTH, expand=True, padx=10, pady=(0, 10))
        
        self.text_area = scrolledtext.ScrolledText(text_frame, 
                                                  font=('Consolas', 10), 
                                                  bg='#1e1e1e', fg='#d4d4d4',
                                                  insertbackground='#ffffff',
                                                  relief='flat', borderwidth=0,
                                                  padx=10, pady=10)
        self.text_area.pack(fill=tk.BOTH, expand=True)
        
        # Drop zone hint
        hint = tk.Label(root, text="💡 Перетащите .enc файл в окно для быстрой расшифровки",
                       font=('Segoe UI', 8), fg='#666666', bg=self.bg)
        hint.pack(pady=(0, 5))
        
        self.current_data = None
        self.current_path = None
        
        # Bind drag and drop (basic - just paste from clipboard)
        self.text_area.bind('<Control-v>', self.on_paste)
        
    def open_file(self):
        path = filedialog.askopenfilename(
            title="Выберите зашифрованный отчёт (.enc)",
            filetypes=[("Encrypted reports", "*.enc"), ("All files", "*.*")]
        )
        if path:
            self.process_file(path)
    
    def process_file(self, path):
        self.status.config(text=f"Расшифровываю: {os.path.basename(path)}...", fg='#ffd700')
        self.root.update()
        
        if not HAS_CRYPTO:
            messagebox.showerror("Ошибка", "Библиотека pycryptodome не найдена")
            self.status.config(text="❌ pycryptodome не найден", fg='#ff5555')
            return
        
        data = decrypt_report_fn(path)
        if data is None:
            messagebox.showerror("Ошибка", "Файл повреждён или ключ не подходит")
            self.status.config(text="❌ Ошибка расшифровки", fg='#ff5555')
            return
        
        self.current_data = data
        self.current_path = path
        self.text_area.delete('1.0', tk.END)
        self.text_area.insert('1.0', format_report(data))
        self.save_btn.config(state='normal')
        self.status.config(text=f"✅ Расшифрован: {os.path.basename(path)}", fg=self.accent)
    
    def save_file(self):
        if not self.current_data:
            messagebox.showwarning("Нет данных", "Сначала расшифруйте файл")
            return
        
        default_name = os.path.splitext(os.path.basename(self.current_path))[0] + '.txt'
        path = filedialog.asksaveasfilename(
            title="Сохранить отчёт",
            defaultextension=".txt",
            initialfile=default_name,
            filetypes=[("Text files", "*.txt"), ("All files", "*.*")]
        )
        if path:
            try:
                with open(path, 'w', encoding='utf-8') as f:
                    f.write(format_report(self.current_data))
                messagebox.showinfo("Успех", f"Отчёт сохранён:\n{path}")
                self.status.config(text=f"✅ Сохранён: {os.path.basename(path)}", fg=self.accent)
            except Exception as e:
                messagebox.showerror("Ошибка", f"Не удалось сохранить:\n{e}")
    
    def on_paste(self, event):
        # Simple paste handler - checks clipboard for file path
        try:
            clip = self.root.clipboard_get()
            if clip.endswith('.enc') and os.path.exists(clip):
                self.process_file(clip)
        except:
            pass

def main():
    root = tk.Tk()
    app = DecryptorApp(root)
    
    # Check command line args
    if len(sys.argv) > 1:
        path = sys.argv[1]
        if os.path.exists(path) and path.endswith('.enc'):
            app.process_file(path)
    
    root.mainloop()

if __name__ == '__main__':
    main()
