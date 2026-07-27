using PropertyOS.Application.Common.Security;

namespace PropertyOS.Application.Documents.Security;

/// <summary>
/// Module 10 Documents permission constants aliased to the central <see cref="PlatformPermissions"/> source of truth.
/// </summary>
public static class DocumentsPermissions
{
    public const string Upload = PlatformPermissions.DocumentsUpload;
    public const string ManageCategories = PlatformPermissions.DocumentsManageCategories;
    public const string ViewConfidential = PlatformPermissions.DocumentsViewConfidential;
}
