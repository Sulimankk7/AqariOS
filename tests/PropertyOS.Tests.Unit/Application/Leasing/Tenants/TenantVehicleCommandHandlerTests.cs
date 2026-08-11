using System;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Leasing.Commands.CreateTenantVehicle;
using PropertyOS.Application.Leasing.Commands.DeleteTenantVehicle;
using PropertyOS.Application.Leasing.Commands.UpdateTenantVehicle;
using PropertyOS.Application.Leasing.Queries.GetVehicleById;
using PropertyOS.Application.Leasing.Queries.GetVehiclesForTenant;
using PropertyOS.Domain.Leasing;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Leasing.Tenants;

public class TenantVehicleCommandHandlerTests
{
    private readonly FakeTenantRepository _repository = new();
    private readonly FakeTenantContext _tenantContext = new();
    private readonly FakeCurrentUserContext _userContext = new();

    [Fact]
    public async Task CreateVehicle_ShouldAddRecord_WhenTenantExistsAndPlateIsUnique()
    {
        // Arrange
        var companyId = _tenantContext.CompanyId!.Value;
        var tenant = Tenant.Create(companyId, "Ahmad", "1234567890", "+962791234567", DateTimeOffset.UtcNow, _userContext.UserId);
        _repository.Store.Add(tenant);

        var handler = new CreateTenantVehicleCommandHandler(_repository, _tenantContext, _userContext);
        var command = new CreateTenantVehicleCommand(tenant.Id, "12-34567", "Toyota Camry 2022", "White");

        // Act
        var resultId = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, resultId);
        Assert.Single(_repository.AddedVehicles);
        var added = _repository.AddedVehicles[0];
        Assert.Equal("12-34567", added.PlateNumber);
        Assert.Equal("Toyota Camry 2022", added.MakeModel);
        Assert.Equal("White", added.Color);
        Assert.Equal(tenant.Id, added.TenantId);
        Assert.Equal(companyId, added.CompanyId);
    }

    [Fact]
    public async Task CreateVehicle_ShouldThrowConflict_WhenPlateNumberExists()
    {
        // Arrange
        var companyId = _tenantContext.CompanyId!.Value;
        var tenant = Tenant.Create(companyId, "Ahmad", "1234567890", "+962791234567", DateTimeOffset.UtcNow, _userContext.UserId);
        _repository.Store.Add(tenant);

        var existingVehicle = TenantVehicle.Create(companyId, tenant.Id, "12-34567", "Nissan Altima", "Black", DateTimeOffset.UtcNow, _userContext.UserId);
        _repository.VehicleStore.Add(existingVehicle);

        var handler = new CreateTenantVehicleCommandHandler(_repository, _tenantContext, _userContext);
        var command = new CreateTenantVehicleCommand(tenant.Id, "12-34567", "Toyota Camry", "White");

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task CreateVehicle_ShouldThrowNotFound_WhenTenantDoesNotExist()
    {
        // Arrange
        var handler = new CreateTenantVehicleCommandHandler(_repository, _tenantContext, _userContext);
        var command = new CreateTenantVehicleCommand(Guid.NewGuid(), "12-34567", "Toyota Camry", "White");

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateVehicle_ShouldUpdateDetails_WhenVehicleExists()
    {
        // Arrange
        var companyId = _tenantContext.CompanyId!.Value;
        var tenantId = Guid.NewGuid();
        var vehicle = TenantVehicle.Create(companyId, tenantId, "12-34567", "Old Car", "Red", DateTimeOffset.UtcNow, _userContext.UserId);
        _repository.VehicleStore.Add(vehicle);

        var handler = new UpdateTenantVehicleCommandHandler(_repository, _tenantContext, _userContext);
        var command = new UpdateTenantVehicleCommand(tenantId, vehicle.Id, "99-99999", "New Car", "Silver");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal("99-99999", vehicle.PlateNumber);
        Assert.Equal("New Car", vehicle.MakeModel);
        Assert.Equal("Silver", vehicle.Color);
    }

    [Fact]
    public async Task DeleteVehicle_ShouldSoftDelete_WhenVehicleExists()
    {
        // Arrange
        var companyId = _tenantContext.CompanyId!.Value;
        var tenantId = Guid.NewGuid();
        var vehicle = TenantVehicle.Create(companyId, tenantId, "12-34567", "Toyota", "White", DateTimeOffset.UtcNow, _userContext.UserId);
        _repository.VehicleStore.Add(vehicle);

        var handler = new DeleteTenantVehicleCommandHandler(_repository, _tenantContext, _userContext);
        var command = new DeleteTenantVehicleCommand(tenantId, vehicle.Id);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(vehicle.DeletedAt);
        Assert.Equal(_userContext.UserId, vehicle.DeletedBy);
    }

    [Fact]
    public async Task GetVehiclesForTenant_ShouldReturnList_WhenTenantExists()
    {
        // Arrange
        var companyId = _tenantContext.CompanyId!.Value;
        var tenant = Tenant.Create(companyId, "Ahmad", "1234567890", "+962791234567", DateTimeOffset.UtcNow, _userContext.UserId);
        _repository.Store.Add(tenant);

        var v1 = TenantVehicle.Create(companyId, tenant.Id, "11-11111", "Toyota", "White", DateTimeOffset.UtcNow, _userContext.UserId);
        var v2 = TenantVehicle.Create(companyId, tenant.Id, "22-22222", "Honda", "Blue", DateTimeOffset.UtcNow, _userContext.UserId);
        _repository.VehicleStore.Add(v1);
        _repository.VehicleStore.Add(v2);

        var handler = new GetVehiclesForTenantQueryHandler(_repository, _tenantContext);
        var query = new GetVehiclesForTenantQuery(tenant.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("11-11111", result[0].PlateNumber);
        Assert.Equal("22-22222", result[1].PlateNumber);
    }

    [Fact]
    public async Task GetVehicleById_ShouldReturnDto_WhenExists()
    {
        // Arrange
        var companyId = _tenantContext.CompanyId!.Value;
        var tenantId = Guid.NewGuid();
        var vehicle = TenantVehicle.Create(companyId, tenantId, "11-11111", "Toyota", "White", DateTimeOffset.UtcNow, _userContext.UserId);
        _repository.VehicleStore.Add(vehicle);

        var handler = new GetVehicleByIdQueryHandler(_repository, _tenantContext);
        var query = new GetVehicleByIdQuery(tenantId, vehicle.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(vehicle.Id, result!.Id);
        Assert.Equal("11-11111", result.PlateNumber);
        Assert.Equal("Toyota", result.MakeModel);
        Assert.Equal("White", result.Color);
    }
}
