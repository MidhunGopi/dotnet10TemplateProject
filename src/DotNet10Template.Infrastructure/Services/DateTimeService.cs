using DotNet10Template.Domain.Interfaces;

namespace DotNet10Template.Infrastructure.Services;

/// <summary>
/// Date time service implementation
/// </summary>
public class DateTimeService : IDateTimeService
{
    public DateTime Now => DateTime.Now;
    public DateTime UtcNow => DateTime.UtcNow;
}
