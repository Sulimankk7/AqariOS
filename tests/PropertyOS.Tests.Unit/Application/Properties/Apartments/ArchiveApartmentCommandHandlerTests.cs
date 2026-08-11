using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Properties;
using PropertyOS.Application.Properties.Apartments.Commands.ArchiveApartment;
using PropertyOS.Application.Properties.Apartments.Services;
using PropertyOS.Domain.Properties;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Properties.Apartments;

public class ArchiveApartmentCommandHandlerTests
{
    private readonly IApartmentRepository _apartmentRepository = Substitute.For<IApartmentRepository>();
    private readonly IApartmentArchiveDependencyChecker _dependencyChecker = Substitute.For<IApartmentArchiveDependencyChecker>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUserContext _currentUserContext = Substitute.For<ICurrentUserContext>();
    private readonly Microsoft.Extensions.Logging.ILogger<ArchiveApartmentCommandHandler> _logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<ArchiveApartmentCommandHandler>>();
    private readonly ArchiveApartmentCommandHandler _handler;

    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid BuildingId = Guid.CreateVersion7();
    private static readonly Guid FloorId = Guid.CreateVersion7();
    private static readonly Guid UserId = Guid.NewGuid();

    public ArchiveApartmentCommandHandlerTests()
    {
        _tenantContext.CompanyId.Returns(CompanyId);
        _currentUserContext.UserId.Returns(UserId);

        _handler = new ArchiveApartmentCommandHandler(
            _apartmentRepository,
            _dependencyChecker,
            _tenantContext,
            _currentUserContext,
            _logger);
    }

    [Fact]
    public async Task Handle_WithValidRequest_ShouldSoftDeleteApartment()
    {
        // Arrange
        var apartmentId = Guid.CreateVersion7();
        var apartment = Apartment.Create(CompanyId, BuildingId, FloorId, "Target Unit", 100m, DateTimeOffset.UtcNow, UserId);
        typeof(Apartment).GetProperty("Id")?.SetValue(apartment, apartmentId);

        _apartmentRepository.GetByIdAsync(apartmentId, Arg.Any<CancellationToken>()).Returns(apartment);

        var command = new ArchiveApartmentCommand(apartmentId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        apartment.DeletedAt.Should().NotBeNull();
        apartment.DeletedBy.Should().Be(UserId);
        apartment.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithoutTenantContext_ShouldThrowUnauthorized()
    {
        // Arrange
        _tenantContext.CompanyId.Returns((Guid?)null);
        var command = new ArchiveApartmentCommand(Guid.NewGuid());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenApartmentNotFound_ShouldThrowNotFound()
    {
        // Arrange
        var apartmentId = Guid.NewGuid();
        _apartmentRepository.GetByIdAsync(apartmentId, Arg.Any<CancellationToken>()).Returns((Apartment?)null);

        var command = new ArchiveApartmentCommand(apartmentId);

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

        var command = new ArchiveApartmentCommand(apartmentId);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_AlreadyArchivedApartment_ShouldBeIdempotent()
    {
        // Arrange
        var apartmentId = Guid.CreateVersion7();
        var apartment = Apartment.Create(CompanyId, BuildingId, FloorId, "Target Unit", 100m, DateTimeOffset.UtcNow, UserId);
        typeof(Apartment).GetProperty("Id")?.SetValue(apartment, apartmentId);

        var firstDeletedAt = DateTimeOffset.UtcNow.AddHours(-1);
        apartment.SoftDelete(firstDeletedAt, UserId);

        _apartmentRepository.GetByIdAsync(apartmentId, Arg.Any<CancellationToken>()).Returns(apartment);

        var command = new ArchiveApartmentCommand(apartmentId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        apartment.DeletedAt.Should().Be(firstDeletedAt);
    }
}
