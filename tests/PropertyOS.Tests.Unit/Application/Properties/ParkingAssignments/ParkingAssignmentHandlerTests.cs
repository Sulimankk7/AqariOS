using FluentAssertions;
using NSubstitute;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Leasing;
using PropertyOS.Application.Properties;
using PropertyOS.Application.Properties.ParkingAssignments.Commands.AssignParkingSpot;
using PropertyOS.Application.Properties.ParkingAssignments.Commands.EndParkingAssignment;
using PropertyOS.Application.Properties.ParkingAssignments.Queries.GetAssignedParkingByLease;
using PropertyOS.Application.Properties.ParkingAssignments.Queries.GetCurrentAssignmentBySpot;
using PropertyOS.Domain.Leasing;
using PropertyOS.Domain.Leasing.Enums;
using PropertyOS.Domain.Properties;
using PropertyOS.Domain.Properties.Enums;

namespace PropertyOS.Tests.Unit.Application.Properties.ParkingAssignments;

public class ParkingAssignmentHandlerTests
{
    private readonly IParkingAssignmentRepository _assignments = Substitute.For<IParkingAssignmentRepository>();
    private readonly IParkingSpotRepository _spots = Substitute.For<IParkingSpotRepository>();
    private readonly ILeaseContractRepository _leases = Substitute.For<ILeaseContractRepository>();
    private readonly ITenantRepository _tenants = Substitute.For<ITenantRepository>();
    private readonly IBuildingRepository _buildings = Substitute.For<IBuildingRepository>();
    private readonly IApartmentRepository _apartments = Substitute.For<IApartmentRepository>();
    private readonly ITenantContext _scope = Substitute.For<ITenantContext>();
    private readonly ICurrentUserContext _user = Substitute.For<ICurrentUserContext>();
    private readonly IBusinessClock _clock = Substitute.For<IBusinessClock>();
    private readonly Guid _companyId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly DateTimeOffset _now = new(2026, 9, 3, 12, 0, 0, TimeSpan.Zero);
    private readonly DateOnly _today = new(2026, 9, 3);
    private readonly ParkingSpot _spot;
    private readonly LeaseContract _lease;
    private readonly Tenant _tenant;
    private readonly Building _building;
    private readonly Apartment _apartment;
    private readonly AssignParkingSpotCommandHandler _handler;

    public ParkingAssignmentHandlerTests()
    {
        _scope.CompanyId.Returns(_companyId);
        _user.UserId.Returns(_userId);
        _clock.UtcNow.Returns(_now);
        _clock.GetJordanBusinessDate(_now).Returns(_today);
        _building = Building.Create(_companyId, "Building", 2, _now, null, id: Guid.NewGuid());
        _apartment = Apartment.Create(_companyId, _building.Id, Guid.NewGuid(), "101", 80, _now, null);
        Set(_apartment, nameof(Apartment.Id), Guid.NewGuid());
        _tenant = Tenant.Create(_companyId, "Tenant Name", "1234567890", "+962790123456", _now, null);
        _lease = LeaseContract.Create(_companyId, _building.Id, _apartment.Id, _tenant.Id, "LC-1",
            _today.AddDays(-1), _today.AddYears(1), 100, PaymentFrequency.Monthly, 1, _now, null,
            status: ContractStatus.Active);
        _spot = ParkingSpot.Create(_companyId, _building.Id, "P-01", _now, null);
        Set(_spot, nameof(ParkingSpot.Id), Guid.NewGuid());
        _spots.GetByIdForUpdateAsync(_spot.Id, _companyId, default).Returns(_spot);
        _spots.GetByIdAsync(_spot.Id, default).Returns(_spot);
        _leases.GetByIdForUpdateAsync(_lease.Id, _companyId, default).Returns(_lease);
        _leases.GetByIdAsync(_lease.Id, default).Returns(_lease);
        _tenants.GetByIdAsync(_tenant.Id, default).Returns(_tenant);
        _buildings.GetByIdAsync(_building.Id, default).Returns(_building);
        _apartments.GetByIdAsync(_apartment.Id, _companyId, default).Returns(_apartment);
        _handler = new(_assignments, _spots, _leases, _tenants, _buildings, _apartments, _scope, _user, _clock);
    }

    private static void Set<T>(T entity, string property, object value) => typeof(T).GetProperty(property)!.SetValue(entity, value);
    private Task<Guid> Assign() => _handler.Handle(new(_spot.Id, _lease.Id), default);

    [Fact]
    public async Task Assign_DerivesScopeAndTenantFromLease_AndUsesBusinessDate()
    {
        var id = await Assign();
        id.Should().NotBeEmpty();
        await _assignments.Received(1).AddAsync(Arg.Is<ParkingAssignment>(a => a.Id == id
            && a.CompanyId == _companyId && a.ParkingSpotId == _spot.Id && a.LeaseContractId == _lease.Id
            && a.AssignedFrom == _today && a.AssignedTo == null && a.Status == ParkingAssignmentStatus.Active
            && a.CreatedBy == _userId && a.CreatedAt == _now), default);
    }

    [Theory]
    [InlineData("spot")]
    [InlineData("lease")]
    [InlineData("tenant")]
    [InlineData("building")]
    [InlineData("apartment")]
    public async Task Assign_MissingRelationship_IsNotFound(string target)
    {
        switch (target)
        {
            case "spot": _spots.GetByIdForUpdateAsync(_spot.Id, _companyId, default).Returns((ParkingSpot?)null); break;
            case "lease": _leases.GetByIdForUpdateAsync(_lease.Id, _companyId, default).Returns((LeaseContract?)null); break;
            case "tenant": _tenants.GetByIdAsync(_tenant.Id, default).Returns((Tenant?)null); break;
            case "building": _buildings.GetByIdAsync(_building.Id, default).Returns((Building?)null); break;
            case "apartment": _apartments.GetByIdAsync(_apartment.Id, _companyId, default).Returns((Apartment?)null); break;
        }
        await Assert.ThrowsAsync<NotFoundException>(Assign);
        await _assignments.DidNotReceive().AddAsync(Arg.Any<ParkingAssignment>(), default);
    }

    [Theory]
    [InlineData("spot")]
    [InlineData("lease")]
    [InlineData("tenant")]
    [InlineData("building")]
    [InlineData("apartment")]
    public async Task Assign_ForeignCompanyRelationship_IsNotFound(string target)
    {
        var other = Guid.NewGuid();
        switch (target)
        {
            case "spot": Set(_spot, "CompanyId", other); break;
            case "lease": Set(_lease, "CompanyId", other); break;
            case "tenant": Set(_tenant, "CompanyId", other); break;
            case "building": Set(_building, "CompanyId", other); break;
            case "apartment": Set(_apartment, "CompanyId", other); break;
        }
        await Assert.ThrowsAsync<NotFoundException>(Assign);
        await _assignments.DidNotReceive().AddAsync(Arg.Any<ParkingAssignment>(), default);
    }

    [Theory]
    [InlineData(ContractStatus.Draft)]
    [InlineData(ContractStatus.PendingSignature)]
    [InlineData(ContractStatus.Expired)]
    [InlineData(ContractStatus.Terminated)]
    [InlineData(ContractStatus.Cancelled)]
    [InlineData(ContractStatus.Renewed)]
    [InlineData(ContractStatus.Superseded)]
    public async Task Assign_NonActiveLease_IsRejected(ContractStatus status)
    {
        Set(_lease, "Status", status);
        (await Assert.ThrowsAsync<BusinessRuleException>(Assign)).Code.Should().Be("LEASE_NOT_ACTIVE");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Assign_OutsideLeaseTerm_IsRejected(bool future)
    {
        Set(_lease, future ? "StartDate" : "EndDate", future ? _today.AddDays(1) : _today);
        (await Assert.ThrowsAsync<BusinessRuleException>(Assign)).Code.Should().Be("LEASE_NOT_ACTIVE");
    }

    [Fact]
    public async Task Assign_InactiveSpot_IsRejected()
    {
        Set(_spot, "IsActive", false);
        (await Assert.ThrowsAsync<BusinessRuleException>(Assign)).Code.Should().Be("PARKING_SPOT_INACTIVE");
    }

    [Fact]
    public async Task Assign_CrossBuilding_IsRejected()
    {
        Set(_spot, "BuildingId", Guid.NewGuid());
        (await Assert.ThrowsAsync<BusinessRuleException>(Assign)).Code.Should().Be("PARKING_BUILDING_MISMATCH");
    }

    [Fact]
    public async Task Assign_AlreadyAssigned_IsConflict()
    {
        _assignments.HasActiveAssignmentAsync(_spot.Id, _companyId, default).Returns(true);
        (await Assert.ThrowsAsync<ConflictException>(Assign)).Code.Should().Be("PARKING_ALREADY_ASSIGNED");
        await _assignments.DidNotReceive().AddAsync(Arg.Any<ParkingAssignment>(), default);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Commands_RequireCompanyAndUser(bool missingCompany)
    {
        if (missingCompany) _scope.CompanyId.Returns((Guid?)null);
        else _user.UserId.Returns((Guid?)null);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(Assign);
        var end = new EndParkingAssignmentCommandHandler(_assignments, _scope, _user, _clock);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => end.Handle(new(Guid.NewGuid()), default));
    }

    [Fact]
    public async Task End_SameDay_PreservesHistoryAndIsIdempotent()
    {
        var a = ParkingAssignment.Create(_companyId, _spot.Id, _lease.Id, _today, _now, _userId);
        _assignments.GetByIdForUpdateAsync(a.Id, _companyId, default).Returns(a);
        var handler = new EndParkingAssignmentCommandHandler(_assignments, _scope, _user, _clock);
        await handler.Handle(new(a.Id), default);
        _clock.UtcNow.Returns(_now.AddDays(1));
        await handler.Handle(new(a.Id), default);
        a.Status.Should().Be(ParkingAssignmentStatus.Ended);
        a.AssignedFrom.Should().Be(_today);
        a.AssignedTo.Should().Be(_today);
        a.CreatedAt.Should().Be(_now);
        a.UpdatedAt.Should().Be(_now);
        a.UpdatedBy.Should().Be(_userId);
        a.DeletedAt.Should().BeNull();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task End_MissingOrForeignAssignment_IsNotFound(bool foreign)
    {
        var id = Guid.NewGuid();
        if (foreign)
            _assignments.GetByIdForUpdateAsync(id, _companyId, default).Returns(
                ParkingAssignment.Create(Guid.NewGuid(), _spot.Id, _lease.Id, _today, _now, null));
        var handler = new EndParkingAssignmentCommandHandler(_assignments, _scope, _user, _clock);
        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new(id), default));
    }

    [Fact]
    public async Task Reads_ForeignParents_AreNotFound()
    {
        Set(_spot, "CompanyId", Guid.NewGuid());
        Set(_lease, "CompanyId", Guid.NewGuid());
        await Assert.ThrowsAsync<NotFoundException>(() => new GetCurrentAssignmentBySpotQueryHandler(_assignments, _spots, _scope)
            .Handle(new(_spot.Id), default));
        await Assert.ThrowsAsync<NotFoundException>(() => new GetAssignedParkingByLeaseQueryHandler(_assignments, _leases, _scope)
            .Handle(new(_lease.Id), default));
        await _assignments.DidNotReceive().GetCurrentBySpotAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), default);
        await _assignments.DidNotReceive().GetCurrentByLeaseAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), default);
    }

    [Fact]
    public async Task Validators_RejectEmptyIds()
    {
        var assignCommand = new AssignParkingSpotCommand(Guid.Empty, Guid.Empty);
        var endCommand = new EndParkingAssignmentCommand(Guid.Empty);

        (await new AssignParkingSpotCommandValidator().ValidateAsync(assignCommand, CancellationToken.None)).Errors.Should().HaveCount(2);
        (await new EndParkingAssignmentCommandValidator().ValidateAsync(endCommand, CancellationToken.None)).IsValid.Should().BeFalse();
    }
}
