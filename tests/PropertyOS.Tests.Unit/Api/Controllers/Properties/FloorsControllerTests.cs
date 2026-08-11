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
using PropertyOS.Application.Properties.Floors.Commands.ArchiveFloor;
using PropertyOS.Application.Properties.Floors.Commands.CreateFloor;
using PropertyOS.Application.Properties.Floors.Commands.UpdateFloor;
using PropertyOS.Application.Properties.Floors.Queries.Common;
using PropertyOS.Application.Properties.Floors.Queries.GetFloorById;
using PropertyOS.Application.Properties.Floors.Queries.ListFloors;
using PropertyOS.Domain.Properties.Enums;
using Xunit;

namespace PropertyOS.Tests.Unit.Api.Controllers.Properties;

public class FloorsControllerTests
{
    private readonly ISender _mediator = Substitute.For<ISender>();
    private readonly Microsoft.Extensions.Logging.ILogger<FloorsController> _logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<FloorsController>>();
    private readonly FloorsController _controller;

    public FloorsControllerTests()
    {
        _controller = new FloorsController(_mediator, _logger)
        {
            ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext()
            }
        };
    }

    [Fact]
    public async Task Create_ShouldSendCreateFloorCommand_WithRouteBuildingId_AndReturnNoContent()
    {
        // Arrange
        var buildingId = Guid.NewGuid();
        var request = new CreateFloorRequest
        {
            FloorNumber = 1,
            FloorLabel = "First Floor",
            FloorType = FloorType.Regular
        };

        _mediator.Send(Arg.Any<CreateFloorCommand>(), Arg.Any<CancellationToken>())
            .Returns(MediatR.Unit.Value);

        // Act
        var result = await _controller.Create(buildingId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        await _mediator.Received(1).Send(Arg.Is<CreateFloorCommand>(c =>
            c.BuildingId == buildingId &&
            c.FloorNumber == 1 &&
            c.FloorLabel == "First Floor" &&
            c.FloorType == FloorType.Regular
        ), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListByBuilding_ShouldSendListFloorsQuery_AndReturnOkResult()
    {
        // Arrange
        var buildingId = Guid.NewGuid();
        var expectedList = new List<FloorDto>
        {
            new FloorDto { Id = Guid.NewGuid(), FloorLabel = "Floor 1" }
        };

        _mediator.Send(Arg.Any<ListFloorsQuery>(), Arg.Any<CancellationToken>())
            .Returns(expectedList);

        // Act
        var result = await _controller.ListByBuilding(buildingId, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedList);

        await _mediator.Received(1).Send(Arg.Is<ListFloorsQuery>(q => q.BuildingId == buildingId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetById_ShouldSendGetFloorByIdQuery_AndReturnOkResult()
    {
        // Arrange
        var floorId = Guid.NewGuid();
        var expectedDto = new FloorDto { Id = floorId, FloorLabel = "Floor 1" };

        _mediator.Send(Arg.Any<GetFloorByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(expectedDto);

        // Act
        var result = await _controller.GetById(floorId, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedDto);

        await _mediator.Received(1).Send(Arg.Is<GetFloorByIdQuery>(q => q.Id == floorId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_ShouldSendUpdateFloorCommand_AndReturnNoContent()
    {
        // Arrange
        var floorId = Guid.NewGuid();
        var request = new UpdateFloorRequest
        {
            FloorLabel = "Updated Floor Label",
            FloorType = FloorType.Roof
        };

        _mediator.Send(Arg.Any<UpdateFloorCommand>(), Arg.Any<CancellationToken>())
            .Returns(MediatR.Unit.Value);

        // Act
        var result = await _controller.Update(floorId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        await _mediator.Received(1).Send(Arg.Is<UpdateFloorCommand>(c =>
            c.Id == floorId &&
            c.FloorLabel == "Updated Floor Label" &&
            c.FloorType == FloorType.Roof
        ), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Archive_ShouldSendArchiveFloorCommand_AndReturnNoContent()
    {
        // Arrange
        var floorId = Guid.NewGuid();
        _mediator.Send(Arg.Any<ArchiveFloorCommand>(), Arg.Any<CancellationToken>())
            .Returns(MediatR.Unit.Value);

        // Act
        var result = await _controller.Archive(floorId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        await _mediator.Received(1).Send(Arg.Is<ArchiveFloorCommand>(c => c.Id == floorId), Arg.Any<CancellationToken>());
    }
}
