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
using PropertyOS.Application.Properties.Buildings.Commands.ArchiveBuilding;
using PropertyOS.Application.Properties.Buildings.Commands.CreateBuilding;
using PropertyOS.Application.Properties.Buildings.Commands.UpdateBuilding;
using PropertyOS.Application.Properties.Buildings.Queries.Common;
using PropertyOS.Application.Properties.Buildings.Queries.GetBuildingById;
using PropertyOS.Application.Properties.Buildings.Queries.ListBuildings;
using PropertyOS.Domain.Properties.Enums;
using Xunit;

namespace PropertyOS.Tests.Unit.Api.Controllers.Properties;

public class BuildingsControllerTests
{
    private readonly ISender _mediator = Substitute.For<ISender>();
    private readonly Microsoft.Extensions.Logging.ILogger<BuildingsController> _logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<BuildingsController>>();
    private readonly BuildingsController _controller;

    public BuildingsControllerTests()
    {
        _controller = new BuildingsController(_mediator, _logger)
        {
            ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext()
            }
        };
    }

    [Fact]
    public async Task Create_ShouldSendCreateBuildingCommand_AndReturnNoContent()
    {
        // Arrange
        var request = new CreateBuildingRequest
        {
            Name = "Al-Noor Tower",
            TotalFloors = 5,
            BuildingType = BuildingType.Residential,
            AddressGovernorate = Governorate.Amman,
            AddressCity = "Amman",
            AddressNeighborhood = "Abdoun"
        };

        _mediator.Send(Arg.Any<CreateBuildingCommand>(), Arg.Any<CancellationToken>())
            .Returns(MediatR.Unit.Value);

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        await _mediator.Received(1).Send(Arg.Is<CreateBuildingCommand>(c =>
            c.Name == "Al-Noor Tower" &&
            c.TotalFloors == 5 &&
            c.BuildingType == BuildingType.Residential &&
            c.AddressGovernorate == Governorate.Amman &&
            c.AddressCity == "Amman" &&
            c.AddressNeighborhood == "Abdoun"
        ), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetById_ShouldSendGetBuildingByIdQuery_AndReturnOkResult()
    {
        // Arrange
        var buildingId = Guid.NewGuid();
        var expectedDto = new BuildingDto
        {
            Id = buildingId,
            Name = "Al-Noor Tower",
            TotalFloors = 5
        };

        _mediator.Send(Arg.Any<GetBuildingByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(expectedDto);

        // Act
        var result = await _controller.GetById(buildingId, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedDto);

        await _mediator.Received(1).Send(Arg.Is<GetBuildingByIdQuery>(q => q.Id == buildingId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task List_ShouldSendListBuildingsQuery_AndReturnOkResult()
    {
        // Arrange
        var expectedList = new List<BuildingDto>
        {
            new BuildingDto { Id = Guid.NewGuid(), Name = "Building 1" },
            new BuildingDto { Id = Guid.NewGuid(), Name = "Building 2" }
        };

        _mediator.Send(Arg.Any<ListBuildingsQuery>(), Arg.Any<CancellationToken>())
            .Returns(expectedList);

        // Act
        var result = await _controller.List(CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedList);

        await _mediator.Received(1).Send(Arg.Any<ListBuildingsQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_ShouldSendUpdateBuildingCommand_AndReturnNoContent()
    {
        // Arrange
        var buildingId = Guid.NewGuid();
        var request = new UpdateBuildingRequest
        {
            Name = "Updated Tower",
            BuildingType = BuildingType.Commercial,
            AddressGovernorate = Governorate.Zarqa,
            AddressCity = "Zarqa",
            AddressNeighborhood = "New Zarqa"
        };

        _mediator.Send(Arg.Any<UpdateBuildingCommand>(), Arg.Any<CancellationToken>())
            .Returns(MediatR.Unit.Value);

        // Act
        var result = await _controller.Update(buildingId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        await _mediator.Received(1).Send(Arg.Is<UpdateBuildingCommand>(c =>
            c.Id == buildingId &&
            c.Name == "Updated Tower" &&
            c.BuildingType == BuildingType.Commercial &&
            c.AddressGovernorate == Governorate.Zarqa &&
            c.AddressCity == "Zarqa" &&
            c.AddressNeighborhood == "New Zarqa"
        ), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Archive_ShouldSendArchiveBuildingCommand_AndReturnNoContent()
    {
        // Arrange
        var buildingId = Guid.NewGuid();
        _mediator.Send(Arg.Any<ArchiveBuildingCommand>(), Arg.Any<CancellationToken>())
            .Returns(MediatR.Unit.Value);

        // Act
        var result = await _controller.Archive(buildingId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        await _mediator.Received(1).Send(Arg.Is<ArchiveBuildingCommand>(c => c.Id == buildingId), Arg.Any<CancellationToken>());
    }
}
