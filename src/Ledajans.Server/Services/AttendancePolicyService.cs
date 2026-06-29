using Ledajans.Server.Data;
using Ledajans.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Ledajans.Server.Services;

public class AttendancePolicyService : IAttendancePolicyService
{
    private readonly AppDbContext _db;
    private readonly AttendanceSettings _defaults;

    public AttendancePolicyService(AppDbContext db, IOptions<AttendanceSettings> defaults)
    {
        _db = db;
        _defaults = defaults.Value;
    }

    public async Task<AttendancePolicyDto> GetAsync(int locationId, CancellationToken cancellationToken = default)
    {
        var policy = await ResolveAsync(locationId, cancellationToken);
        return ToDto(policy);
    }

    public async Task<AttendancePolicyDto> UpdateAsync(int locationId, AttendancePolicyDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.LateCheckInHour is < 0 or > 23)
            throw new ArgumentOutOfRangeException(nameof(dto.LateCheckInHour));
        if (dto.LateCheckInMinute is < 0 or > 59)
            throw new ArgumentOutOfRangeException(nameof(dto.LateCheckInMinute));

        var row = await _db.CompanySettings.FindAsync([locationId], cancellationToken);
        if (row is null)
        {
            row = new CompanySettings { LocationId = locationId };
            _db.CompanySettings.Add(row);
        }

        row.LateCheckInHour = dto.LateCheckInHour;
        row.LateCheckInMinute = dto.LateCheckInMinute;
        await _db.SaveChangesAsync(cancellationToken);

        return ToDto(await ResolveAsync(locationId, cancellationToken));
    }

    public Task<ResolvedAttendancePolicy> GetResolvedAsync(int locationId, CancellationToken cancellationToken = default)
        => ResolveAsync(locationId, cancellationToken);

    public async Task<bool> IsLateAsync(int locationId, DateTime checkInUtc, CancellationToken cancellationToken = default)
    {
        var policy = await ResolveAsync(locationId, cancellationToken);
        return policy.IsLate(checkInUtc);
    }

    public Task<double> GetMaxGpsAccuracyMetersAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_defaults.MaxGpsAccuracyMeters);

    private async Task<ResolvedAttendancePolicy> ResolveAsync(int locationId, CancellationToken cancellationToken)
    {
        var row = await _db.CompanySettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.LocationId == locationId, cancellationToken);

        return new ResolvedAttendancePolicy
        {
            LateCheckInHour = row?.LateCheckInHour ?? _defaults.LateCheckInHour,
            LateCheckInMinute = row?.LateCheckInMinute ?? _defaults.LateCheckInMinute,
            MaxGpsAccuracyMeters = _defaults.MaxGpsAccuracyMeters
        };
    }

    private static AttendancePolicyDto ToDto(ResolvedAttendancePolicy policy)
        => new()
        {
            LateCheckInHour = policy.LateCheckInHour,
            LateCheckInMinute = policy.LateCheckInMinute,
            WorkStartTime = policy.LateThreshold.ToString("HH:mm")
        };
}
