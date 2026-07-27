using System;

namespace PropertyOS.Application.Common.Interfaces;

public interface IBusinessClock
{
    DateTimeOffset UtcNow { get; }
    DateOnly GetJordanBusinessDate(DateTimeOffset? utcInstant = null);
}
