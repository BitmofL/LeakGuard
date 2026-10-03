using LeakGuard.Core.Services;
using Xunit;

namespace LeakGuard.Tests;

public class DataMaskerTests
{
    [Fact]
    public void MaskEmail_HidesLocalPart()
    {
        // Arrange
        var email = "ivan@example.com";

        // Act
        var result = DataMasker.MaskEmail(email);

        // Assert
        Assert.Contains("@example.com", result);
        Assert.StartsWith("i", result);
        Assert.Contains("*", result);
        Assert.NotEqual(email, result);
    }

    [Fact]
    public void MaskEmail_SingleCharLocal()
    {
        // Arrange
        var email = "a@example.com";

        // Act
        var result = DataMasker.MaskEmail(email);

        // Assert
        Assert.Equal("a@example.com", result);
    }

    [Fact]
    public void MaskPhone_HidesMiddleDigits()
    {
        // Arrange
        var phone = "+7(999)123-45-67";

        // Act
        var result = DataMasker.MaskPhone(phone);

        // Assert - маска скрывает средние цифры, последние 4 видимы
        Assert.Contains("*", result);
        Assert.NotEqual(phone, result);
    }

    [Fact]
    public void MaskDocumentNumber_HidesAllButLast4()
    {
        // Arrange
        var number = "4521 123456";

        // Act
        var result = DataMasker.MaskDocumentNumber(number);

        // Assert
        Assert.EndsWith("3456", result);
        Assert.Contains("*", result);
    }

    [Fact]
    public void MaskSecret_HidesMiddle()
    {
        // Arrange
        var secret = "sk-abc123def456ghi789";

        // Act
        var result = DataMasker.MaskSecret(secret);

        // Assert
        Assert.StartsWith("sk-", result);
        Assert.EndsWith("89", result);
        Assert.Contains("*", result);
    }

    [Fact]
    public void MaskSecret_ShortString_FullyMasked()
    {
        // Arrange
        var secret = "abc";

        // Act
        var result = DataMasker.MaskSecret(secret);

        // Assert
        Assert.Equal("***", result);
    }

    [Fact]
    public void MaskContent_HidesEmail()
    {
        // Arrange
        var content = "Contact: ivan@example.com for details";

        // Act
        var result = DataMasker.MaskContent(content);

        // Assert
        Assert.Contains("@example.com", result);
        Assert.Contains("*", result);
        Assert.DoesNotContain("ivan@", result);
    }

    [Fact]
    public void MaskContent_HidesApiKey()
    {
        // Arrange
        var content = "api_key = mysecretkey123";

        // Act
        var result = DataMasker.MaskContent(content);

        // Assert
        Assert.Contains("api_key", result);
        Assert.Contains("*", result);
        Assert.DoesNotContain("mysecretkey123", result);
    }

    [Fact]
    public void MaskPath_HidesSensitiveFolders()
    {
        // Arrange
        var path = @"C:\Users\Ivan\AppData\Local\.ssh\id_rsa";

        // Act
        var result = DataMasker.MaskPath(path);

        // Assert
        Assert.Contains("***", result);
        Assert.DoesNotContain("AppData", result);
    }

    [Fact]
    public void GetFingerprint_ProvidesConsistentHash()
    {
        // Arrange
        var input = "test-string";

        // Act
        var hash1 = DataMasker.GetFingerprint(input);
        var hash2 = DataMasker.GetFingerprint(input);

        // Assert
        Assert.Equal(hash1, hash2);
        Assert.NotEqual(input, hash1);
        Assert.Equal(16, hash1.Length);
    }

    [Fact]
    public void GetFingerprint_DifferentInputs_DifferentHashes()
    {
        // Arrange
        var input1 = "test-1";
        var input2 = "test-2";

        // Act
        var hash1 = DataMasker.GetFingerprint(input1);
        var hash2 = DataMasker.GetFingerprint(input2);

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void MaskContent_EmptyInput_ReturnsEmpty()
    {
        // Arrange
        string? content = null;

        // Act
        var result = DataMasker.MaskContent(content!);

        // Assert
        Assert.Equal(string.Empty, result);
    }
}
