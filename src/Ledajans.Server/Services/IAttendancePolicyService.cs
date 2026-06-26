using Ledajans.Shared;

namespace Ledajans.Server.Services;

public interface IAttendancePolicyService
{
    Task<AttendancePolicyDto> GetAsync(CancellationToken cancellationToken = default);
    Task<ResolvedAttendancePolicy> GetResolvedAsync(CancellationToken cancellationToken = default);
    Task<AttendancePolicyDto> UpdateAsync(AttendancePolicyDto dto, CancellationToken cancellationToken = default);
    Task<bool> IsLateAsync(DateTime checkInUtc, CancellationToken cancellationToken = default);
    Task<double> GetMaxGpsAccuracyMetersAsync(CancellationToken cancellationToken = default);
}

public sealed class ResolvedAttendancePolicy
{
    public int LateCheckInHour { get; init; }
    public int LateCheckInMinute { get; init; }
    public double MaxGpsAccuracyMeters { get; init; }

    public TimeOnly LateThreshold => new(LateCheckInHour, LateCheckInMinute);

    public bool IsLate(DateTime checkInUtc)
        => AppTime.TimeInTurkey(checkInUtc) > LateThreshold;
}
