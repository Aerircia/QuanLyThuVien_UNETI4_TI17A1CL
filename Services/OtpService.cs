// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Xây dựng Service tạo OTP.
// Thêm VerifyAsync: kiểm tra OTP không tiêu thụ mã (bước 2 quên mật khẩu)

using System.Globalization;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using QuanLyThuVien_UNETI4_TI17A1CL.Data;
using QuanLyThuVien_UNETI4_TI17A1CL.Models;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Auth;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services;

/// <summary>
/// Tạo và kiểm tra OTP trên bảng OTPs (không có cột thời điểm tạo và số lần thử).
/// - Thời điểm tạo được suy ra: ThoiGianHetHan - Lifetime (đổi Lifetime sẽ lệch nhẹ các mã đang tồn tại).
/// - Số lần nhập sai được đếm trong IMemoryCache (mất khi khởi động lại ứng dụng).
/// - Mã lưu dạng văn bản thuần vì cột chỉ rộng 6 ký tự: bù lại bằng thời hạn ngắn, dùng một lần và giới hạn số lần thử.
/// </summary>
public class OtpService : IOtpService
{
    private const int CodeLength = 6;

    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly OtpOptions _options;
    private readonly TimeProvider _clock;

    public OtpService(AppDbContext db, IMemoryCache cache, IOptions<OtpOptions> options, TimeProvider clock)
    {
        _db = db;
        _cache = cache;
        _options = options.Value;
        _clock = clock;
    }

    public async Task<OtpIssueResult> IssueAsync(string email, CancellationToken ct = default)
    {
        email = Normalize(email);
        var now = Now();
        var lifetime = _options.Lifetime;

        // Mã được tạo trong vòng 1 giờ qua <=> ThoiGianHetHan > (now - 1h) + lifetime.
        var threshold = now.AddHours(-1) + lifetime;
        var expiries = await _db.Set<Otp>()
            .AsNoTracking()
            .Where(o => o.Email == email && o.ThoiGianHetHan > threshold)
            .Select(o => o.ThoiGianHetHan)
            .ToListAsync(ct);

        var createdTimes = expiries.Select(e => e - lifetime).OrderBy(t => t).ToList();

        if (createdTimes.Count >= _options.MaxRequestsPerHour)
        {
            var wait = createdTimes[0].AddHours(1) - now;
            return new OtpIssueResult(OtpIssueStatus.HourlyLimitReached, null, CeilSeconds(wait));
        }

        if (createdTimes.Count > 0)
        {
            var cooldownEnds = createdTimes[^1] + _options.ResendCooldown;
            if (cooldownEnds > now)
                return new OtpIssueResult(OtpIssueStatus.CooldownActive, null, CeilSeconds(cooldownEnds - now));
        }

        // Dọn các bản ghi đã hết hạn quá 1 ngày.
        var purgeBefore = now.AddDays(-1);
        await _db.Set<Otp>().Where(o => o.ThoiGianHetHan < purgeBefore).ExecuteDeleteAsync(ct);

        // Vô hiệu các mã cũ chưa dùng của email này.
        await _db.Set<Otp>()
            .Where(o => o.Email == email && !o.TinhTrangSuDung)
            .ExecuteUpdateAsync(s => s.SetProperty(o => o.TinhTrangSuDung, true), ct);

        var code = RandomNumberGenerator.GetInt32(0, 1_000_000)
            .ToString("D" + CodeLength, CultureInfo.InvariantCulture);

        _db.Set<Otp>().Add(new Otp
        {
            Email = email,
            OtpCode = code,
            ThoiGianHetHan = now + lifetime,
            TinhTrangSuDung = false
        });
        await _db.SaveChangesAsync(ct);

        _cache.Remove(AttemptKey(email));
        return new OtpIssueResult(OtpIssueStatus.Issued, code);
    }

    public Task<OtpVerifyStatus> VerifyAsync(string email, string code, CancellationToken ct = default) =>
        VerifyCoreAsync(email, code, consume: false, ct);

    public Task<OtpVerifyStatus> VerifyAndConsumeAsync(string email, string code, CancellationToken ct = default) =>
        VerifyCoreAsync(email, code, consume: true, ct);

    private async Task<OtpVerifyStatus> VerifyCoreAsync(string email, string code, bool consume, CancellationToken ct)
    {
        email = Normalize(email);
        code = (code ?? string.Empty).Trim();

        var key = AttemptKey(email);
        var attempts = _cache.Get<int>(key);
        if (attempts >= _options.MaxVerifyAttempts)
            return OtpVerifyStatus.TooManyAttempts;

        var now = Now();
        var wellFormed = code.Length == CodeLength && code.All(char.IsAsciiDigit);

        if (wellFormed)
        {
            var matching = _db.Set<Otp>()
                .Where(o => o.Email == email
                            && o.OtpCode == code
                            && !o.TinhTrangSuDung
                            && o.ThoiGianHetHan > now);

            if (consume)
            {
                // Một lệnh UPDATE có điều kiện: hai request đồng thời không thể cùng dùng một mã.
                var consumed = await matching
                    .ExecuteUpdateAsync(s => s.SetProperty(o => o.TinhTrangSuDung, true), ct);

                if (consumed > 0)
                {
                    _cache.Remove(key);
                    return OtpVerifyStatus.Valid;
                }
            }
            else if (await matching.AnyAsync(ct))
            {
                // Chỉ kiểm tra (bước 2 của quên mật khẩu): chưa đánh dấu đã dùng, sai vẫn tính vào số lần thử.
                return OtpVerifyStatus.Valid;
            }
        }

        attempts++;
        _cache.Set(key, attempts, _options.Lifetime);

        if (attempts >= _options.MaxVerifyAttempts)
        {
            // Sai quá số lần cho phép: vô hiệu mã hiện tại, người dùng phải xin mã mới.
            await _db.Set<Otp>()
                .Where(o => o.Email == email && !o.TinhTrangSuDung)
                .ExecuteUpdateAsync(s => s.SetProperty(o => o.TinhTrangSuDung, true), ct);
            return OtpVerifyStatus.TooManyAttempts;
        }

        return OtpVerifyStatus.Invalid;
    }

    private DateTime Now() => _clock.GetUtcNow().UtcDateTime;

    private static string Normalize(string email) => (email ?? string.Empty).Trim();

    private static string AttemptKey(string email) => "otp:attempts:" + email.ToUpperInvariant();

    private static int CeilSeconds(TimeSpan span) => Math.Max(1, (int)Math.Ceiling(span.TotalSeconds));
}
