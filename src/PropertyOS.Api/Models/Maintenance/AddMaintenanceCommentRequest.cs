namespace PropertyOS.Api.Models.Maintenance;

/// <summary>
/// Request model for adding a comment to a maintenance request.
/// </summary>
public record AddMaintenanceCommentRequest(
    string CommentText
);
