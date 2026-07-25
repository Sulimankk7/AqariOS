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
using PropertyOS.Application.Properties.ParkingSpots.Commands.ArchiveParkingSpot;
using PropertyOS.Application.Properties.ParkingSpots.Commands.CreateParkingSpot;
using PropertyOS.Application.Properties.ParkingSpots.Commands.UpdateParkingSpot;
using PropertyOS.Application.Properties.ParkingSpots.Queries.Common;
using PropertyOS.Application.Properties.ParkingSpots.Queries.GetParkingSpotById;
using PropertyOS.Application.Properties.ParkingSpots.Queries.ListParkingSpots;
using PropertyOS.Domain.Properties.Enums;
using Xunit;

namespace PropertyOS.Tests.Unit.Api.Controllers.Properties;

public class ParkingSpotsControllerTests
{
    private readonly ISender _mediator = Substitute.For<ISender>();
    private readonly ParkingSpotsController _controller;

    public ParkingSpotsControllerTests()
    {
        _controller = new ParkingSpotsController(_mediator);
    }

    [Fact]
    public async Task Create_ShouldSendCreateParkingSpotCommand_WithRouteBuildingId_AndReturnNoContent()
    {
        // Arrange
        var buildingId = Guid.NewGuid();
        var request = new CreateParkingSpotRequest
        {
            SpotCode = "P1-01",
            ParkingType = ParkingType.Covered,
            LocationDescription = "Underground B1"
        };

        _mediator.Send(Arg.Any<CreateParkingSpotCommand>(), Arg.Any<CancellationToken>())
            .Returns(MediatR.Unit.Value);

        // Act
        var result = await _controller.Create(buildingId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        await _mediator.Received(1).Send(Arg.Is<CreateParkingSpotCommand>(c =>
            c.BuildingId == buildingId &&
            c.SpotCode == "P1-01" &&
            c.ParkingType == ParkingType.Covered &&
            c.LocationDescription == "Underground B1"
        ), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListByBuilding_ShouldSendListParkingSpotsQuery_AndReturnOkResult()
    {
        // Arrange
        var buildingId = Guid.NewGuid();
        var expectedList = new List<ParkingSpotDto>
        {
            new ParkingSpotDto { Id = Guid.NewGuid(), SpotCode = "P1-01" }
        };

        _mediator.Send(Arg.Any<ListParkingSpotsQuery>(), Arg.Any<CancellationToken>())
            .Returns(expectedList);

        // Act
        var result = await _controller.ListByBuilding(buildingId, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedList);

        await _mediator.Received(1).Send(Arg.Is<ListParkingSpotsQuery>(q => q.BuildingId == buildingId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetById_ShouldSendGetParkingSpotByIdQuery_AndReturnOkResult()
    {
        // Arrange
        var spotId = Guid.NewGuid();
        var expectedDto = new ParkingSpotDto { Id = spotId, SpotCode = "P1-01" };

        _mediator.Send(Arg.Any<GetParkingSpotByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(expectedDto);

        // Act
        var result = await _controller.GetById(spotId, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedDto);

        await _mediator.Received(1).Send(Arg.Is<GetParkingSpotByIdQuery>(q => q.Id == spotId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_ShouldSendUpdateParkingSpotCommand_AndReturnNoContent()
    {
        // Arrange
        var spotId = Guid.NewGuid();
        var request = new UpdateParkingSpotRequest
        {
            SpotCode = "P1-01-NEW",
            ParkingType = ParkingType.DisabledAccess,
            LocationDescription = "Updated Level B2"
        };

        _mediator.Send(Arg.Any<UpdateParkingSpotCommand>(), Arg.Any<CancellationToken>())
            .Returns(MediatR.Unit.Value);

        // Act
        var result = await _controller.Update(spotId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        await _mediator.Received(1).Send(Arg.Is<UpdateParkingSpotCommand>(c =>
            c.Id == spotId &&
            c.SpotCode == "P1-01-NEW" &&
            c.ParkingType == ParkingType.DisabledAccess &&
            c.LocationDescription == "Updated Level B2"
        ), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Archive_ShouldSendArchiveParkingSpotCommand_AndReturnNoContent()
    {
        // Arrange
        var spotId = Guid.NewGuid();
        _mediator.Send(Arg.Any<ArchiveParkingSpotCommand>(), Arg.Any<CancellationToken>())
            .Returns(MediatR.Unit.Value);

        // Act
        var result = await _controller.Archive(spotId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        await _mediator.Received(1).Send(Arg.Is<ArchiveParkingSpotCommand>(c => c.Id == spotId), Arg.Any<CancellationToken>());
    }
}
