// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Tạo ViewModel ResetPasswordViewModel để nhận dữ liệu từ người dùng khi họ muốn đặt lại mật khẩu.

using System.ComponentModel.DataAnnotations;

namespace QuanLyThuVien_UNETI4_TI17A1CL.ViewModels;

public class ResetPasswordViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    [StringLength(255, ErrorMessage = "Email tối đa {1} ký tự.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mã OTP.")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Mã OTP gồm đúng 6 chữ số.")]
    [Display(Name = "Mã OTP")]
    public string MaOtp { get; set; } = string.Empty;

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
