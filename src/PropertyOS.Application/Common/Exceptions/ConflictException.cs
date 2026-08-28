using System;

namespace PropertyOS.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when a request conflicts with current state or existing record (e.g. duplicate active subscription).
/// </summary>
public class ConflictException : Exception
{
    public string? Code { get; }

    /// <summary>
    /// Initializes a new instance of the ConflictException class.
    /// </summary>
    /// <param name="message">Error description message.</param>
    public ConflictException(string message) : base(message)
    {
    }

    public ConflictException(string message, string code) : base(message)
    {
        Code = code;
    }
}
