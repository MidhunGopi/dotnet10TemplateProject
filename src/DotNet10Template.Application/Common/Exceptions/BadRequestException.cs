namespace DotNet10Template.Application.Common.Exceptions;

/// <summary>
/// Exception for bad request
/// </summary>
public class BadRequestException : Exception
{
    public BadRequestException() : base() { }

    public BadRequestException(string message) : base(message) { }

    public BadRequestException(string message, Exception innerException) : base(message, innerException) { }
}
