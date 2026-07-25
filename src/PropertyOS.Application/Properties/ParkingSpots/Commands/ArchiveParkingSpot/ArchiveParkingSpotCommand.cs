using System;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Properties.ParkingSpots.Commands.ArchiveParkingSpot;

public record ArchiveParkingSpotCommand(Guid Id) : ICommand;
