namespace PropertyOS.Api.Models.Leasing;

public class UpdateTenantEmergencyContactRequest
{
    public string Name { get; set; } = string.Empty;
    public string RelationshipType { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
}
