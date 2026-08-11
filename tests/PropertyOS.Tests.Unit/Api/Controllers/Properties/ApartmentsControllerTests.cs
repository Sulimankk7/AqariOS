using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using PropertyOS.Api.Controllers;
using PropertyOS.Api.Models.Properties;
using PropertyOS.Application.Properties.Apartments.Commands.ArchiveApartment;
using PropertyOS.Application.Properties.Apartments.Commands.CreateApartment;
using PropertyOS.Application.Properties.Apartments.Commands.UpdateApartment;
using PropertyOS.Application.Properties.Apartments.Queries.Common;
using PropertyOS.Application.Properties.Apartments.Queries.GetApartmentById;
using PropertyOS.Application.Properties.Apartments.Queries.ListApartments;
using PropertyOS.Domain.Properties.Enums;
using Xunit;

namespace PropertyOS.Tests.Unit.Api.Controllers.Properties;

public class ApartmentsControllerTests
{
    private readonly ISender _mediator = Substitute.For<ISender>();
    private readonly Microsoft.Extensions.Logging.ILogger<ApartmentsController> _logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<ApartmentsController>>();
    private readonly ApartmentsController _controller;

    public ApartmentsControllerTests()
    {
        _controller = new ApartmentsController(_mediator, _logger)
        {
            ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext()
            }
        };
    }

    [Fact]
    public async Task Create_ShouldSendCreateApartmentCommand_WithRouteFloorId_AndReturnNoContent()
    {
        // Arrange
        var floorId = Guid.NewGuid();
        var request = new CreateApartmentRequest
        {
            UnitNumber = "101",
            AreaSqm = 120.5m,
            OwnershipStatus = OwnershipStatus.CompanyOwned,
            Bedrooms = 2,
            Bathrooms = 2,
            BaseRentAmount = 500m,
            BaseRentCurrency = "JOD"
        };

        _mediator.Send(Arg.Any<CreateApartmentCommand>(), Arg.Any<CancellationToken>())
            .Returns(MediatR.Unit.Value);

        // Act
        var result = await _controller.Create(floorId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        await _mediator.Received(1).Send(Arg.Is<CreateApartmentCommand>(c =>
            c.FloorId == floorId &&
            c.UnitNumber == "101" &&
            c.AreaSqm == 120.5m &&
            c.OwnershipStatus == OwnershipStatus.CompanyOwned &&
            c.Bedrooms == 2 &&
            c.Bathrooms == 2 &&
            c.BaseRentAmount == 500m &&
            c.BaseRentCurrency == "JOD"
        ), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task List_ShouldSendListApartmentsQuery_AndReturnOkResult()
    {
        // Arrange
        var buildingId = Guid.NewGuid();
        var floorId = Guid.NewGuid();
        var expectedList = new List<ApartmentDto>
        {
            new ApartmentDto { Id = Guid.NewGuid(), UnitNumber = "101" }
        };

        _mediator.Send(Arg.Any<ListApartmentsQuery>(), Arg.Any<CancellationToken>())
            .Returns(expectedList);

        // Act
        var result = await _controller.List(buildingId, floorId, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedList);

        await _mediator.Received(1).Send(Arg.Is<ListApartmentsQuery>(q =>
            q.BuildingId == buildingId &&
            q.FloorId == floorId
        ), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetById_ShouldSendGetApartmentByIdQuery_AndReturnOkResult()
    {
        // Arrange
        var apartmentId = Guid.NewGuid();
        var expectedDto = new ApartmentDto { Id = apartmentId, UnitNumber = "101" };

        _mediator.Send(Arg.Any<GetApartmentByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(expectedDto);

        // Act
        var result = await _controller.GetById(apartmentId, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedDto);

        await _mediator.Received(1).Send(Arg.Is<GetApartmentByIdQuery>(q => q.Id == apartmentId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_ShouldSendUpdateApartmentCommand_AndReturnNoContent()
    {
        // Arrange
        var apartmentId = Guid.NewGuid();
        var request = new UpdateApartmentRequest
        {
            BaseRentAmount = 600m,
            BaseRentCurrency = "JOD"
        };

        _mediator.Send(Arg.Any<UpdateApartmentCommand>(), Arg.Any<CancellationToken>())
            .Returns(MediatR.Unit.Value);

        // Act
        var result = await _controller.Update(apartmentId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        await _mediator.Received(1).Send(Arg.Is<UpdateApartmentCommand>(c =>
            c.Id == apartmentId &&
            c.BaseRentAmount == 600m &&
            c.BaseRentCurrency == "JOD"
        ), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Archive_ShouldSendArchiveApartmentCommand_AndReturnNoContent()
    {
        // Arrange
        var apartmentId = Guid.NewGuid();
        _mediator.Send(Arg.Any<ArchiveApartmentCommand>(), Arg.Any<CancellationToken>())
            .Returns(MediatR.Unit.Value);

        // Act
        var result = await _controller.Archive(apartmentId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        await _mediator.Received(1).Send(Arg.Is<ArchiveApartmentCommand>(c => c.Id == apartmentId), Arg.Any<CancellationToken>());
    }
}
