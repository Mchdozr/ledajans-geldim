using Ledajans.Shared;

namespace Ledajans.Server.Services;

public interface IAttendancePolicyService
{
    Task<AttendancePolicyDto> GetAsync(int locationId, CancellationToken cancellationToken = default);
    Task<ResolvedAttendancePolicy> GetResolvedAsync(int locationId, CancellationToken cancellationToken = default);
    Task<AttendancePolicyDto> UpdateAsync(int locationId, AttendancePolicyDto dto, CancellationToken cancellationToken = default);
    Task<bool> IsLateAsync(int locationId, DateTime checkInUtc, CancellationToken cancellationToken = default);
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
