using PropertyOS.Domain.PlatformAdministration.Enums;

namespace PropertyOS.Domain.PlatformAdministration;

public sealed class ContactRequest
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string CompanyName { get; private set; } = string.Empty;
    public string PhoneNumber { get; private set; } = string.Empty;
    public int NumberOfBuildings { get; private set; }
    public string? Notes { get; private set; }
    public ContactRequestStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private ContactRequest() { }

    public static ContactRequest Create(string name, string companyName, string phoneNumber, int numberOfBuildings, string? notes, DateTimeOffset now)
    {
        return new ContactRequest
        {
            Id = Guid.CreateVersion7(),
            Name = name.Trim(),
            CompanyName = companyName.Trim(),
            PhoneNumber = NormalizePhone(phoneNumber),
            NumberOfBuildings = numberOfBuildings,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            Status = ContactRequestStatus.New,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateStatus(ContactRequestStatus status, DateTimeOffset now)
    {
        Status = status;
        UpdatedAt = now;
    }

    private static string NormalizePhone(string phone) => phone.Trim().Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
}
