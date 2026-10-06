// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Xây dựng data model TaiKhoan

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Models;

[Table("TaiKhoan")]
public class TaiKhoan
{
    [Key] public int MaTaiKhoan { get; set; }

    [Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]
    [StringLength(255)]
    [Display(Name = "Tên đăng nhập")]
    public string TenDangNhap { get; set; } = string.Empty;

    // Lưu hash (PasswordHasher), không lưu mật khẩu thô
    [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
    [StringLength(255)]
    [Display(Name = "Mật khẩu")]
    public string MatKhau { get; set; } = string.Empty;

    [Required, StringLength(255)]
    [Display(Name = "Họ tên")]
    public string HoTen { get; set; } = string.Empty;

    [Required, StringLength(255), EmailAddress(ErrorMessage = "Email không đúng định dạng")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Vai trò")]
    public VaiTro VaiTro { get; set; } = VaiTro.BanDoc;

    [Display(Name = "Trạng thái")]
    public TrangThaiTaiKhoan TrangThai { get; set; } = TrangThaiTaiKhoan.HoatDong;

    public BanDoc? BanDoc { get; set; }
}
