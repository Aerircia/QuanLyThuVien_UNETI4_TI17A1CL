// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Xây dựng lớp LogEmailSender để ghi log email (Development).

using QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services.Email;

/// <summary>
/// CHỈ DÙNG KHI PHÁT TRIỂN (đăng ký trong Program.cs khi chưa cấu hình SMTP và môi trường là Development):
/// ghi nội dung email, gồm cả mã OTP, vào log thay vì gửi đi.
/// </summary>
public class LogEmailSender : IEmailSender
{
    private readonly ILogger<LogEmailSender> _logger;

    public LogEmailSender(ILogger<LogEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string toAddress, string subject, string body, CancellationToken ct = default)
    {
        _logger.LogWarning("[DEV EMAIL - không gửi thật] Tới: {To} | Tiêu đề: {Subject}{NewLine}{Body}",
            toAddress, subject, Environment.NewLine, body);
        return Task.CompletedTask;
    }
}
