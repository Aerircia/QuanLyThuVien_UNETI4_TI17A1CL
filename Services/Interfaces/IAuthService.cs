// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Xây dựng interface IAuthService để định nghĩa các phương thức xác thực người dùng.
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Auth;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;

public interface IAuthService
{
    /// <summary>Kiểm tra tên đăng nhập + mật khẩu và trạng thái tài khoản.</summary>
    Task<LoginResult> LoginAsync(string tenDangNhap, string matKhau, CancellationToken ct = default);

    /// <summary>
    /// Xác thực lại một phiên đang có: tài khoản còn tồn tại, không bị khóa.
    /// Trả về thông tin MỚI NHẤT từ DB (họ tên, vai trò) để Session luôn đúng.
    /// </summary>
    Task<LoginResult> ValidateAsync(int maTaiKhoan, CancellationToken ct = default);

    Task<ChangePasswordResult> ChangePasswordAsync(int maTaiKhoan, string matKhauCu, string matKhauMoi, CancellationToken ct = default);
}
