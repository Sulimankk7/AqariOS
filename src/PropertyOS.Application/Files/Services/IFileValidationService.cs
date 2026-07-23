using System.IO;

namespace PropertyOS.Application.Files.Services;

public interface IFileValidationService
{
    void ValidateFile(string filename, string mimeType, long sizeBytes, Stream headerStream);
    string SanitizeFilename(string filename);
    string GenerateStorageKey(System.Guid companyId, string moduleName, System.Guid entityId, System.Guid fileId, string sanitizedFilename);
}
