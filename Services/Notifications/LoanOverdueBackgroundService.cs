// Họ và tên: Phùng Ngọc Bảo
// Mã sinh viên: 23103100278
// Nội dung thực hiện: Tự động kiểm tra và hủy phiếu đúng 00:00 ngày hết hạn nhận sách
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Common;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services.Notifications;
public sealed class LoanOverdueBackgroundService : BackgroundService
{
    private static readonly TimeSpan FailureRetryInterval = TimeSpan.FromMinutes(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LoanOverdueBackgroundService> _logger;
    private readonly TimeProvider _timeProvider;

    public LoanOverdueBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<LoanOverdueBackgroundService> logger,
        TimeProvider timeProvider)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Chụp mốc 00:00 trước khi chạy bù. Nếu khởi động lúc 23:59 nhưng xử lý
        // ban đầu kéo dài qua 00:00, vòng lặp vẫn chạy ngay thay vì lỡ cả một ngày
        var nextMidnight = LibraryClock.NextMidnightUtc(_timeProvider);

        // Chạy bù các phiếu đã đến ngày hết hạn nếu ứng dụng khởi động sau 00:00
        await ProcessWithRetryAsync("khi ứng dụng khởi động", stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = nextMidnight - _timeProvider.GetUtcNow();
            if (delay < TimeSpan.Zero) delay = TimeSpan.Zero;

            _logger.LogInformation(
                "Lịch tự hủy phiếu mượn tiếp theo: {NextMidnight} (giờ Việt Nam).",
                LibraryClock.ToVietnamTime(nextMidnight));

            try
            {
                await Task.Delay(delay, _timeProvider, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            await ProcessWithRetryAsync("mốc 00:00 ngày hết hạn", stoppingToken);
            // Cộng đúng một ngày vào mốc đã lập lịch. Nếu lần xử lý kéo dài qua
            // nửa đêm tiếp theo, vòng lặp sẽ chạy bù ngay thay vì bỏ lỡ ngày đó
            nextMidnight = nextMidnight.AddDays(1);
        }
    }

    private async Task ProcessWithRetryAsync(string reason, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var loans = scope.ServiceProvider.GetRequiredService<IPhieuMuonService>();
                var count = await loans.AutoCancelOverdueApprovedLoansAsync(ct);

                if (count > 0)
                    _logger.LogInformation(
                        "Đã tự động hủy {Count} phiếu mượn {Reason}.", count, reason);
                else
                    _logger.LogInformation(
                        "Không có phiếu mượn cần tự động hủy {Reason}.", reason);

                return;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Lỗi khi kiểm tra phiếu mượn {Reason}. Sẽ thử lại sau {RetryMinutes} phút.",
                    reason, FailureRetryInterval.TotalMinutes);

                try
                {
                    await Task.Delay(FailureRetryInterval, _timeProvider, ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    return;
                }
            }
        }
    }
}
