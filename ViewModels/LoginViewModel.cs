// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Xây dựng ViewModel để đăng nhập
// Đăng nhập bằng tên đăng nhập hoặc email

using System.ComponentModel.DataAnnotations;

namespace QuanLyThuVien_UNETI4_TI17A1CL.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
    [StringLength(255, ErrorMessage = "Tên đăng nhập tối đa {1} ký tự.")]
    [Display(Name = "Tên đăng nhập hoặc email")]
    public string TenDangNhap { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string MatKhau { get; set; } = string.Empty;

    /// <summary>Địa chỉ quay lại sau khi đăng nhập; chỉ chấp nhận URL nội bộ (kiểm tra ở controller).</summary>
    public string? ReturnUrl { get; set; }
}

