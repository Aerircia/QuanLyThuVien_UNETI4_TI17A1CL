// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: ViewModel cho trang chủ (landing) và thẻ sách dùng chung

using QuanLyThuVien_UNETI4_TI17A1CL.Models;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;

namespace QuanLyThuVien_UNETI4_TI17A1CL.ViewModels;

/// <summary>Một đầu sách để hiển thị dạng thẻ/dòng. Số lượng còn lại được tính từ DangGiu và DaMat (LINQ), không lưu cột.</summary>
public sealed class BookCardItem
{
    public int MaSach { get; set; }
    public string TenSach { get; set; } = string.Empty;
    public string TacGia { get; set; } = string.Empty;
    public int NamXuatBan { get; set; }
    public int MaTheLoai { get; set; }
    public string TenTheLoai { get; set; } = string.Empty;
    public int SoLuongTong { get; set; }
    public decimal GiaBia { get; set; }
    public string? ViTriKe { get; set; }
    public TrangThaiSach TrangThai { get; set; }
    public string? ImgPath { get; set; }

    /// <summary>Số phiếu Đã duyệt + Đang mượn của đầu sách.</summary>
    public int DangGiu { get; set; }

    /// <summary>Số bản đã ghi nhận Mất khi trả.</summary>
    public int DaMat { get; set; }

    public int SoLuongConLai => Math.Max(0, SoLuongTong - DangGiu - DaMat);
    public bool ConSach => TrangThai == TrangThaiSach.DangPhucVu && SoLuongConLai > 0;

    public string BadgeText => TrangThai switch
    {
        TrangThaiSach.TamNgung => "Tạm ngưng",
        TrangThaiSach.NgungPhucVu => "Ngừng phục vụ",
        _ => SoLuongConLai > 0 ? $"Còn {SoLuongConLai}/{SoLuongTong}" : "Hết sách"
    };

    public string BadgeClass => TrangThai switch
    {
        TrangThaiSach.TamNgung => "badge-soft-warning",
        TrangThaiSach.NgungPhucVu => "badge-soft-secondary",
        _ => SoLuongConLai > 0 ? "badge-soft-success" : "badge-soft-danger"
    };
}

public sealed class CategoryItem
{
    public int MaTheLoai { get; set; }
    public string TenTheLoai { get; set; } = string.Empty;
    public int SoDauSach { get; set; }
}

public sealed class AuthorItem
{
    public string TenTacGia { get; set; } = string.Empty;
    public int SoDauSach { get; set; }
    public int SoLuotMuon { get; set; }

    /// <summary>Tối đa 3 đầu sách của tác giả, dùng cho thẻ bìa xếp chồng.</summary>
    public List<BookCardItem> Books { get; set; } = new();
}

public sealed class RankedBookItem
{
    public BookCardItem Book { get; set; } = default!;
    public int SoLuotMuon { get; set; }
}

public sealed class LandingViewModel
{
    public int TongDauSach { get; set; }
    public int TongBanSach { get; set; }
    public int TongBanDoc { get; set; }
    public int TongLuotMuon { get; set; }

    /// <summary>Số đầu sách đang phục vụ và còn bản (hiển thị ở hero).</summary>
    public int SoDauSachSanSang { get; set; }

    /// <summary>3 bìa xếp quạt ở hero: ưu tiên sách được mượn nhiều.</summary>
    public List<BookCardItem> HeroBooks { get; set; } = new();

    public List<CategoryItem> TheLoais { get; set; } = new();
    public List<RankedBookItem> MuonNhieu { get; set; } = new();
    public List<BookCardItem> SachMoi { get; set; } = new();
    public List<AuthorItem> TacGiaNoiBat { get; set; } = new();
    public BookCardItem? SachCuaNgay { get; set; }
    public List<string> GoiY { get; set; } = new();

    /// <summary>Quy định mượn/trả/phạt lấy từ CauHinhThuVien để FAQ luôn khớp cấu hình.</summary>
    public CauHinhThuVien CauHinh { get; set; } = new();
}
