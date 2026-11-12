// Họ và tên: Phùng Ngọc Bảo
// Mã sinh viên: 23103100278
// Nội dung thực hiện: Xây dựng hàng đợi email thông báo
using System.Threading.Channels;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services.Notifications;

public sealed record EmailQueueItem(string To,string Subject,string TextBody,string HtmlBody,string? RevokeOtpOnFailure = null);

public interface IEmailQueue
{
    ValueTask EnqueueAsync(EmailQueueItem item, CancellationToken ct = default);
    ValueTask<EmailQueueItem> DequeueAsync(CancellationToken ct);
}

public sealed class EmailQueue : IEmailQueue
{
    private readonly Channel<EmailQueueItem> _channel = Channel.CreateBounded<EmailQueueItem>(new BoundedChannelOptions(500)
    { FullMode = BoundedChannelFullMode.Wait, SingleReader = true, SingleWriter = false });
    public ValueTask EnqueueAsync(EmailQueueItem item, CancellationToken ct = default) => _channel.Writer.WriteAsync(item, ct);
    public ValueTask<EmailQueueItem> DequeueAsync(CancellationToken ct) => _channel.Reader.ReadAsync(ct);
}
