using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Properties;
using PropertyOS.Application.Properties.Apartments.Queries.GetApartmentById;
using PropertyOS.Domain.Properties;
using PropertyOS.Domain.Properties.Enums;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Properties.Apartments;

public class GetApartmentByIdQueryHandlerTests
{
    private readonly IApartmentRepository _apartmentRepository = Substitute.For<IApartmentRepository>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly GetApartmentByIdQueryHandler _handler;

    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid BuildingId = Guid.CreateVersion7();
    private static readonly Guid FloorId = Guid.CreateVersion7();

    public GetApartmentByIdQueryHandlerTests()
    {
        _tenantContext.CompanyId.Returns(CompanyId);
        _handler = new GetApartmentByIdQueryHandler(_apartmentRepository, _tenantContext);
    }

    [Fact]
    public async Task Handle_WithExistingApartment_ShouldReturnApartmentDto()
    {
        // Arrange
        var apartmentId = Guid.CreateVersion7();
        var apartment = Apartment.Create(CompanyId, BuildingId, FloorId, "101", 100m, DateTimeOffset.UtcNow, null, baseRentAmount: 450m);
        typeof(Apartment).GetProperty("Id")?.SetValue(apartment, apartmentId);

        _apartmentRepository.GetByIdAsync(apartmentId, Arg.Any<CancellationToken>()).Returns(apartment);

        var query = new GetApartmentByIdQuery(apartmentId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(apartmentId);
        result.UnitNumber.Should().Be("101");
        result.AreaSqm.Should().Be(100m);
        result.BaseRentAmount.Should().Be(450m);
        result.CompanyId.Should().Be(CompanyId);
        result.BuildingId.Should().Be(BuildingId);
        result.FloorId.Should().Be(FloorId);
    }

    [Fact]
    public async Task Handle_WithoutTenantContext_ShouldThrowUnauthorized()
    {
        // Arrange
        _tenantContext.CompanyId.Returns((Guid?)null);
        var query = new GetApartmentByIdQuery(Guid.NewGuid());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _handler.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenApartmentNotFound_ShouldThrowNotFound()
    {
        // Arrange
        var apartmentId = Guid.NewGuid();
        _apartmentRepository.GetByIdAsync(apartmentId, Arg.Any<CancellationToken>()).Returns((Apartment?)null);

        var query = new GetApartmentByIdQuery(apartmentId);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(query, CancellationToken.None));
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

        var query = new GetApartmentByIdQuery(apartmentId);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenApartmentArchived_ShouldThrowNotFound()
    {
        // Arrange
        var apartmentId = Guid.CreateVersion7();
        var apartment = Apartment.Create(CompanyId, BuildingId, FloorId, "101", 100m, DateTimeOffset.UtcNow, null);
        typeof(Apartment).GetProperty("Id")?.SetValue(apartment, apartmentId);
        apartment.SoftDelete(DateTimeOffset.UtcNow, null);

        _apartmentRepository.GetByIdAsync(apartmentId, Arg.Any<CancellationToken>()).Returns(apartment);

        var query = new GetApartmentByIdQuery(apartmentId);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(query, CancellationToken.None));
    }
}
