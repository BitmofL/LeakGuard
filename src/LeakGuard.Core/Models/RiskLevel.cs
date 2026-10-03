namespace LeakGuard.Core.Models;

/// <summary>
/// Уровень риска обнаружения.
/// </summary>
public enum RiskLevel
{
    /// <summary>Информация — не является риском</summary>
    Info = 0,

    /// <summary>Низкий риск — данные могут быть доступны</summary>
    Low = 1,

    /// <summary>Средний риск — требует проверки</summary>
    Medium = 2,

    /// <summary>Высокий риск — потенциальная утечка</summary>
    High = 3
}
