// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Xây dựng view model cho chức năng đặt lại mật khẩu của admin

using System.ComponentModel.DataAnnotations;

namespace QuanLyThuVien_UNETI4_TI17A1CL.ViewModels;

public class AdminResetPasswordViewModel
{
    public int MaTaiKhoan { get; set; }

    /// <summary>Chỉ để hiển thị.</summary>
    [Display(Name = "Tên đăng nhập")]
    public string TenDangNhap { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu mới phải từ {2} đến {1} ký tự.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu mới")]
    public string MatKhauMoi { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu mới.")]
    [Compare(nameof(MatKhauMoi), ErrorMessage = "Mật khẩu nhập lại không khớp.")]
    [DataType(DataType.Password)]
    [Display(Name = "Nhập lại mật khẩu mới")]
    public string XacNhanMatKhauMoi { get; set; } = string.Empty;
}
