// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Xây dựng lớp hỗ trợ phân trang

using Microsoft.EntityFrameworkCore;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services.Common;

public static class PagingDefaults
{
    public const int DefaultPageSize = 10;

    /// <summary>Các cỡ trang được phép chọn.</summary>
    public static readonly IReadOnlyList<int> PageSizes = new[] { 10, 20, 30 };

    /// <summary>Cỡ trang không hợp lệ thì dùng mặc định.</summary>
    public static int NormalizePageSize(int pageSize) =>
        PageSizes.Contains(pageSize) ? pageSize : DefaultPageSize;
}

/// <summary>Một trang dữ liệu. Phân trang chạy trong SQL (Count + Skip/Take).</summary>
public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; }
    public int Page { get; }
    public int PageSize { get; }
    public int TotalCount { get; }
    public int TotalPages { get; }
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;

    private PagedResult(IReadOnlyList<T> items, int page, int pageSize, int totalCount)
    {
        Items = items;
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
        TotalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
    }

    /// <summary>
    /// Truy vấn PHẢI đã có OrderBy ổn định. Trang vượt quá cuối được kéo về trang cuối.
    /// </summary>
    public static async Task<PagedResult<T>> CreateAsync(
        IQueryable<T> orderedQuery, int page, int pageSize, CancellationToken ct = default)
    {
        pageSize = PagingDefaults.NormalizePageSize(pageSize);

        var total = await orderedQuery.CountAsync(ct);
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        page = Math.Clamp(page, 1, totalPages);

        var items = await orderedQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<T>(items, page, pageSize, total);
    }
}
