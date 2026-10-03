# LeakGuard — Скрипт сборки
# Запуск: .\build.ps1

param(
    [string]$Configuration = "Release",
    [switch]$PublishPortable,
    [switch]$RunTests,
    [switch]$Clean
)

$ErrorActionPreference = "Stop"
$SolutionPath = "Win-scanner.sln"
$CoreProject = "src\LeakGuard.Core\LeakGuard.Core.csproj"
$AppProject = "src\LeakGuard.App\LeakGuard.App.csproj"
$TestProject = "src\LeakGuard.Tests\LeakGuard.Tests.csproj"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  LeakGuard Build Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Проверка .NET SDK
Write-Host "[1/6] Проверка .NET SDK..." -ForegroundColor Yellow
$dotnetVersion = dotnet --version 2>$null
if (-not $dotnetVersion) {
    Write-Host "ERROR: .NET SDK не найден!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Установите .NET 8 SDK:" -ForegroundColor Yellow
    Write-Host "  https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Или используйте Chocolatey:" -ForegroundColor Yellow
    Write-Host "  choco install dotnet-sdk -y" -ForegroundColor Yellow
    Write-Host ""
    exit 1
}
Write-Host "  .NET SDK version: $dotnetVersion" -ForegroundColor Green

# Очистка
if ($Clean) {
    Write-Host ""
    Write-Host "[2/6] Очистка..." -ForegroundColor Yellow
    dotnet clean $SolutionPath
    Write-Host "  Очищено." -ForegroundColor Green
}

# Восстановление пакетов
Write-Host ""
Write-Host "[2/6] Восстановление пакетов..." -ForegroundColor Yellow
dotnet restore $SolutionPath
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Не удалось восстановить пакеты!" -ForegroundColor Red
    exit 1
}
Write-Host "  Пакеты восстановлены." -ForegroundColor Green

# Сборка
Write-Host ""
Write-Host "[3/6] Сборка ($Configuration)..." -ForegroundColor Yellow
dotnet build $SolutionPath --configuration $Configuration --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Сборка не удалась!" -ForegroundColor Red
    exit 1
}
Write-Host "  Сборка завершена успешно." -ForegroundColor Green

# Тесты
if ($RunTests) {
    Write-Host ""
    Write-Host "[4/6] Запуск тестов..." -ForegroundColor Yellow
    dotnet test $TestProject --configuration $Configuration --no-build --verbosity normal
    if ($LASTEXITCODE -ne 0) {
        Write-Host "WARNING: Некоторые тесты не прошли!" -ForegroundColor Red
    } else {
        Write-Host "  Все тесты прошли успешно." -ForegroundColor Green
    }
}

# Portable-версия
if ($PublishPortable) {
    Write-Host ""
    Write-Host "[5/6] Создание portable-версии..." -ForegroundColor Yellow
    $PublishDir = ".\publish\leakguard-portable"
    if (Test-Path $PublishDir) { Remove-Item $PublishDir -Recurse -Force }
    
    dotnet publish $AppProject `
        -c $Configuration `
        -r win-x64 `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:PublishReadyToRun=false `
        -p:IncludeAllContentForSelfExtract=true `
        -o $PublishDir

    if ($LASTEXITCODE -eq 0) {
        Write-Host "  Portable-версия создана: $PublishDir" -ForegroundColor Green
        Write-Host ""
        Write-Host "  Файлы:" -ForegroundColor Yellow
        Get-ChildItem $PublishDir | ForEach-Object {
            Write-Host "    $($_.Name) ($([math]::Round($_.Length/1MB,2)) MB)" -ForegroundColor Gray
        }
    } else {
        Write-Host "ERROR: Не удалось создать portable-версию!" -ForegroundColor Red
    }
}

# Хеши
if ($PublishPortable) {
    Write-Host ""
    Write-Host "[6/6] Генерация хешей..." -ForegroundColor Yellow
    $Hashes = @()
    Get-ChildItem "$PublishDir\*.exe" | ForEach-Object {
        $hash = Get-FileHash $_ -Algorithm SHA256
        $Hashes += "$($hash.Hash)  $($($_.Name))"
    }
    $Hashes | Out-File "$PublishDir\hashes.txt" -Encoding utf8
    Write-Host "  Хеши сохранены в hashes.txt" -ForegroundColor Green
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Сборка завершена!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Запуск: dotnet run --project $AppProject" -ForegroundColor Yellow
Write-Host "Portable: .\publish\leakguard-portable\LeakGuard.exe" -ForegroundColor Yellow
