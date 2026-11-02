// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Xây dựng interface cho dịch vụ đặt lại mật khẩu
//Thêm VerifyOtpAsync (bước 2)

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;

public enum RequestResetStatus
{
    Sent,
    EmailNotFound,
    AccountLocked,
    CooldownActive,
    HourlyLimitReached,
    EmailSendFailed
}

public sealed record RequestResetResult(RequestResetStatus Status, int RetryAfterSeconds = 0);

public enum ResetPasswordStatus
{
    Success,
    EmailNotFound,
    AccountLocked,
    InvalidOrExpiredCode,
    TooManyAttempts
}

public interface IPasswordResetService
{
    /// <summary>Bước 1: gửi mã OTP tới email của tài khoản.</summary>
    Task<RequestResetResult> RequestAsync(string email, CancellationToken ct = default);

    /// <summary>Bước 2: chỉ kiểm tra mã OTP (không tiêu thụ mã). Mã vẫn được tiêu thụ ở bước 3.</summary>
    Task<ResetPasswordStatus> VerifyOtpAsync(string email, string code, CancellationToken ct = default);

    /// <summary>Bước 3: kiểm tra lại mã OTP và đặt mật khẩu mới (một transaction).</summary>
    Task<ResetPasswordStatus> ResetAsync(string email, string code, string newPassword, CancellationToken ct = default);
}
