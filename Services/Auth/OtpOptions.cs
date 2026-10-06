// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Cấu hình các tham số cho OTP

using System.ComponentModel.DataAnnotations;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services.Auth;

/// <summary>Cấu hình OTP (mục "Otp" trong appsettings). Đổi giá trị cần triển khai lại ứng dụng.</summary>
public class OtpOptions
{
    public const string SectionName = "Otp";

    [Range(1, 60)]
    public int LifetimeMinutes { get; set; } = 10;

    [Range(10, 3600)]
    public int ResendCooldownSeconds { get; set; } = 60;

    [Range(1, 100)]
    public int MaxRequestsPerHour { get; set; } = 5;

    [Range(1, 20)]
    public int MaxVerifyAttempts { get; set; } = 5;

    public TimeSpan Lifetime => TimeSpan.FromMinutes(LifetimeMinutes);
    public TimeSpan ResendCooldown => TimeSpan.FromSeconds(ResendCooldownSeconds);
}
