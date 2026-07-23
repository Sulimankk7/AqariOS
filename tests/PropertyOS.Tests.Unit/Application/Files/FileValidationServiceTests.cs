using System;
using System.IO;
using System.Text;
using FluentAssertions;
using PropertyOS.Application.Files.Options;
using PropertyOS.Application.Files.Services;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Files;

public class FileValidationServiceTests
{
    private readonly FileValidationService _validationService;

    public FileValidationServiceTests()
    {
        var options = new FileStorageOptions
        {
            MaxSizeBytes = 10_000_000,
            PreSignedUrlExpirationMinutes = 5
        };

        _validationService = new FileValidationService(options);
    }

    [Fact]
    public void ValidateFile_WithValidPdf_ShouldSucceed()
    {
        var pdfHeader = Encoding.ASCII.GetBytes("%PDF-1.4 header content");
        using var stream = new MemoryStream(pdfHeader);

        Action act = () => _validationService.ValidateFile("deed.pdf", "application/pdf", 1024, stream);

        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateFile_WithDoubleExtension_ShouldThrowArgumentException()
    {
        var content = Encoding.ASCII.GetBytes("%PDF-1.4");
        using var stream = new MemoryStream(content);

        Action act = () => _validationService.ValidateFile("document.pdf.exe", "application/pdf", 1024, stream);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Double extension attacks are forbidden*");
    }

    [Fact]
    public void ValidateFile_WithExecutableExtension_ShouldThrowArgumentException()
    {
        var content = Encoding.ASCII.GetBytes("binary content");
        using var stream = new MemoryStream(content);

        Action act = () => _validationService.ValidateFile("script.sh", "application/x-sh", 1024, stream);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*forbidden*");
    }

    [Fact]
    public void SanitizeFilename_WithDirectoryTraversal_ShouldRemovePathChars()
    {
        var sanitized = _validationService.SanitizeFilename("../../etc/passwd");

        sanitized.Should().Be("passwd");
    }
}
