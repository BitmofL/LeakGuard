using LeakGuard.Core.Models;
using LeakGuard.Core.Services;
using Xunit;

namespace LeakGuard.Tests;

public class ReportExporterTests
{
    [Fact]
    public async Task ExportHtml_GeneratesValidHtml()
    {
        // Arrange
        var settings = new AppSettings();
        var exporter = new ReportExporter(settings);
        var results = new List<ScanResult>
        {
            new()
            {
                FilePath = @"C:\Users\Test\Documents\report.pdf",
                Category = ScanCategory.PersonalData,
                RiskLevel = RiskLevel.Low,
                Reason = "Found email pattern",
                Confidence = 0.85,
                Recommendation = "Check this file",
                FileType = ".pdf"
            }
        };

        var outputPath = Path.Combine(Path.GetTempPath(), "test_report.html");

        // Act
        await exporter.ExportAsync(results, ReportFormat.Html, outputPath);

        // Assert
        Assert.True(File.Exists(outputPath));
        var content = await File.ReadAllTextAsync(outputPath);
        Assert.Contains("LeakGuard", content);
        Assert.Contains("Found email pattern", content);

        // Cleanup
        File.Delete(outputPath);
    }

    [Fact]
    public async Task ExportJson_GeneratesValidJson()
    {
        // Arrange
        var settings = new AppSettings
        {
            IncludeFilePathsInReport = true,
            IncludeMaskedExamplesInReport = true,
            IncludeTechnicalInfoInReport = false
        };
        var exporter = new ReportExporter(settings);
        var results = new List<ScanResult>
        {
            new()
            {
                FilePath = @"C:\Users\Test\file.txt",
                Category = ScanCategory.Documents,
                RiskLevel = RiskLevel.Medium,
                Reason = "Sensitive keyword found",
                Confidence = 0.7,
                Recommendation = "Review this file",
                MaskedExample = "p*** example",
                TechnicalInfo = "tech info"
            }
        };

        var outputPath = Path.Combine(Path.GetTempPath(), "test_report.json");

        // Act
        await exporter.ExportAsync(results, ReportFormat.Json, outputPath);

        // Assert
        Assert.True(File.Exists(outputPath));
        var content = await File.ReadAllTextAsync(outputPath);
        Assert.Contains("LeakGuard", content);
        Assert.Contains("Sensitive keyword found", content);

        // Cleanup
        File.Delete(outputPath);
    }

    [Fact]
    public async Task ExportHtml_MasksPaths_WhenDisabled()
    {
        // Arrange
        var settings = new AppSettings
        {
            IncludeFilePathsInReport = false
        };
        var exporter = new ReportExporter(settings);
        var results = new List<ScanResult>
        {
            new()
            {
                FilePath = @"C:\Users\Ivan\AppData\.ssh\id_rsa",
                Category = ScanCategory.DeveloperFiles,
                RiskLevel = RiskLevel.High,
                Reason = "Sensitive file extension",
                Confidence = 0.95,
                Recommendation = "Secure this file",
                FileType = ".rsa"
            }
        };

        var outputPath = Path.Combine(Path.GetTempPath(), "test_report_masked.html");

        // Act
        await exporter.ExportAsync(results, ReportFormat.Html, outputPath);

        // Assert
        var content = await File.ReadAllTextAsync(outputPath);
        Assert.DoesNotContain("AppData", content);
        Assert.DoesNotContain(".ssh", content);
        Assert.Contains("***", content);

        // Cleanup
        File.Delete(outputPath);
    }

    [Fact]
    public async Task ExportHtml_EmptyResults_GeneratesSuccessMessage()
    {
        // Arrange
        var settings = new AppSettings();
        var exporter = new ReportExporter(settings);
        var results = new List<ScanResult>();

        var outputPath = Path.Combine(Path.GetTempPath(), "test_report_empty.html");

        // Act
        await exporter.ExportAsync(results, ReportFormat.Html, outputPath);

        // Assert
        var content = await File.ReadAllTextAsync(outputPath);
        Assert.Contains("Проблем не обнаружено", content);

        // Cleanup
        File.Delete(outputPath);
    }

    [Fact]
    public async Task ExportHtml_HtmlEscapesContent()
    {
        // Arrange
        var settings = new AppSettings();
        var exporter = new ReportExporter(settings);
        var results = new List<ScanResult>
        {
            new()
            {
                FilePath = @"C:\test<script>alert('xss')</script>.txt",
                Category = ScanCategory.PersonalData,
                RiskLevel = RiskLevel.Low,
                Reason = "Found <script> tag",
                Confidence = 0.5,
                Recommendation = "Check <this> & 'that'",
                FileType = ".txt"
            }
        };

        var outputPath = Path.Combine(Path.GetTempPath(), "test_report_xss.html");

        // Act
        await exporter.ExportAsync(results, ReportFormat.Html, outputPath);

        // Assert
        var content = await File.ReadAllTextAsync(outputPath);
        Assert.DoesNotContain("<script>", content);
        Assert.Contains("&lt;script&gt;", content);
        Assert.Contains("&amp;", content);

        // Cleanup
        File.Delete(outputPath);
    }
}
