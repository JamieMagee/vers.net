using System;

namespace Vers;

/// <summary>
/// Exception thrown for vers parsing, validation, or evaluation errors.
/// </summary>
public class VersException : Exception
{
    public VersException(string message)
        : base(message) { }

    public VersException(string message, Exception innerException)
        : base(message, innerException) { }
}
