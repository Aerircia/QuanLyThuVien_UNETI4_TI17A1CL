// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Record thông tin người dùng đã xác thực trong request hiện tại.

using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services.Auth;

public sealed record CurrentUser(
    int MaTaiKhoan,
    string HoTen,
    VaiTro VaiTro,
    string TenDangNhap = "",
    string Email = "");
