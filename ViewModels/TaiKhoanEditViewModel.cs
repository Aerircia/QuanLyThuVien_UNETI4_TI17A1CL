// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Tạo ViewModel cho việc tạo tài khoản người dùng trong ứng dụng quản lý thư viện.

using System.ComponentModel.DataAnnotations;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;

namespace QuanLyThuVien_UNETI4_TI17A1CL.ViewModels;

public class TaiKhoanEditViewModel
{
    public int MaTaiKhoan { get; set; }

    /// <summary>Chỉ để hiển thị, không sửa được (controller nạp lại từ DB).</summary>
    [Display(Name = "Tên đăng nhập")]
    public string TenDangNhap { get; set; } = string.Empty;

    /// <summary>Chỉ để hiển thị; quy tắc "không đổi vai trò" do service kiểm tra.</summary>
    public bool CoHoSoBanDoc { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
    [StringLength(255, ErrorMessage = "Họ tên tối đa {1} ký tự.")]
    [Display(Name = "Họ tên")]
    public string HoTen { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    [StringLength(255, ErrorMessage = "Email tối đa {1} ký tự.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Vai trò")]
    public VaiTro VaiTro { get; set; }

    [Display(Name = "Trạng thái")]
    public TrangThaiTaiKhoan TrangThai { get; set; }
}
