namespace PropertyOS.Api.Models.Maintenance;

/// <summary>
/// Request model for editing an existing maintenance request comment.
/// </summary>
public record EditMaintenanceCommentRequest(
    string NewText
);
