namespace DotNet10Template.Application.Common.Exceptions;

/// <summary>
/// Exception for forbidden access
/// </summary>
public class ForbiddenAccessException : Exception
{
    public ForbiddenAccessException() : base("You do not have permission to access this resource.") { }

    public ForbiddenAccessException(string message) : base(message) { }
}
