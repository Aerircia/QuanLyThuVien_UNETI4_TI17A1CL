// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Xây dựng interface cho việc gửi email

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;

public interface IEmailSender
{
    /// <summary>Gửi email văn bản thuần. Ném exception nếu gửi thất bại.</summary>
    Task SendAsync(string toAddress, string subject, string body, CancellationToken ct = default);
    Task SendHtmlAsync(string toAddress, string subject, string textBody, string htmlBody, CancellationToken ct = default);
}
