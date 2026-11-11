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
                await sender.SendHtmlAsync(item.To, item.Subject, item.TextBody, item.HtmlBody, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { _logger.LogError(ex, "Gửi email thông báo thất bại tới {Recipient}; email không chặn nghiệp vụ.", item.To); }
        }
    }
}
