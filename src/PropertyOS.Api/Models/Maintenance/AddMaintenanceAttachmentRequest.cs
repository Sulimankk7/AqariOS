using System;

namespace PropertyOS.Api.Models.Maintenance;

/// <summary>
/// Request model for attaching a file reference to a maintenance request.
/// The uploader is always the authenticated caller — never client-supplied
/// (actor attribution must not be spoofable at the HTTP boundary).
/// </summary>
public record AddMaintenanceAttachmentRequest(
    Guid? FileId = null,
    string? Description = null
);
