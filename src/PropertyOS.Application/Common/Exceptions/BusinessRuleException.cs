using System;

namespace PropertyOS.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when a domain business rule is violated.
/// Maps to HTTP 422 Unprocessable Entity.
/// </summary>
public class BusinessRuleException : Exception
{
    public string? Code { get; }

    public BusinessRuleException(string message)
        : base(message)
    {
    }

    public BusinessRuleException(string message, string code)
        : base(message)
    {
        Code = code;
    }

    public BusinessRuleException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
