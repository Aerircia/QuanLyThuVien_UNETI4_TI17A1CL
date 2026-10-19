// Họ và tên: Nguyễn Hữu Tuấn
// Mã sinh viên: 23103100136
// Nội dung thực hiện: ViewModel hiển thị tra cứu danh mục sách trên trang chủ

using QuanLyThuVien_UNETI4_TI17A1CL.Models;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;

namespace QuanLyThuVien_UNETI4_TI17A1CL.ViewModels;

public sealed class BookItemViewModel
{
    public int MaSach { get; set; }
    public string TenSach { get; set; } = string.Empty;
    public string TacGia { get; set; } = string.Empty;
    public int MaTheLoai { get; set; }
    public string? TenTheLoai { get; set; }
    public string? NhaXuatBan { get; set; }
    public int NamXuatBan { get; set; }
    public int SoLuongTong { get; set; }
    public int SoLuongConLai { get; set; }
    public decimal GiaBia { get; set; }
    public string? ViTriKe { get; set; }
    public string? MoTa { get; set; }
    public TrangThaiSach TrangThai { get; set; }

    public bool ConSach => TrangThai == TrangThaiSach.DangPhucVu && SoLuongConLai > 0;
}

public sealed class BookCatalogViewModel
{
    public string? Search { get; set; }
    public int? MaTheLoai { get; set; }
    public bool? ConSach { get; set; }

    public List<TheLoai> TheLoais { get; set; } = new();
    public List<BookItemViewModel> Books { get; set; } = new();

    public int TongSoDauSach { get; set; }
    public int TongSoBanSach { get; set; }
    public int SoDauSachSanSang { get; set; }
}
