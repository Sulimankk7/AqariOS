using System;
using PropertyOS.Domain.Files.Entities;

namespace PropertyOS.Tests.Integration.Infrastructure;

public static class TestFileStorageFactory
{
    public static FileStorage CreateValidFileStorage(
        Guid companyId,
        Guid? uploadedBy = null,
        string filename = "test_document.pdf",
        string mimeType = "application/pdf",
        long sizeBytes = 1024,
        string? storageKey = null)
    {
        var key = storageKey ?? $"test/{companyId}/{Guid.NewGuid()}/{filename}";
        return FileStorage.Create(
            companyId: companyId,
            uploadedBy: uploadedBy,
            originalFilename: filename,
            mimeType: mimeType,
            sizeBytes: sizeBytes,
            storageKey: key,
            now: DateTimeOffset.UtcNow,
            createdBy: uploadedBy
        );
    }

    public static FileStorage CreateImageFileStorage(
        Guid companyId,
        Guid? uploadedBy = null,
        string filename = "test_image.png",
        string mimeType = "image/png",
        long sizeBytes = 2048,
        string? storageKey = null)
    {
        return CreateValidFileStorage(companyId, uploadedBy, filename, mimeType, sizeBytes, storageKey);
    }
}
