// Họ và tên: Phùng Ngọc Bảo
// Mã sinh viên: 23103100278
// Nội dung thực hiện: Xây dựng background service gửi email thông báo
using Microsoft.Extensions.DependencyInjection;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services.Notifications;

public sealed class EmailNotificationBackgroundService : BackgroundService
{
    private readonly IEmailQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmailNotificationBackgroundService> _logger;
    public EmailNotificationBackgroundService(IEmailQueue queue, IServiceScopeFactory scopeFactory, ILogger<EmailNotificationBackgroundService> logger)
    { _queue = queue; _scopeFactory = scopeFactory; _logger = logger; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            EmailQueueItem item;
            try { item = await _queue.DequeueAsync(stoppingToken); }
            catch (OperationCanceledException) { break; }
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var sender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
                if (string.IsNullOrWhiteSpace(item.HtmlBody))
                    await sender.SendAsync(item.To, item.Subject, item.TextBody, stoppingToken);
                else
                    await sender.SendHtmlAsync(item.To, item.Subject, item.TextBody, item.HtmlBody, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gửi email thất bại tới {Recipient}.", item.To);
                if (item.RevokeOtpOnFailure is not null)
                {
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var otp = scope.ServiceProvider.GetRequiredService<IOtpService>();
                        await otp.RevokeAsync(item.To, item.RevokeOtpOnFailure, CancellationToken.None);
                    }
                    catch (Exception revokeError)
                    {
                        _logger.LogWarning(revokeError, "Không thu hồi được OTP sau khi gửi email thất bại.");
                    }
                }
            }
        }
    }
}
