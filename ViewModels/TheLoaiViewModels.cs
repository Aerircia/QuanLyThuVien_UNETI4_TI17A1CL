// Họ và tên: Vũ Tiến Thành
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Tạo ViewModel danh sách, chi tiết và form thêm/sửa thể loại, không bind trực tiếp entity.

using System.ComponentModel.DataAnnotations;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;

namespace QuanLyThuVien_UNETI4_TI17A1CL.ViewModels;

public sealed class TheLoaiIndexViewModel
{
    public string? TuKhoa { get; init; }
    public PaginatedList<TheLoaiListItemViewModel> TheLoais { get; init; } = default!;
}

public sealed record TheLoaiListItemViewModel(
    int MaTheLoai,
    string TenTheLoai,
    string? MoTa,
    string? KhuVucLuuTru,
    TrangThaiTheLoai TrangThai,
    int SoLuongSach);

public sealed class TheLoaiFormViewModel
{
    public int MaTheLoai { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên thể loại")]
    [StringLength(255, ErrorMessage = "Tên thể loại không được vượt quá 255 ký tự")]
    [Display(Name = "Tên thể loại")]
    public string TenTheLoai { get; set; } = string.Empty;

    [StringLength(255, ErrorMessage = "Mô tả không được vượt quá 255 ký tự")]
    [Display(Name = "Mô tả")]
    public string? MoTa { get; set; }

    [StringLength(100, ErrorMessage = "Khu vực lưu trữ không được vượt quá 100 ký tự")]
    [Display(Name = "Khu vực lưu trữ")]
    public string? KhuVucLuuTru { get; set; }

    [Display(Name = "Trạng thái")]
    public TrangThaiTheLoai TrangThai { get; set; } = TrangThaiTheLoai.HoatDong;
}

public sealed class TheLoaiDetailsViewModel
{
    public int MaTheLoai { get; init; }
    public string TenTheLoai { get; init; } = string.Empty;
    public string? MoTa { get; init; }
    public string? KhuVucLuuTru { get; init; }
    public TrangThaiTheLoai TrangThai { get; init; }
    public int SoLuongSach { get; init; }
}
