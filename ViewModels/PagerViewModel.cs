// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Xây dựng view model cho chức năng phân trang

using QuanLyThuVien_UNETI4_TI17A1CL.Services.Common;

namespace QuanLyThuVien_UNETI4_TI17A1CL.ViewModels;

/// <summary>Dữ liệu cho Views/Shared/_PagerPartial; dùng lại cho mọi danh sách có phân trang.</summary>
public class PagerViewModel
{
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
    public int TotalCount { get; init; }
    public string Controller { get; init; } = string.Empty;
    public string Action { get; init; } = "Index";

    /// <summary>Điều kiện tìm kiếm/lọc/sắp xếp cần giữ khi chuyển trang (không gồm page, pageSize).</summary>
    public Dictionary<string, string> RouteValues { get; init; } = new();

    public static PagerViewModel From<T>(
        PagedResult<T> result, string controller, string action, Dictionary<string, string> routeValues) => new()
    {
        Page = result.Page,
        PageSize = result.PageSize,
        TotalPages = result.TotalPages,
        TotalCount = result.TotalCount,
        Controller = controller,
        Action = action,
        RouteValues = routeValues
    };
}
