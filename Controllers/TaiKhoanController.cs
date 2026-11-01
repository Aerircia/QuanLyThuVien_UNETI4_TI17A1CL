// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Xây dựng TaiKhoanController để xử lý đăng nhập, đăng xuất, đổi mật khẩu, từ chối truy cập và đăng ký tài khoản.

using Microsoft.AspNetCore.Mvc;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Auth;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Common;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;
using QuanLyThuVien_UNETI4_TI17A1CL.ViewModels;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Controllers;

public class TaiKhoanController : Controller
{
    private readonly IAuthService _auth;
    private readonly IPasswordResetService _passwordReset;
    private readonly ICurrentUserService _current;
    private readonly ITaiKhoanService _taiKhoanService;

    public TaiKhoanController(
        IAuthService auth,
        IPasswordResetService passwordReset,
        ICurrentUserService current,
        ITaiKhoanService taiKhoanService)
    {
        _auth = auth;
        _passwordReset = passwordReset;
        _current = current;
        _taiKhoanService = taiKhoanService;
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

    // ---------- Đăng ký tài khoản (Người dùng tự đăng ký) ----------

    [HttpGet]
    public IActionResult Register()
    {
        if (_current.IsAuthenticated)
            return RedirectToAction("Index", "Home");

        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (_current.IsAuthenticated)
            return RedirectToAction("Index", "Home");

        if (!ModelState.IsValid)
            return View(model);

        var result = await _taiKhoanService.CreateAsync(
            new CreateTaiKhoanInput(
                model.TenDangNhap.Trim(),
                model.MatKhau,
                model.HoTen.Trim(),
                model.Email.Trim(),
                VaiTro.BanDoc,
                TrangThaiTaiKhoan.HoatDong),
            HttpContext.RequestAborted);

        if (!result.Succeeded)
        {
            result.AddTo(ModelState);
            model.MatKhau = model.XacNhanMatKhau = string.Empty;
            return View(model);
        }

        TempData["ThongBao"] = "Đăng ký tài khoản thành công! Vui lòng đăng nhập, sau đó vào mục 'Thẻ & hồ sơ cá nhân' để đăng ký thẻ bạn đọc.";
        return RedirectToAction(nameof(Login));
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

    // ---------- Quên mật khẩu, bước 1: nhập email để nhận OTP ----------

    [HttpGet]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _passwordReset.RequestAsync(model.Email, HttpContext.RequestAborted);

        switch (result.Status)
        {
            case RequestResetStatus.Sent:
                TempData["ThongBao"] = "Mã OTP đã được gửi tới email của bạn.";
                return RedirectToAction(nameof(ResetPassword), new { email = model.Email.Trim() });

            case RequestResetStatus.EmailNotFound:
                ModelState.AddModelError(nameof(model.Email), "Email không tồn tại trong hệ thống.");
                break;

            case RequestResetStatus.AccountLocked:
                ModelState.AddModelError(string.Empty, "Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên.");
                break;

            case RequestResetStatus.CooldownActive:
                ModelState.AddModelError(string.Empty,
                    $"Vui lòng đợi {result.RetryAfterSeconds} giây trước khi yêu cầu mã mới.");
                break;

            case RequestResetStatus.HourlyLimitReached:
                ModelState.AddModelError(string.Empty,
                    $"Bạn đã yêu cầu mã quá nhiều lần. Vui lòng thử lại sau khoảng {Math.Max(1, (result.RetryAfterSeconds + 59) / 60)} phút.");
                break;

            default:
                ModelState.AddModelError(string.Empty, "Không gửi được email. Vui lòng thử lại sau.");
                break;
        }

        return View(model);
    }

    // ---------- Quên mật khẩu, bước 2: nhập OTP và mật khẩu mới ----------

    [HttpGet]
    public IActionResult ResetPassword(string? email = null) =>
        View(new ResetPasswordViewModel { Email = email ?? string.Empty });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.MatKhauMoi = model.XacNhanMatKhauMoi = string.Empty;
            return View(model);
        }

        var status = await _passwordReset.ResetAsync(
            model.Email, model.MaOtp, model.MatKhauMoi, HttpContext.RequestAborted);

        switch (status)
        {
            case ResetPasswordStatus.Success:
                TempData["ThongBao"] = "Đặt lại mật khẩu thành công. Vui lòng đăng nhập bằng mật khẩu mới.";
                return RedirectToAction(nameof(Login));

            case ResetPasswordStatus.EmailNotFound:
                ModelState.AddModelError(nameof(model.Email), "Email không tồn tại trong hệ thống.");
                break;

            case ResetPasswordStatus.AccountLocked:
                ModelState.AddModelError(string.Empty, "Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên.");
                break;

            case ResetPasswordStatus.TooManyAttempts:
                ModelState.AddModelError(string.Empty,
                    "Bạn đã nhập sai quá nhiều lần. Vui lòng quay lại bước trước để nhận mã mới.");
                break;

            default:
                ModelState.AddModelError(nameof(model.MaOtp), "Mã OTP không đúng hoặc đã hết hạn.");
                break;
        }

        model.MaOtp = model.MatKhauMoi = model.XacNhanMatKhauMoi = string.Empty;
        return View(model);
    }
}
