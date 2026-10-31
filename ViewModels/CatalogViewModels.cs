// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: ViewModel cho trang tra cứu kho sách (tìm kiếm, lọc, sắp xếp, phân trang) và chi tiết sách

using System.Globalization;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Common;

namespace QuanLyThuVien_UNETI4_TI17A1CL.ViewModels;

public enum TonKhoFilter
{
    ConSach,
    HetSach
}

/// <summary>Điều kiện tra cứu, bind từ query string (GET) nên giữ nguyên khi chuyển trang.</summary>
public sealed class CatalogFilter
{
    public static readonly IReadOnlyList<(string Value, string Label)> SortOptions = new[]
    {
        ("az", "Tên sách A → Z"),
        ("za", "Tên sách Z → A"),
        ("newest", "Năm xuất bản mới nhất"),
        ("oldest", "Năm xuất bản cũ nhất"),
        ("stockdesc", "Số lượng còn lại giảm dần"),
        ("stockasc", "Số lượng còn lại tăng dần"),
        ("priceasc", "Giá bìa tăng dần"),
        ("pricedesc", "Giá bìa giảm dần")
    };

    public string? Search { get; set; }
    public int? MaTheLoai { get; set; }
    public string? TacGia { get; set; }
    public int? NamTu { get; set; }
    public int? NamDen { get; set; }
    public decimal? GiaTu { get; set; }
    public decimal? GiaDen { get; set; }
    public TrangThaiSach? TrangThai { get; set; }
    public TonKhoFilter? TonKho { get; set; }
    public string Sort { get; set; } = "az";

    /// <summary>"grid" hoặc "list".</summary>
    public string View { get; set; } = "grid";

    /// <summary>Số điều kiện đang dùng trong hộp "Nâng cao" (để hiện trên nút).</summary>
    public int AdvancedCount =>
        (TacGia is null ? 0 : 1) + (NamTu.HasValue || NamDen.HasValue ? 1 : 0) +
        (GiaTu.HasValue || GiaDen.HasValue ? 1 : 0) + (TrangThai.HasValue ? 1 : 0) + (TonKho.HasValue ? 1 : 0);

    public bool HasAny => Search is not null || MaTheLoai.HasValue || AdvancedCount > 0;

    /// <summary>Làm sạch dữ liệu người dùng nhập (không tin query string).</summary>
    public void Normalize()
    {
        Search = string.IsNullOrWhiteSpace(Search) ? null : Search.Trim();
        TacGia = string.IsNullOrWhiteSpace(TacGia) ? null : TacGia.Trim();
        if (MaTheLoai <= 0) MaTheLoai = null;
        if (TrangThai is { } t && !Enum.IsDefined(t)) TrangThai = null;
        if (TonKho is { } k && !Enum.IsDefined(k)) TonKho = null;
        if (!SortOptions.Any(o => o.Value == Sort)) Sort = "az";
        if (View != "list") View = "grid";
        if (NamTu <= 0) NamTu = null;
        if (NamDen <= 0) NamDen = null;
        if (GiaTu < 0) GiaTu = null;
        if (GiaDen < 0) GiaDen = null;
        if (NamTu > NamDen) (NamTu, NamDen) = (NamDen, NamTu);
        if (GiaTu > GiaDen) (GiaTu, GiaDen) = (GiaDen, GiaTu);
    }

    /// <summary>Các tham số đang dùng (không gồm page, pageSize) để _PagerPartial giữ lại khi chuyển trang.</summary>
    public Dictionary<string, string> ToRouteValues()
    {
        var d = new Dictionary<string, string>();
        if (Search is not null) d["search"] = Search;
        if (MaTheLoai.HasValue) d["maTheLoai"] = MaTheLoai.Value.ToString(CultureInfo.InvariantCulture);
        if (TacGia is not null) d["tacGia"] = TacGia;
        if (NamTu.HasValue) d["namTu"] = NamTu.Value.ToString(CultureInfo.InvariantCulture);
        if (NamDen.HasValue) d["namDen"] = NamDen.Value.ToString(CultureInfo.InvariantCulture);
        if (GiaTu.HasValue) d["giaTu"] = GiaTu.Value.ToString(CultureInfo.InvariantCulture);
        if (GiaDen.HasValue) d["giaDen"] = GiaDen.Value.ToString(CultureInfo.InvariantCulture);
        if (TrangThai.HasValue) d["trangThai"] = TrangThai.Value.ToString();
        if (TonKho.HasValue) d["tonKho"] = TonKho.Value.ToString();
        if (Sort != "az") d["sort"] = Sort;
        if (View != "grid") d["view"] = View;
        return d;
    }
}

public sealed class CatalogViewModel
{
    public CatalogFilter Filter { get; set; } = new();
    public PagedResult<BookCardItem> Result { get; set; } = default!;
    public PagerViewModel Pager { get; set; } = default!;
    public List<CategoryItem> TheLoais { get; set; } = new();
    public List<string> TacGias { get; set; } = new();
}

public sealed class BookDetailViewModel
{
    public BookCardItem Book { get; set; } = default!;
    public string? NhaXuatBan { get; set; }
    public string? MoTa { get; set; }
    public string? KhuVucLuuTru { get; set; }
    public List<BookCardItem> CungTheLoai { get; set; } = new();
}
