// Họ và tên: Vũ Tiến Thành
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Khai báo bộ lọc, dữ liệu phân trang sách gồm vị trí kệ và danh sách thể loại.

using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;

namespace QuanLyThuVien_UNETI4_TI17A1CL.ViewModels;

public enum SapXepSach
{
    TenSach,
    TacGia,
    NamXuatBan,
    SoLuongConLai
}

public sealed class SachIndexViewModel
{
    public string? TuKhoa { get; set; }
    public string? TuKhoaTheLoai { get; set; }
    public int? TheLoaiId { get; set; }
    public bool? ConSach { get; set; }
    public TrangThaiSach? TrangThai { get; set; }
    public SapXepSach SapXep { get; set; } = SapXepSach.TenSach;
    public bool GiamDan { get; set; }
    public int Page { get; set; } = 1;
    public PaginatedList<SachIndexItemViewModel> Sach { get; set; } = default!;
    public IReadOnlyList<TheLoaiOptionViewModel> TheLoaiOptions { get; set; } = [];
}

public sealed record SachIndexItemViewModel(
    int MaSach,
    string TenSach,
    string TacGia,
    string? ISBN,
    string? NhaXuatBan,
    string? ViTriKe,
    int NamXuatBan,
    int SoLuongTong,
    int SoLuongConLai,
    TrangThaiSach TrangThai,
    string? ImgPath,
    int MaTheLoai,
    string TenTheLoai);

public sealed record TheLoaiOptionViewModel(int MaTheLoai, string TenTheLoai, int SoLuongSach);
