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
    // Email OTP có kênh riêng để thư thông báo hàng loạt không làm chậm hoặc đẩy lùi mã đặt lại mật khẩu.
    private readonly Channel<EmailQueueItem> _urgent = Channel.CreateBounded<EmailQueueItem>(new BoundedChannelOptions(100)
    { FullMode = BoundedChannelFullMode.Wait, SingleReader = true, SingleWriter = false });
    private readonly Channel<EmailQueueItem> _normal = Channel.CreateBounded<EmailQueueItem>(new BoundedChannelOptions(500)
    { FullMode = BoundedChannelFullMode.Wait, SingleReader = true, SingleWriter = false });

    public ValueTask EnqueueAsync(EmailQueueItem item, CancellationToken ct = default) =>
        (item.RevokeOtpOnFailure is null ? _normal : _urgent).Writer.WriteAsync(item, ct);

    public async ValueTask<EmailQueueItem> DequeueAsync(CancellationToken ct)
    {
        while (true)
        {
            if (_urgent.Reader.TryRead(out var urgent)) return urgent;
            if (_normal.Reader.TryRead(out var normal)) return normal;

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            var waitUrgent = _urgent.Reader.WaitToReadAsync(cts.Token).AsTask();
            var waitNormal = _normal.Reader.WaitToReadAsync(cts.Token).AsTask();
            await Task.WhenAny(waitUrgent, waitNormal);
            cts.Cancel();
        }
    }
}
