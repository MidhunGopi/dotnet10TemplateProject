namespace DotNet10Template.Domain.Interfaces;

/// <summary>
/// Interface for date time service
/// </summary>
public interface IDateTimeService
{
    DateTime Now { get; }
    DateTime UtcNow { get; }
}
