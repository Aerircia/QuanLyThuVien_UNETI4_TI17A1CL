// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Xây dựng lớp SmtpEmailSender để gửi email qua SMTP
// Thêm giới hạn thời gian gửi (10 giây) để SMTP lỗi không làm treo yêu cầu

using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services.Email;

public class SmtpEmailSender : IEmailSender
{
    // Mặc định MailKit chờ 2 phút khi máy chủ SMTP không phản hồi.
    private const int TimeoutMs = 10_000;

    private readonly EmailOptions _options;

    public SmtpEmailSender(IOptions<EmailOptions> options)
    {
        _options = options.Value;
    }

    public Task SendAsync(string toAddress, string subject, string body, CancellationToken ct = default) =>
        SendCoreAsync(toAddress, subject, new TextPart("plain") { Text = body }, ct);

    public Task SendHtmlAsync(string toAddress, string subject, string textBody, string htmlBody, CancellationToken ct = default) =>
        SendCoreAsync(toAddress, subject, new MultipartAlternative { new TextPart("plain") { Text = textBody }, new TextPart("html") { Text = htmlBody } }, ct);

    private async Task SendCoreAsync(string toAddress, string subject, MimeEntity body, CancellationToken ct)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        message.To.Add(MailboxAddress.Parse(toAddress));
        message.Subject = subject;
        message.Body = body;

        // Giới hạn TỔNG thời gian kết nối + xác thực + gửi. Hết giờ thì ném TimeoutException
        // (không phải OperationCanceledException) để PasswordResetService xử lý như một lỗi gửi mail.
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeoutMs);

        try
        {
            using var client = new SmtpClient { Timeout = TimeoutMs };
            await client.ConnectAsync(
                _options.Host,
                _options.Port,
                _options.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto,
                timeout.Token);

            if (!string.IsNullOrEmpty(_options.Username))
                await client.AuthenticateAsync(_options.Username, _options.Password, timeout.Token);

            await client.SendAsync(message, timeout.Token);
            await client.DisconnectAsync(true, timeout.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested && timeout.IsCancellationRequested)
        {
            throw new TimeoutException($"Gửi email vượt quá {TimeoutMs / 1000} giây.");
        }
    }
}
