// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Xây dựng Service đặt lại mật khẩu

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QuanLyThuVien_UNETI4_TI17A1CL.Data;
using QuanLyThuVien_UNETI4_TI17A1CL.Models;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Auth;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services;

public class PasswordResetService : IPasswordResetService
{
    private readonly AppDbContext _db;
    private readonly IOtpService _otp;
    private readonly IEmailSender _email;
    private readonly IPasswordHasher<TaiKhoan> _hasher;
    private readonly OtpOptions _otpOptions;
    private readonly ILogger<PasswordResetService> _logger;

    public PasswordResetService(
        AppDbContext db,
        IOtpService otp,
        IEmailSender email,
        IPasswordHasher<TaiKhoan> hasher,
        IOptions<OtpOptions> otpOptions,
        ILogger<PasswordResetService> logger)
    {
        _db = db;
        _otp = otp;
        _email = email;
        _hasher = hasher;
        _otpOptions = otpOptions.Value;
        _logger = logger;
    }

    public async Task<RequestResetResult> RequestAsync(string email, CancellationToken ct = default)
    {
        var tk = await FindByEmailAsync(email, ct);
        if (tk is null)
            return new RequestResetResult(RequestResetStatus.EmailNotFound);

        if (tk.TrangThai == TrangThaiTaiKhoan.BiKhoa)
            return new RequestResetResult(RequestResetStatus.AccountLocked);

        var issue = await _otp.IssueAsync(tk.Email, ct);
        switch (issue.Status)
        {
            case OtpIssueStatus.CooldownActive:
                return new RequestResetResult(RequestResetStatus.CooldownActive, issue.RetryAfterSeconds);
            case OtpIssueStatus.HourlyLimitReached:
                return new RequestResetResult(RequestResetStatus.HourlyLimitReached, issue.RetryAfterSeconds);
        }

        var body =
            $"Xin chào {tk.HoTen},{Environment.NewLine}{Environment.NewLine}" +
            $"Mã OTP đặt lại mật khẩu của bạn là: {issue.Code}{Environment.NewLine}" +
            $"Mã có hiệu lực trong {_otpOptions.LifetimeMinutes} phút và chỉ dùng được một lần.{Environment.NewLine}{Environment.NewLine}" +
            "Nếu bạn không yêu cầu đặt lại mật khẩu, hãy bỏ qua email này.";

        try
        {
            await _email.SendAsync(tk.Email, "Mã OTP đặt lại mật khẩu", body, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Không gửi được email OTP cho tài khoản {MaTaiKhoan}", tk.MaTaiKhoan);
            return new RequestResetResult(RequestResetStatus.EmailSendFailed);
        }

        return new RequestResetResult(RequestResetStatus.Sent);
    }

    public async Task<ResetPasswordStatus> ResetAsync(
        string email, string code, string newPassword, CancellationToken ct = default)
    {
        var tk = await FindByEmailAsync(email, ct);
        if (tk is null)
            return ResetPasswordStatus.EmailNotFound;

        if (tk.TrangThai == TrangThaiTaiKhoan.BiKhoa)
            return ResetPasswordStatus.AccountLocked;

        // Đánh dấu OTP đã dùng và lưu mật khẩu mới trong CÙNG một transaction:
        // lưu mật khẩu lỗi thì mã không bị "đốt".
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var verify = await _otp.VerifyAndConsumeAsync(tk.Email, code, ct);
        if (verify != OtpVerifyStatus.Valid)
        {
            // Commit để giữ lại việc vô hiệu mã khi sai quá số lần.
            await tx.CommitAsync(ct);
            return verify == OtpVerifyStatus.TooManyAttempts
                ? ResetPasswordStatus.TooManyAttempts
                : ResetPasswordStatus.InvalidOrExpiredCode;
        }

        tk.MatKhau = _hasher.HashPassword(tk, newPassword);
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return ResetPasswordStatus.Success;
    }

    /// <summary>Tìm đúng MỘT tài khoản theo email; trùng email (dữ liệu cũ) thì coi như không tìm thấy.</summary>
    private async Task<TaiKhoan?> FindByEmailAsync(string email, CancellationToken ct)
    {
        email = (email ?? string.Empty).Trim();
        if (email.Length == 0) return null;

        var matches = await _db.Set<TaiKhoan>()
            .Where(t => t.Email == email)
            .Take(2)
            .ToListAsync(ct);

        if (matches.Count > 1)
        {
            _logger.LogWarning("Có nhiều tài khoản dùng chung email khi đặt lại mật khẩu; bỏ qua yêu cầu.");
            return null;
        }

        return matches.Count == 1 ? matches[0] : null;
    }
}
