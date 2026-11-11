// Họ và tên: Phùng Ngọc Bảo
// Mã sinh viên: 23103100278
// Nội dung thực hiện: Đồng hồ và các tiện ích liên quan đến thời gian, giờ Việt Nam, lập lịch 00:00
using System;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services.Common;
public static class LibraryClock
{
    private static readonly TimeZoneInfo VietnamTimeZone = ResolveVietnamTimeZone();

    public static DateTimeOffset Now(TimeProvider? timeProvider = null)
    {
        timeProvider ??= TimeProvider.System;
        return TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), VietnamTimeZone);
    }

    public static DateTimeOffset ToVietnamTime(DateTimeOffset value) =>
        TimeZoneInfo.ConvertTime(value, VietnamTimeZone);

    // Chuyển DateTime lưu UTC trong SQL Server sang giờ Việt Nam
    public static DateTimeOffset ToVietnamTime(DateTime utcValue)
    {
        var utc = DateTime.SpecifyKind(utcValue, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTime(new DateTimeOffset(utc), VietnamTimeZone);
    }

    public static DateTime LocalDateTime(TimeProvider? timeProvider = null) => Now(timeProvider).DateTime;

    public static DateOnly Today(TimeProvider? timeProvider = null) => DateOnly.FromDateTime(LocalDateTime(timeProvider));

    // Thời điểm 00:00 kế tiếp theo giờ Việt Nam, trả về UTC để lập lịch chính xác
    public static DateTimeOffset NextMidnightUtc(TimeProvider? timeProvider = null)
    {
        timeProvider ??= TimeProvider.System;
        var localNow = Now(timeProvider);
        var nextLocalMidnight = DateTime.SpecifyKind(localNow.Date.AddDays(1), DateTimeKind.Unspecified);
        var nextMidnightUtc = TimeZoneInfo.ConvertTimeToUtc(nextLocalMidnight, VietnamTimeZone);
        return new DateTimeOffset(nextMidnightUtc);
    }

    public static TimeSpan DelayUntilNextMidnight(TimeProvider? timeProvider = null)
    {
        timeProvider ??= TimeProvider.System;
        var delay = NextMidnightUtc(timeProvider) - timeProvider.GetUtcNow();
        return delay > TimeSpan.Zero ? delay : TimeSpan.Zero;
    }

    private static TimeZoneInfo ResolveVietnamTimeZone()
    {
        // IANA cho Linux/container, Windows ID là phương án dự phòng
        try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh"); }
        catch (TimeZoneNotFoundException) { }
        catch (InvalidTimeZoneException) { }

        try { return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); }
        catch (TimeZoneNotFoundException) { }
        catch (InvalidTimeZoneException) { }

        // Việt Nam hiện dùng UTC+07:00 và không áp dụng DST
        return TimeZoneInfo.CreateCustomTimeZone(
            "Asia/Ho_Chi_Minh", TimeSpan.FromHours(7), "Giờ Việt Nam", "Giờ Việt Nam");
    }
}
