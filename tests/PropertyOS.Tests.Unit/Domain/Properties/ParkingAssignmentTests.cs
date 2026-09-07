using FluentAssertions;
using PropertyOS.Domain.Properties;
using PropertyOS.Domain.Properties.Enums;

namespace PropertyOS.Tests.Unit.Domain.Properties;

public class ParkingAssignmentTests
{
    [Fact]
    public void End_BeforeStart_IsRejectedWithoutMutation()
    {
        var day = new DateOnly(2026, 9, 3);
        var a = ParkingAssignment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), day, DateTimeOffset.UtcNow, null);
        var act = () => a.EndAssignment(day.AddDays(-1), DateTimeOffset.UtcNow, null);
        act.Should().Throw<ArgumentException>();
        a.Status.Should().Be(ParkingAssignmentStatus.Active);
        a.AssignedTo.Should().BeNull();
    }

    [Fact]
    public void Create_RejectsInvalidIdsAndDates()
    {
        var day = new DateOnly(2026, 9, 3);
        var act = () => ParkingAssignment.Create(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), day, DateTimeOffset.UtcNow, null);
        act.Should().Throw<ArgumentException>();
        var dates = () => ParkingAssignment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), day,
            DateTimeOffset.UtcNow, null, day.AddDays(-1));
        dates.Should().Throw<ArgumentException>();
    }
}
