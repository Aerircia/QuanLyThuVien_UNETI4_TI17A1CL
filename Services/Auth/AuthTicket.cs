// Dựng ClaimsPrincipal cho cookie xác thực từ CurrentUser. Đây là nơi duy nhất định nghĩa các claim.

using System.Security.Claims;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services.Auth;

public static class AuthTicket
{
    public const string UsernameClaim = "lms:username";

    public static ClaimsPrincipal ToPrincipal(CurrentUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.MaTaiKhoan.ToString()),
            new(ClaimTypes.Name, user.HoTen),
            new(ClaimTypes.Role, user.VaiTro.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(UsernameClaim, user.TenDangNhap)
        };
        return new ClaimsPrincipal(
            new ClaimsIdentity(claims, Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme));
    }
}
