// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Xây dựng TaiKhoanController để xử lý đăng nhập, đăng xuất, đổi mật khẩu, từ chối truy cập.

using Microsoft.AspNetCore.Mvc;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Auth;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;
using QuanLyThuVien_UNETI4_TI17A1CL.ViewModels;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Controllers;

public class TaiKhoanController : Controller
{
    private readonly IAuthService _auth;
    private readonly ICurrentUserService _current;

    public TaiKhoanController(IAuthService auth, ICurrentUserService current)
    {
        _auth = auth;
        _current = current;
    }

    // ---------- Đăng nhập ----------

    [HttpGet]
    public IActionResult Login(string? returnUrl = null, string? reason = null)
    {
        if (_current.IsAuthenticated)
            return RedirectToAction("Index", "Home");

        ViewBag.ThongBao = reason switch
        {
            "locked" => "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên.",
            "invalid" => "Phiên đăng nhập không còn hợp lệ. Vui lòng đăng nhập lại.",
            _ => null
        };

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _auth.LoginAsync(model.TenDangNhap, model.MatKhau, HttpContext.RequestAborted);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Failure switch
            {
                LoginFailure.AccountLocked => "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên.",
                _ => "Tên đăng nhập hoặc mật khẩu không đúng."
            });
            model.MatKhau = string.Empty;
            return View(model);
        }

        // Bỏ dữ liệu phiên cũ rồi mới ghi thông tin đăng nhập mới.
        HttpContext.Session.SignOut();
        HttpContext.Session.SignIn(result.User!);

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return LocalRedirect(model.ReturnUrl);

        return RedirectToAction("Index", "Home");
    }

    // ---------- Đăng xuất ----------

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.SignOut();
        return RedirectToAction(nameof(Login));
    }

    // ---------- Từ chối truy cập ----------

    [HttpGet]
    public IActionResult AccessDenied()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View();
    }

    // ---------- Đổi mật khẩu ----------

    [AuthorizeRole]
    [HttpGet]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [AuthorizeRole]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = _current.User!; // [AuthorizeRole] đã đảm bảo đã đăng nhập
        var result = await _auth.ChangePasswordAsync(
            user.MaTaiKhoan, model.MatKhauCu, model.MatKhauMoi, HttpContext.RequestAborted);

        switch (result)
        {
            case ChangePasswordResult.Success:
                TempData["ThongBao"] = "Đổi mật khẩu thành công.";
                return RedirectToAction(nameof(ChangePassword));

            case ChangePasswordResult.WrongOldPassword:
                ModelState.AddModelError(nameof(model.MatKhauCu), "Mật khẩu hiện tại không đúng.");
                break;

            case ChangePasswordResult.SameAsOldPassword:
                ModelState.AddModelError(nameof(model.MatKhauMoi), "Mật khẩu mới phải khác mật khẩu hiện tại.");
                break;

            default:
                ModelState.AddModelError(string.Empty, "Không tìm thấy tài khoản.");
                break;
        }

        model.MatKhauCu = model.MatKhauMoi = model.XacNhanMatKhauMoi = string.Empty;
        return View(model);
    }
}
