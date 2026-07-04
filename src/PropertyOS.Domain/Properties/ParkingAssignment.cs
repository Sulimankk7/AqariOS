using System;
using PropertyOS.Domain.Common;
using PropertyOS.Domain.Properties.Enums;

namespace PropertyOS.Domain.Properties;

public class ParkingAssignment : ISoftDeletable
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid ParkingSpotId { get; private set; }
    public Guid LeaseContractId { get; private set; }
    
    public DateOnly AssignedFrom { get; private set; }
    public DateOnly? AssignedTo { get; private set; }
    public ParkingAssignmentStatus Status { get; private set; }
    
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    
    public DateTimeOffset? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    private ParkingAssignment() { }

    public static ParkingAssignment Create(
        Guid companyId,
        Guid parkingSpotId,
        Guid leaseContractId,
        DateOnly assignedFrom,
        DateTimeOffset createdAt,
        Guid? createdBy,
        DateOnly? assignedTo = null,
        ParkingAssignmentStatus status = ParkingAssignmentStatus.Active)
    {
        if (companyId == Guid.Empty) throw new ArgumentException("Required", nameof(companyId));
        if (parkingSpotId == Guid.Empty) throw new ArgumentException("Required", nameof(parkingSpotId));
        if (leaseContractId == Guid.Empty) throw new ArgumentException("Required", nameof(leaseContractId));
        
        if (assignedTo.HasValue && assignedTo.Value <= assignedFrom)
            throw new ArgumentException("AssignedTo must be greater than AssignedFrom.");

        return new ParkingAssignment
        {
            Id = Guid.Empty,
            CompanyId = companyId,
            ParkingSpotId = parkingSpotId,
            LeaseContractId = leaseContractId,
            AssignedFrom = assignedFrom,
            AssignedTo = assignedTo,
            Status = status,
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        };
    }

    public void SoftDelete(DateTimeOffset deletedAt, Guid? deletedBy)
    {
        if (DeletedAt.HasValue) return;
        DeletedAt = deletedAt;
        DeletedBy = deletedBy;
        UpdatedAt = deletedAt;
        UpdatedBy = deletedBy;
    }
}
