using System.ComponentModel;

namespace PropertyOS.Domain.Properties.Enums;

public enum ParkingAssignmentStatus
{
    [Description("active")]
    Active,

    [Description("ended")]
    Ended
}
