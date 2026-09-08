namespace PropertyOS.Api.ContactRequests.Requests;
public sealed record CreateContactRequestRequest(string Name, string CompanyName, string PhoneNumber, int NumberOfBuildings, string? Notes);
