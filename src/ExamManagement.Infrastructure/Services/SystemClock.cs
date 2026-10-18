using ExamManagement.Application.Common.Abstractions;

namespace ExamManagement.Infrastructure.Services;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
