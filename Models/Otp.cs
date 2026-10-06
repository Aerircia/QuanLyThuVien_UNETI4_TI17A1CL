// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Xây dựng data model OTP

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Models;

// Mã OTP gửi qua email (độc lập với TaiKhoan, liên kết bằng Email)
[Table("OTPs")]
public class Otp
{
    [Key] public int MaOtp { get; set; }

    [Required, StringLength(320), EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(6, MinimumLength = 6)]
    [Display(Name = "Mã OTP")]
    public string OtpCode { get; set; } = string.Empty;

    // datetime2; dùng giờ nhất quán (DateTime.Now hoặc UtcNow) ở mọi nơi tạo/kiểm tra
    [Display(Name = "Thời gian hết hạn")]
    public DateTime ThoiGianHetHan { get; set; }

    [Display(Name = "Đã sử dụng")]
    public bool TinhTrangSuDung { get; set; }
}
