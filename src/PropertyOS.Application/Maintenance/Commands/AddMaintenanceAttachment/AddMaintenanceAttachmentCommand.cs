using System;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Maintenance.Commands.AddMaintenanceAttachment;

public record AddMaintenanceAttachmentCommand(
    Guid RequestId,
    Guid? FileId = null,
    string? Description = null,
    /// <summary>
    /// Who performed the upload. Defaults to the current user when null,
    /// but can be explicitly set for tenant-portal uploads, background jobs,
    /// or bulk import scenarios.
    /// </summary>
    Guid? UploadedBy = null
) : ICommand<Guid>;
