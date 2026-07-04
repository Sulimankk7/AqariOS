using System.ComponentModel;

namespace PropertyOS.Domain.Properties.Enums;

public enum ParkingType
{
    [Description("standard")]
    Standard,

    [Description("covered")]
    Covered,

    [Description("visitor")]
    Visitor,

    [Description("disabled_access")]
    DisabledAccess
}
