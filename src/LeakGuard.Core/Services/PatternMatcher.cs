using System.Text.RegularExpressions;
using LeakGuard.Core.Models;
using LeakGuard.Core.Rules;

namespace LeakGuard.Core.Services;

/// <summary>
///Matcher для обнаружения паттернов в текстовом содержимом файлов.
/// </summary>
public class PatternMatcher
{
    private readonly List<(string Name, Regex Pattern, ScanCategory Category, RiskLevel Risk, double Confidence)> _patterns;

    public PatternMatcher()
    {
        _patterns = new List<(string, Regex, ScanCategory, RiskLevel, double)>
        {
            (nameof(PatternLibrary.Email), PatternLibrary.GetPattern(nameof(PatternLibrary.Email)), ScanCategory.PersonalData, RiskLevel.Low, 0.85),
            (nameof(PatternLibrary.PhoneRU), PatternLibrary.GetPattern(nameof(PatternLibrary.PhoneRU)), ScanCategory.PersonalData, RiskLevel.Low, 0.90),
            (nameof(PatternLibrary.PhoneGeneric), PatternLibrary.GetPattern(nameof(PatternLibrary.PhoneGeneric)), ScanCategory.PersonalData, RiskLevel.Low, 0.70),
            (nameof(PatternLibrary.INN), PatternLibrary.GetPattern(nameof(PatternLibrary.INN)), ScanCategory.SensitiveDocuments, RiskLevel.Medium, 0.50),
            (nameof(PatternLibrary.Snils), PatternLibrary.GetPattern(nameof(PatternLibrary.Snils)), ScanCategory.SensitiveDocuments, RiskLevel.Medium, 0.90),
            (nameof(PatternLibrary.PassportRU), PatternLibrary.GetPattern(nameof(PatternLibrary.PassportRU)), ScanCategory.SensitiveDocuments, RiskLevel.High, 0.75),
            (nameof(PatternLibrary.DateOfBirth), PatternLibrary.GetPattern(nameof(PatternLibrary.DateOfBirth)), ScanCategory.PersonalData, RiskLevel.Low, 0.60),
            (nameof(PatternLibrary.Url), PatternLibrary.GetPattern(nameof(PatternLibrary.Url)), ScanCategory.PersonalData, RiskLevel.Low, 0.50),
            (nameof(PatternLibrary.TelegramHandle), PatternLibrary.GetPattern(nameof(PatternLibrary.TelegramHandle)), ScanCategory.PersonalData, RiskLevel.Low, 0.80),
        };
    }

    /// <summary>
    /// Ищет паттерны в тексте. Возвращает найденные совпадения.
    /// </summary>
    public List<(string Name, string Match, ScanCategory Category, RiskLevel Risk, double Confidence)> FindPatterns(string text)
    {
        var results = new List<(string, string, ScanCategory, RiskLevel, double)>();

        foreach (var (name, pattern, category, risk, confidence) in _patterns)
        {
            var matches = pattern.Matches(text);
            foreach (Match match in matches)
            {
                results.Add((name, match.Value, category, risk, confidence));
            }
        }

        return results;
    }

    /// <summary>
    /// Проверяет имя файла на чувствительность.
    /// </summary>
    public (bool IsSensitive, string? Reason) CheckFileName(string fileName)
    {
        var lowerName = fileName.ToLowerInvariant();

        foreach (var keyword in PatternLibrary.SensitiveFileNames)
        {
            if (lowerName.Contains(keyword))
            {
                return (true, $"Имя файла содержит чувствительное слово: '{keyword}'");
            }
        }

        return (false, null);
    }

    /// <summary>
    /// Проверяет ключевые слова в содержимом.
    /// </summary>
    public List<string> FindSensitiveKeywords(string text)
    {
        var found = new List<string>();
        var lowerText = text.ToLowerInvariant();

        foreach (var keyword in PatternLibrary.SensitiveKeywords)
        {
            if (lowerText.Contains(keyword))
            {
                found.Add(keyword);
            }
        }

        return found;
    }
}
