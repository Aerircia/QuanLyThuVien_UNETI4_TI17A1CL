// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Xây dựng AuthService để xử lý đăng nhập, xác thực, thay đổi mật khẩu.

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien_UNETI4_TI17A1CL.Data;
using QuanLyThuVien_UNETI4_TI17A1CL.Models;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Auth;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<TaiKhoan> _hasher;

    public AuthService(AppDbContext db, IPasswordHasher<TaiKhoan> hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    public async Task<LoginResult> LoginAsync(string tenDangNhap, string matKhau, CancellationToken ct = default)
    {
        tenDangNhap = (tenDangNhap ?? string.Empty).Trim();

        // Có tracking vì có thể phải băm lại mật khẩu và lưu.
        var tk = await _db.Set<TaiKhoan>()
            .FirstOrDefaultAsync(t => t.TenDangNhap == tenDangNhap, ct);

        // Không tồn tại hoặc sai mật khẩu -> cùng một kết quả.
        if (tk is null || string.IsNullOrEmpty(matKhau))
            return LoginResult.Fail(LoginFailure.InvalidCredentials);

        var verify = _hasher.VerifyHashedPassword(tk, tk.MatKhau, matKhau);
        if (verify == PasswordVerificationResult.Failed)
            return LoginResult.Fail(LoginFailure.InvalidCredentials);

        // Mật khẩu đúng mới báo khóa.
        if (tk.TrangThai == TrangThaiTaiKhoan.BiKhoa)
            return LoginResult.Fail(LoginFailure.AccountLocked);

        if (verify == PasswordVerificationResult.SuccessRehashNeeded)
        {
            tk.MatKhau = _hasher.HashPassword(tk, matKhau);
            await _db.SaveChangesAsync(ct);
        }

        return LoginResult.Success(new CurrentUser(tk.MaTaiKhoan, tk.HoTen, tk.VaiTro));
    }

    public async Task<LoginResult> ValidateAsync(int maTaiKhoan, CancellationToken ct = default)
    {
        var tk = await _db.Set<TaiKhoan>()
            .AsNoTracking()
            .Where(t => t.MaTaiKhoan == maTaiKhoan)
            .Select(t => new { t.MaTaiKhoan, t.HoTen, t.VaiTro, t.TrangThai })
            .FirstOrDefaultAsync(ct);

        if (tk is null)
            return LoginResult.Fail(LoginFailure.InvalidCredentials);

        if (tk.TrangThai == TrangThaiTaiKhoan.BiKhoa)
            return LoginResult.Fail(LoginFailure.AccountLocked);

        return LoginResult.Success(new CurrentUser(tk.MaTaiKhoan, tk.HoTen, tk.VaiTro));
    }

    public async Task<ChangePasswordResult> ChangePasswordAsync(
        int maTaiKhoan, string matKhauCu, string matKhauMoi, CancellationToken ct = default)
    {
        var tk = await _db.Set<TaiKhoan>().FirstOrDefaultAsync(t => t.MaTaiKhoan == maTaiKhoan, ct);
        if (tk is null)
            return ChangePasswordResult.AccountNotFound;

        if (_hasher.VerifyHashedPassword(tk, tk.MatKhau, matKhauCu) == PasswordVerificationResult.Failed)
            return ChangePasswordResult.WrongOldPassword;

        if (matKhauCu == matKhauMoi)
            return ChangePasswordResult.SameAsOldPassword;

        tk.MatKhau = _hasher.HashPassword(tk, matKhauMoi);
        await _db.SaveChangesAsync(ct);
        return ChangePasswordResult.Success;
    }
}
