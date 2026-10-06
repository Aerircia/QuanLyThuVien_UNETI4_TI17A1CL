// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Xây dựng interface cho dịch vụ OTP

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;

public enum OtpIssueStatus
{
    Issued,
    CooldownActive,
    HourlyLimitReached
}

/// <param name="Code">Mã OTP (chỉ có khi Status = Issued).</param>
/// <param name="RetryAfterSeconds">Số giây phải chờ khi bị giới hạn.</param>
public sealed record OtpIssueResult(OtpIssueStatus Status, string? Code = null, int RetryAfterSeconds = 0);

public enum OtpVerifyStatus
{
    Valid,
    /// <summary>Sai mã hoặc mã đã hết hạn / đã dùng.</summary>
    Invalid,
    TooManyAttempts
}

public interface IOtpService
{
    /// <summary>
    /// Tạo mã OTP mới cho email (vô hiệu các mã cũ chưa dùng), có kiểm tra thời gian chờ và giới hạn mỗi giờ.
    /// </summary>
    Task<OtpIssueResult> IssueAsync(string email, CancellationToken ct = default);

    /// <summary>
    /// Kiểm tra mã và đánh dấu đã dùng trong một lệnh UPDATE duy nhất (mã chỉ dùng được một lần).
    /// Chạy được bên trong transaction của người gọi.
    /// </summary>
    Task<OtpVerifyStatus> VerifyAndConsumeAsync(string email, string code, CancellationToken ct = default);
}
