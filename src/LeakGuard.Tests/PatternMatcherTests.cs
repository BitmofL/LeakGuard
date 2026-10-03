using LeakGuard.Core.Rules;
using LeakGuard.Core.Services;
using Xunit;

namespace LeakGuard.Tests;

public class PatternMatcherTests
{
    private readonly PatternMatcher _matcher;

    public PatternMatcherTests()
    {
        _matcher = new PatternMatcher();
    }

    [Fact]
    public void FindPatterns_FindsEmail()
    {
        // Arrange
        var text = "My email is test.user@example.com";

        // Act
        var results = _matcher.FindPatterns(text);

        // Assert
        Assert.Contains(results, r => r.Match.Contains("test.user@example.com"));
    }

    [Fact]
    public void FindPatterns_FindsRussianPhone()
    {
        // Arrange
        var text = "Phone: +7(999)123-45-67";

        // Act
        var results = _matcher.FindPatterns(text);

        // Assert
        Assert.Contains(results, r => r.Match.Contains("+7"));
    }

    [Fact]
    public void FindPatterns_FindsMultipleEmails()
    {
        // Arrange
        var text = "Contact: a@test.com or b@test.com";

        // Act
        var results = _matcher.FindPatterns(text);

        // Assert
        Assert.Equal(2, results.Count(r => r.Name == nameof(PatternLibrary.Email)));
    }

    [Fact]
    public void FindPatterns_EmptyText_ReturnsEmpty()
    {
        // Arrange
        var text = string.Empty;

        // Act
        var results = _matcher.FindPatterns(text);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void CheckFileName_FindsSensitiveWord()
    {
        // Arrange
        var fileName = "passwords_backup.txt";

        // Act
        var result = _matcher.CheckFileName(fileName);

        // Assert
        Assert.True(result.IsSensitive);
        Assert.NotNull(result.Reason);
    }

    [Fact]
    public void CheckFileName_NoSensitiveWord()
    {
        // Arrange
        var fileName = "notes.txt";

        // Act
        var result = _matcher.CheckFileName(fileName);

        // Assert
        Assert.False(result.IsSensitive);
        Assert.Null(result.Reason);
    }

    [Fact]
    public void CheckFileName_CaseInsensitive()
    {
        // Arrange
        var fileName = "SECRET_config.json";

        // Act
        var result = _matcher.CheckFileName(fileName);

        // Assert
        Assert.True(result.IsSensitive);
    }

    [Fact]
    public void FindSensitiveKeywords_FindsKeywords()
    {
        // Arrange
        var text = "password = abc123 and secret key here";

        // Act
        var results = _matcher.FindSensitiveKeywords(text);

        // Assert
        Assert.Contains("password", results);
        Assert.Contains("secret", results);
    }

    [Fact]
    public void FindSensitiveKeywords_NoKeywords_ReturnsEmpty()
    {
        // Arrange
        var text = "This is a normal text with no sensitive words";

        // Act
        var results = _matcher.FindSensitiveKeywords(text);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void FindPatterns_FindsTelegramHandle()
    {
        // Arrange
        var text = "Contact me @username123";

        // Act
        var results = _matcher.FindPatterns(text);

        // Assert
        Assert.Contains(results, r => r.Match.Contains("@username123"));
    }

    [Fact]
    public void FindPatterns_FindsUrl()
    {
        // Arrange
        var text = "Visit https://example.com/page for more";

        // Act
        var results = _matcher.FindPatterns(text);

        // Assert
        Assert.Contains(results, r => r.Match.Contains("https://"));
    }
}
