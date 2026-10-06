// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Xây dựng view model cho chức năng quên mật khẩu

using System.ComponentModel.DataAnnotations;

namespace QuanLyThuVien_UNETI4_TI17A1CL.ViewModels;

public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    [StringLength(255, ErrorMessage = "Email tối đa {1} ký tự.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;
}
