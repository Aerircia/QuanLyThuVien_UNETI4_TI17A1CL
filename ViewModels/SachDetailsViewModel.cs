// Họ và tên: Vũ Tiến Thành
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Khai báo ViewModel chi tiết sách gồm vị trí kệ cùng dữ liệu xác nhận xóa và đổi trạng thái.

using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;

namespace QuanLyThuVien_UNETI4_TI17A1CL.ViewModels;

public sealed class SachDetailsViewModel
{
    public int MaSach { get; init; }
    public string TenSach { get; init; } = string.Empty;
    public string TacGia { get; init; } = string.Empty;
    public string? ISBN { get; init; }
    public string? NhaXuatBan { get; init; }
    public string? ViTriKe { get; init; }
    public int NamXuatBan { get; init; }
    public string TenTheLoai { get; init; } = string.Empty;
    public int MaTheLoai { get; init; }
    public string? MoTa { get; init; }
    public string? ImgPath { get; init; }
    public int SoLuongTong { get; init; }
    public int SoLuongConLai { get; init; }
    public TrangThaiSach TrangThai { get; init; }
}

public sealed class SachChangeStatusViewModel
{
    public int MaSach { get; init; }
    public string TenSach { get; init; } = string.Empty;
    public TrangThaiSach TrangThai { get; set; }
}

public sealed class SachDeleteViewModel
{
    public int MaSach { get; init; }
    public string TenSach { get; init; } = string.Empty;
    public string TacGia { get; init; } = string.Empty;
    public string? NhaXuatBan { get; init; }
    public string TenTheLoai { get; init; } = string.Empty;
    public int SoLuongTong { get; init; }
    public TrangThaiSach TrangThai { get; init; }
    public string? ImgPath { get; init; }
    public bool CoPhieuDangXuLy { get; set; }
    public bool CoLichSuMuon { get; set; }
}
