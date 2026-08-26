using NileTechno.Application.Common.Interfaces;

namespace NileTechno.Infrastructure.Services;

public class DateTimeService : IDateTime
{
    public DateTime UtcNow => DateTime.UtcNow;
}
