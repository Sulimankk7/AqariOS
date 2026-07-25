using System;
using System.Collections.Generic;
using MediatR;
using PropertyOS.Application.Properties.Buildings.Queries.Common;

namespace PropertyOS.Application.Properties.Buildings.Queries.ListBuildings;

public record ListBuildingsQuery() : IRequest<List<BuildingDto>>;
