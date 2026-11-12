// Họ và tên: Vũ Ngọc Hương Giang
// Mã sinh viên: 23103100280
// Nội dung thực hiện: ViewModel cho trang quản lý và gửi thông báo của Admin/Thủ thư.

using System.ComponentModel.DataAnnotations;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Common;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;

namespace QuanLyThuVien_UNETI4_TI17A1CL.ViewModels;

public class QuanLyThongBaoViewModel
{
    public string? Search { get; set; }
    public string? Loai { get; set; }
    public string? NhomNhan { get; set; }
    public bool? DangHienThi { get; set; }
    public int PageSize { get; set; }
    public PagedResult<QuanLyThongBaoItem> Result { get; set; } = null!;
    public PagerViewModel Pager { get; set; } = null!;
}

public class GuiThongBaoViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tiêu đề")]
    [StringLength(120, ErrorMessage = "Tiêu đề tối đa 120 ký tự")]
    [Display(Name = "Tiêu đề")]
    public string TieuDe { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập nội dung")]
    [StringLength(1000, ErrorMessage = "Nội dung tối đa 1000 ký tự")]
    [Display(Name = "Nội dung")]
    public string NoiDung { get; set; } = string.Empty;

    [Display(Name = "Mức độ")]
    public string MucDo { get; set; } = "in";

    [Display(Name = "Người nhận")]
    public string NhomNhan { get; set; } = "BanDoc";
}
