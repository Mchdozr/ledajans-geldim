using System.ComponentModel.DataAnnotations;

namespace Ledajans.Shared;

public record LoginRequest
{
    [Required(ErrorMessage = "Kullanıcı adı gerekli")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre gerekli")]
    public string Password { get; set; } = string.Empty;
}

public record LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime ExpiresUtc { get; set; }
}

public record CheckInRequest
{
    [Range(-90, 90)]
    public double Latitude { get; set; }

    [Range(-180, 180)]
    public double Longitude { get; set; }

    public double? Accuracy { get; set; }
}

public record CheckInResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime? CheckInUtc { get; set; }
    public double DistanceMeters { get; set; }
}

public record CheckOutResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime? CheckOutUtc { get; set; }
    public double DistanceMeters { get; set; }
}

public record TodayStatusResponse
{
    public bool HasCheckedIn { get; set; }
    public DateTime? CheckInUtc { get; set; }
    public bool HasCheckedOut { get; set; }
    public DateTime? CheckOutUtc { get; set; }
}

public record GeofenceDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Konum adı gerekli")]
    public string Name { get; set; } = string.Empty;

    [Range(-90, 90, ErrorMessage = "Enlem -90 ile 90 arasında olmalı")]
    public double Latitude { get; set; }

    [Range(-180, 180, ErrorMessage = "Boylam -180 ile 180 arasında olmalı")]
    public double Longitude { get; set; }

    [Range(10, 100000, ErrorMessage = "Yarıçap 10 ile 100000 metre arasında olmalı")]
    public int RadiusMeters { get; set; } = 100;
}

public record UserDto
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Role { get; set; } = "Employee";
    public string Department { get; set; } = Departments.Teknik;
    public bool IsActive { get; set; } = true;
}

public record CreateUserRequest
{
    [Required(ErrorMessage = "Kullanıcı adı gerekli")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ad soyad gerekli")]
    public string FullName { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Geçerli bir e-posta girin")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Şifre gerekli")]
    public string Password { get; set; } = string.Empty;

    public string Role { get; set; } = "Employee";
    public string Department { get; set; } = Departments.Teknik;
}

public record UpdateUserRequest
{
    [Required(ErrorMessage = "Ad soyad gerekli")]
    public string FullName { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Geçerli bir e-posta girin")]
    public string? Email { get; set; }

    public string Role { get; set; } = "Employee";
    public bool IsActive { get; set; } = true;
    public string Department { get; set; } = Departments.Teknik;
}

public record SetPasswordRequest
{
    [Required(ErrorMessage = "Şifre gerekli")]
    public string Password { get; set; } = string.Empty;
}

public record AttendanceReportItem
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public DateTime CheckInUtc { get; set; }
    public DateTime? CheckOutUtc { get; set; }
    public DateOnly LocalDate { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double DistanceMeters { get; set; }
    public string? IpAddress { get; set; }
    public bool IsManual { get; set; }
    public string? AdminNote { get; set; }
    public bool IsLate { get; set; }
}

public record TodaySummaryEmployee
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Department { get; set; } = string.Empty;
}

public record TodaySummaryResponse
{
    public DateOnly Date { get; set; }
    public int TotalActive { get; set; }
    public int ExcusedCount { get; set; }
    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public bool IsCompanyHoliday { get; set; }
    public List<AttendanceReportItem> Present { get; set; } = new();
    public List<TodaySummaryEmployee> Absent { get; set; } = new();
    public List<TodaySummaryEmployee> Excused { get; set; } = new();
}

public record NonWorkingDayDto
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public string Type { get; set; } = NonWorkingDayTypes.Holiday;
    public string? UserId { get; set; }
    public string? UserFullName { get; set; }
    public string? Note { get; set; }
}

public record CreateNonWorkingDayRequest
{
    [Required]
    public DateOnly Date { get; set; }

    [Required]
    public string Type { get; set; } = NonWorkingDayTypes.Holiday;

    public string? UserId { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }
}

public record ManualCheckInRequest
{
    [Required(ErrorMessage = "Kullanıcı gerekli")]
    public string UserId { get; set; } = string.Empty;

    public DateOnly? LocalDate { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }
}

public record MyAttendanceHistoryItem
{
    public DateOnly LocalDate { get; set; }
    public DateTime CheckInUtc { get; set; }
    public DateTime? CheckOutUtc { get; set; }
    public double DistanceMeters { get; set; }
}

public static class Roles
{
    public const string Admin = "Admin";
    public const string Employee = "Employee";
}
