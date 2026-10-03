# LeakGuard — Полный обзор проекта

## Что создано

### Архитектура (MVVM, 2 проекта)

```
Win-scanner/
├── src/
│   ├── LeakGuard.Core/          # Бизнес-логика (класслбиблиотека)
│   │   ├── Models/
│   │   │   ├── RiskLevel.cs     # 4 уровня: Info, Low, Medium, High
│   │   │   ├── ScanCategory.cs  # 11 категорий обнаружения
│   │   │   ├── ScanResult.cs    # Результат сканирования
│   │   │   ├── ScanScope.cs     # Область сканирования
│   │   │   └── AppSettings.cs   # Все настройки приложения
│   │   ├── Services/
│   │   │   ├── BaseViewModel.cs       # Базовый класс ViewModel
│   │   │   ├── RelayCommand.cs        # ICommand реализация
│   │   │   ├── PatternMatcher.cs      # Regex-паттерны ПДН
│   │   │   ├── DataMasker.cs          # Маскирование email, телефонов, ключей
│   │   │   ├── FileScanner.cs         # Сканер файлов и папок
│   │   │   ├── ReportExporter.cs      # Экспорт в HTML/JSON/PDF
│   │   │   ├── ReportStorage.cs       # Хранение + DPAPI шифрование
│   │   │   ├── ConsentManager.cs      # Запрос согласия пользователя
│   │   │   ├── StartupManager.cs      # Автозапуск через schtasks
│   │   │   └── LoggingService.cs      # Локальные логи без ПДН
│   │   └── Rules/
│   │       └── PatternLibrary.cs      # Regex-шаблоны + ключевые слова
│   │
│   ├── LeakGuard.App/           # WPF-приложение (MVVM)
│   │   ├── App.xaml / .cs       # Точка входа
│   │   ├── Views/
│   │   │   ├── MainWindow.xaml      # Главное окно с Frame-навигацией
│   │   │   ├── DashboardView.xaml   # Главный экран (4 карточки)
│   │   │   ├── ScanView.xaml        # Страница сканирования с прогрессом
│   │   │   ├── ResultsView.xaml     # Результаты с фильтрами
│   │   │   ├── SettingsView.xaml    # Настройки (автозапуск, отчёты, удаление)
│   │   │   ├── AboutView.xaml       # О программе (BitmofL, GitHub)
│   │   │   └── GuideView.xaml       # Руководство пользователя
│   │   ├── Styles/
│   │   │   ├── Theme.xaml       # Тёмная тема (графит + зелёный/янтарный/красный)
│   │   │   └── Controls.xaml    # Стили кнопок, карточек, анимации
│   │   └── Converters/
│   │       └── RiskConverters.cs # RiskLevel → цвет
│   │
│   └── LeakGuard.Tests/         # Unit-тесты (xUnit)
│       ├── DataMaskerTests.cs       # 12 тестов маскирования
│       ├── PatternMatcherTests.cs   # 11 тестов паттернов
│       └── ReportExporterTests.cs   # 5 тестов экспорта
│
├── .github/workflows/ci.yml     # CI/CD: build, test, publish, release
├── build.ps1                    # Скрипт сборки
├── global.json                  # Требование .NET 8 SDK
├── .editorconfig                # Форматирование кода
├── .gitignore
├── Win-scanner.sln              # Solution
├── README.md
├── LICENSE (MIT)
├── PRIVACY.md
├── USER_AGREEMENT.md
└── RELEASE_CHECKLIST.md
```

## Ключевые файлы и их назначение

| Файл | Назначение |
|------|-----------|
| `FileScanner.cs` | Ядро сканирования: рекурсивный обход папок, проверка имён файлов, сканирование содержимого текстовых файлов, извлечение EXIF из изображений |
| `PatternMatcher.cs` | Regex-матчинг: email, телефоны (RU + generic), ИНН, СНИЛС, паспорт, дата рождения, URL, Telegram-никнеймы |
| `DataMasker.cs` | Безопасное отображение: маскирование email, телефона, номера документа, токена, пути, содержимого файла, генерация хеш-отпечатка |
| `ReportExporter.cs` | Экспорт: HTML-отчёт с цветовой кодировкой рисков, JSON со структурой, поддержка DPAPI-шифрования |
| `ConsentManager.cs` | Управление согласием: диалоги для UAC, автозапуска, экспорта, интернет-проверок |
| `StartupManager.cs` | Автозапуск: schtasks Create/Delete, проверка статуса, инструкции для ручного удаления |
| `ReportStorage.cs` | Хранение: сохранение/загрузка настроек, экспорт отчётов, автоудаление по таймеру, DPAPI-шифрование |

## Цветовая схема

| Элемент | Цвет | HEX |
|---------|------|-----|
| Фон основной | Графитовый | #1a1b26 |
| Фон карточки | Тёмно-серый | #1e2030 |
| Акцент зелёный | Безопасный | #9ece6a |
| Акцент янтарный | Предупреждение | #e0af68 |
| Акцент красный | Критический | #f7768e |
| Акцент синий | Информация | #7aa2f7 |
| Акцент фиолетовый | Доп. | #bb9af7 |
| Текст основной | Светло-серый | #c0caf5 |
| Текст вторичный | Серый | #787c99 |

## Как запустить

### 1. Установка .NET 8 SDK

```powershell
# Вариант 1: с сайта
# https://dotnet.microsoft.com/download/dotnet/8.0

# Вариант 2: через Chocolatey (если установлен)
choco install dotnet-sdk -y

# Вариант 3: через winget
winget install Microsoft.DotNet.SDK.8
```

### 2. Сборка и запуск

```powershell
# Перейти в проект
cd D:\Documents\Win-scanner

# Собрать всё
dotnet build Win-scanner.sln --configuration Debug

# Запустить
dotnet run --project src\LeakGuard.App\LeakGuard.App.csproj
```

### 3. Portable-версия

```powershell
# Самодостаточная папка для флешки
dotnet publish src\LeakGuard.App\LeakGuard.App.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -o .\publish\

# Запуск с флешки
D:\LeakGuard.exe
```

### 4. Тесты

```powershell
dotnet test src\LeakGuard.Tests\LeakGuard.Tests.csproj
```

### 5. Тестирование на чистой VM

1. Создать VM (Hyper-V / VirtualBox) с Windows 10/11 x64
2. Установить .NET 8 Runtime (для portable) или SDK (для сборки)
3. Скопировать LeakGuard.exe
4. Запустить — без прав администратора
5. Проверить:
   - Нет сетевых подключений (Process Monitor / Wireshark)
   - Нет UAC при запуске
   - Настройки в `%APPDATA%\LeakGuard`
   - Логи в `%APPDATA%\LeakGuard\Logs\`

## Что работает в MVP

| Функция | Статус |
|---------|--------|
| Быстрое сканирование (Desktop, Documents, Downloads, Pictures, Startup) | ✅ |
| Полное сканирование (с предупреждением) | ✅ |
| Сканирование содержимого текстовых файлов (regex) | ✅ |
| EXIF/GPS из изображений | ✅ |
| Обнаружение чувствительных имён файлов | ✅ |
| Обнаружение чувствительных расширений (.env, .pem) | ✅ |
| Обнаружение архивов и бэкапов | ✅ |
| Маскирование email, телефона, документов, ключей | ✅ |
| Экспорт в HTML (цветной отчёт) | ✅ |
| Экспорт в JSON | ✅ |
| DPAPI шифрование отчётов | ✅ |
| Автоудаление отчётов по таймеру | ✅ |
| Запрос согласия (ConsentManager) | ✅ |
| Автозапуск через schtasks | ✅ |
| Настройки (сохранение/загрузка) | ✅ |
| Фильтры результатов (риск, категория, поиск) | ✅ |
| Игнорирование результатов | ✅ |
| Локальное логирование без ПДН | ✅ |
| Страница «О программе» | ✅ |
| Руководство пользователя | ✅ |
| Unit-тесты (28 тестов) | ✅ |
| CI/CD (GitHub Actions) | ✅ |

## Что добавить в следующих версиях

| Функция | Приоритет |
|---------|-----------|
| Онлайн-проверка утечек (HaveIBeenPwned API) | Высокий |
| Проверка общих папок Windows (net share) | Высокий |
| Проверка расширений браузеров | Средний |
| PDF-экспорт (PdfSharpCore) | Средний |
| Распаковка архивов (с явным согласием) | Средний |
| Расписание сканирования (еженедельно/ежемесячно) | Средний |
| Установщик (WiX / Inno Setup) | Низкий |
| Подписание кода (Code Signing Certificate) | Низкий |
| Мультиязычность (i18n) | Низкий |
| Тёмная/светлая тема переключатель | Низкий |

## Структура данных ScanResult

```csharp
public class ScanResult
{
    public string Id;               // GUID
    public string FilePath;         // Полный путь
    public ScanCategory Category;   // Категория (11 видов)
    public RiskLevel RiskLevel;     // Info / Low / Medium / High
    public string Reason;           // Причина обнаружения
    public double Confidence;       // 0.0 — 1.0
    public string Recommendation;   // Безопасная рекомендация
    public string? MaskedExample;   // Замаскированный пример содержимого
    public string? TechnicalInfo;   // Для отчёта специалиста
    public string? FileType;        // Расширение
    public DateTime? FileModifiedDate;
    public long? FileSize;
    public bool IsIgnored;
    public bool IsWhitelisted;
}
```

## Безопасность по умолчанию

| Параметр | Значение |
|----------|---------|
| Интернет | ❌ Отключён |
| Автозапуск | ❌ Отключён |
| Права администратора | ❌ Не требуются |
| Сбор данных | ❌ Не собирается |
| Отправка в облако | ❌ Не отправляется |
| Логи с ПДН | ❌ Не содержат |
| Шифрование отчётов | ⚙ Опционально (DPAPI) |
| Маскирование ПДН | ✅ По умолчанию |

## Контакты

Разработчик: **BitmofL**  
GitHub: https://github.com/BitmofL  
По вопросам: Discussions на странице продукта
