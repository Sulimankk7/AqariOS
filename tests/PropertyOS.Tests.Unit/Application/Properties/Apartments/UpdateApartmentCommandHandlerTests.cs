using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Properties;
using PropertyOS.Application.Properties.Apartments.Commands.UpdateApartment;
using PropertyOS.Domain.Properties;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Properties.Apartments;

public class UpdateApartmentCommandHandlerTests
{
    private readonly IApartmentRepository _apartmentRepository = Substitute.For<IApartmentRepository>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUserContext _currentUserContext = Substitute.For<ICurrentUserContext>();
    private readonly UpdateApartmentCommandHandler _handler;

    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid BuildingId = Guid.CreateVersion7();
    private static readonly Guid FloorId = Guid.CreateVersion7();
    private static readonly Guid UserId = Guid.NewGuid();

    public UpdateApartmentCommandHandlerTests()
    {
        _tenantContext.CompanyId.Returns(CompanyId);
        _currentUserContext.UserId.Returns(UserId);

        _handler = new UpdateApartmentCommandHandler(
            _apartmentRepository,
            _tenantContext,
            _currentUserContext);
    }

    [Fact]
    public async Task Handle_WithValidRequest_ShouldUpdateBaseRent()
    {
        // Arrange
        var apartmentId = Guid.CreateVersion7();
        var apartment = Apartment.Create(CompanyId, BuildingId, FloorId, "101", 100m, DateTimeOffset.UtcNow, UserId, baseRentAmount: 400m);
        typeof(Apartment).GetProperty("Id")?.SetValue(apartment, apartmentId);

        _apartmentRepository.GetByIdAsync(apartmentId, Arg.Any<CancellationToken>()).Returns(apartment);

        var command = new UpdateApartmentCommand(apartmentId, BaseRentAmount: 550m, BaseRentCurrency: "JOD");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        apartment.BaseRentAmount.Should().Be(550m);
        apartment.BaseRentCurrency.Should().Be("JOD");
        apartment.CompanyId.Should().Be(CompanyId);
        apartment.BuildingId.Should().Be(BuildingId);
        apartment.FloorId.Should().Be(FloorId);
        apartment.UpdatedBy.Should().Be(UserId);
    }

    [Fact]
    public async Task Handle_WithoutTenantContext_ShouldThrowUnauthorized()
    {
        // Arrange
        _tenantContext.CompanyId.Returns((Guid?)null);
        var command = new UpdateApartmentCommand(Guid.NewGuid(), 500m);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenApartmentNotFound_ShouldThrowNotFound()
    {
        // Arrange
        var apartmentId = Guid.NewGuid();
        _apartmentRepository.GetByIdAsync(apartmentId, Arg.Any<CancellationToken>()).Returns((Apartment?)null);

        var command = new UpdateApartmentCommand(apartmentId, 500m);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ForDifferentCompanyApartment_ShouldThrowNotFound()
    {
        // Arrange
        var otherCompanyId = Guid.NewGuid();
        var apartmentId = Guid.CreateVersion7();
        var apartment = Apartment.Create(otherCompanyId, BuildingId, FloorId, "101", 100m, DateTimeOffset.UtcNow, null);
        typeof(Apartment).GetProperty("Id")?.SetValue(apartment, apartmentId);

        _apartmentRepository.GetByIdAsync(apartmentId, Arg.Any<CancellationToken>()).Returns(apartment);

        var command = new UpdateApartmentCommand(apartmentId, 500m);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(command, CancellationToken.None));
    }
}
