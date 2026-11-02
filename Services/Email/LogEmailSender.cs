// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Xây dựng lớp LogEmailSender để ghi log email (Development).
// Bổ sung: bọc SmtpEmailSender để gửi thật và ghi log kết quả ra terminal cùng lúc.

using QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services.Email;

/// <summary>
/// CHỈ DÙNG KHI PHÁT TRIỂN. Đăng ký bằng factory trong ServiceCollectionExtensions (không đăng ký trực tiếp
/// bằng AddScoped&lt;IEmailSender, LogEmailSender&gt; vì sẽ tự phụ thuộc vào chính nó).
/// - Không có <c>inner</c> (chưa cấu hình SMTP): chỉ ghi nội dung email, gồm cả mã OTP, vào log.
/// - Có <c>inner</c> (SmtpEmailSender): gửi thật trước; gửi xong mới ghi log email kèm OTP;
///   gửi lỗi thì ghi log lỗi (không in OTP) rồi ném lại exception để PasswordResetService xóa dòng OTP.
/// </summary>
public class LogEmailSender : IEmailSender
{
    private readonly ILogger<LogEmailSender> _logger;
    private readonly IEmailSender? _inner;

    public LogEmailSender(ILogger<LogEmailSender> logger, IEmailSender? inner = null)
    {
        _logger = logger;
        _inner = inner;
    }

    public async Task SendAsync(string toAddress, string subject, string body, CancellationToken ct = default)
    {
        if (_inner is null)
        {
            _logger.LogWarning("[DEV EMAIL - không gửi thật] Tới: {To} | Tiêu đề: {Subject}{NewLine}{Body}",
                toAddress, subject, Environment.NewLine, body);
            return;
        }

        try
        {
            await _inner.SendAsync(toAddress, subject, body, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError("[DEV EMAIL - GỬI THẤT BẠI] Tới: {To} | Tiêu đề: {Subject} | Lỗi: {Error}",
                toAddress, subject, ex.Message);
            throw;
        }

        _logger.LogWarning("[DEV EMAIL - đã gửi thật] Tới: {To} | Tiêu đề: {Subject}{NewLine}{Body}",
            toAddress, subject, Environment.NewLine, body);
    }
}
