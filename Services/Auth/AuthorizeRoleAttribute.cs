// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Xây dựng AuthorizeRoleAttribute để kiểm tra đăng nhập + vai trò TẠI SERVER, trước khi action chạy.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services.Auth;

/// <summary>
/// Dùng: [AuthorizeRole(VaiTro.Admin, VaiTro.ThuThu)]. Không truyền vai trò = chỉ cần đã đăng nhập.
/// Mỗi request kiểm tra lại tài khoản trong DB: tài khoản bị khóa/xóa sẽ bị đăng xuất ngay,
/// và đổi vai trò có hiệu lực ngay (Session được làm mới).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class AuthorizeRoleAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly VaiTro[] _roles;

    public AuthorizeRoleAttribute(params VaiTro[] roles) => _roles = roles;

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var http = context.HttpContext;
        var sessionUser = http.Session.GetCurrentUser();

        if (sessionUser is null)
        {
            context.Result = NotSignedIn(context, reason: null);
            return;
        }

        var auth = http.RequestServices.GetRequiredService<IAuthService>();
        var check = await auth.ValidateAsync(sessionUser.MaTaiKhoan, http.RequestAborted);

        if (!check.Succeeded)
        {
            http.Session.SignOut();
            context.Result = NotSignedIn(context,
                reason: check.Failure == LoginFailure.AccountLocked ? "locked" : "invalid");
            return;
        }

        var user = check.User!;
        if (user != sessionUser)
            http.Session.SignIn(user); // họ tên / vai trò đã đổi trong DB

        if (_roles.Length > 0 && !_roles.Contains(user.VaiTro))
            context.Result = Forbidden(context);
    }

    private static IActionResult NotSignedIn(AuthorizationFilterContext context, string? reason)
    {
        var request = context.HttpContext.Request;
        if (IsAjax(request))
            return new StatusCodeResult(StatusCodes.Status401Unauthorized);

        // Chỉ GET mới quay lại được trang cũ sau khi đăng nhập.
        string? returnUrl = HttpMethods.IsGet(request.Method)
            ? $"{request.PathBase}{request.Path}{request.QueryString}"
            : null;

        return new RedirectToActionResult("Login", "TaiKhoan", new { returnUrl, reason });
    }

    private static IActionResult Forbidden(AuthorizationFilterContext context)
    {
        if (IsAjax(context.HttpContext.Request))
            return new StatusCodeResult(StatusCodes.Status403Forbidden);

        return new RedirectToActionResult("AccessDenied", "TaiKhoan", null);
    }

    private static bool IsAjax(HttpRequest request) =>
        request.Headers.XRequestedWith == "XMLHttpRequest";
}
