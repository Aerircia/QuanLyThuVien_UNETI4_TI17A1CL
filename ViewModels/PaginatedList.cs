// Họ và tên: Vũ Tiến Thành
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Cung cấp helper phân trang bất đồng bộ trên IQueryable để đếm và lấy từng trang dữ liệu từ cơ sở dữ liệu.

using Microsoft.EntityFrameworkCore;

namespace QuanLyThuVien_UNETI4_TI17A1CL.ViewModels;

public sealed class PaginatedList<T>(IReadOnlyList<T> items, int pageIndex, int totalCount, int pageSize)
{
    public IReadOnlyList<T> Items { get; } = items;
    public int PageIndex { get; } = pageIndex;
    public int PageSize { get; } = pageSize;
    public int TotalCount { get; } = totalCount;
    public int TotalPages { get; } = (int)Math.Ceiling(totalCount / (double)pageSize);
    public bool HasPreviousPage => PageIndex > 1;
    public bool HasNextPage => PageIndex < TotalPages;

    public static async Task<PaginatedList<T>> CreateAsync(
        IQueryable<T> query,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var safePageSize = Math.Max(pageSize, 1);
        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)safePageSize);
        var safePageIndex = Math.Clamp(pageIndex, 1, Math.Max(totalPages, 1));
        var items = await query.Skip((safePageIndex - 1) * safePageSize)
            .Take(safePageSize)
            .ToListAsync(cancellationToken);
        return new PaginatedList<T>(items, safePageIndex, totalCount, safePageSize);
    }
}
