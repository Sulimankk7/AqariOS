using System;

namespace PropertyOS.Application.Properties.ParkingAssignments.Queries.Common;

public record ParkingAssignmentDto(
    Guid AssignmentId,
    Guid ParkingSpotId,
    string ParkingSpotCode,
    string ParkingType,
    string Location,
    Guid LeaseContractId,
    Guid TenantId,
    string TenantName,
    DateOnly StartDate,
    DateOnly? EndDate,
    string Status
);
