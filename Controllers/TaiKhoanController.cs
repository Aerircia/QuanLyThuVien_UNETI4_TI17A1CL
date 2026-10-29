// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Xây dựng TaiKhoanController để xử lý đăng nhập, đăng xuất, đổi mật khẩu, từ chối truy cập.

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Auth;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Common;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;
using QuanLyThuVien_UNETI4_TI17A1CL.ViewModels;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Controllers;

public class TaiKhoanController : Controller
{
    private const string KeyCooldown = "Cooldown";
    private const string KeyResetEmail = "ResetEmail";
    private const string KeyResetOtp = "ResetOtp";

    private readonly IAuthService _auth;
    private readonly IPasswordResetService _passwordReset;
    private readonly ITaiKhoanService _taiKhoan;
    private readonly ICurrentUserService _current;
    private readonly OtpOptions _otp;

    public TaiKhoanController(
        IAuthService auth,
        IPasswordResetService passwordReset,
        ITaiKhoanService taiKhoan,
        ICurrentUserService current,
        IOptions<OtpOptions> otp)
    {
        _auth = auth;
        _passwordReset = passwordReset;
        _taiKhoan = taiKhoan;
        _current = current;
        _otp = otp.Value;
    }

    // Nhân viên vào Dashboard, bạn đọc về trang chủ.
    private IActionResult RedirectByRole(VaiTro vaiTro) =>
        vaiTro is VaiTro.Admin or VaiTro.ThuThu
            ? RedirectToAction("Dashboard", "ThongKe")
            : RedirectToAction("Index", "Home");

    // ---------- Đăng nhập ----------

    [HttpGet]
    public IActionResult Login(string? returnUrl = null, string? reason = null)
    {
        if (_current.IsAuthenticated)
            return RedirectByRole(_current.User!.VaiTro);

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

        return RedirectByRole(result.User!.VaiTro);
    }

    // ---------- Đăng ký (chỉ tạo TaiKhoan vai trò Bạn đọc) ----------

    [HttpGet]
    public IActionResult Register()
    {
        if (_current.IsAuthenticated)
            return RedirectByRole(_current.User!.VaiTro);

        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (_current.IsAuthenticated)
            return RedirectByRole(_current.User!.VaiTro);

        if (!ModelState.IsValid)
        {
            model.MatKhau = model.XacNhanMatKhau = string.Empty;
            return View(model);
        }

        // Vai trò và trạng thái do server quyết định, không lấy từ form.
        var result = await _taiKhoan.CreateAsync(
            new CreateTaiKhoanInput(
                model.TenDangNhap, model.MatKhau, model.HoTen, model.Email,
                VaiTro.BanDoc, TrangThaiTaiKhoan.HoatDong),
            HttpContext.RequestAborted);

        if (!result.Succeeded)
        {
            result.AddTo(ModelState);
            model.MatKhau = model.XacNhanMatKhau = string.Empty;
            return View(model);
        }

        TempData["ThongBao"] = "Đăng ký thành công. Vui lòng đăng nhập.";
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

        if (result.Status == RequestResetStatus.Sent)
        {
            TempData["ThongBao"] = "Mã OTP đã được gửi tới email của bạn.";
            TempData[KeyCooldown] = _otp.ResendCooldownSeconds;
            return RedirectToAction(nameof(VerifyOtp), new { email = model.Email.Trim() });
        }

        if (result.Status == RequestResetStatus.EmailNotFound)
            ModelState.AddModelError(nameof(model.Email), "Email không tồn tại trong hệ thống.");
        else
            ModelState.AddModelError(string.Empty, DescribeRequestFailure(result));

        return View(model);
    }

    // ---------- Quên mật khẩu, bước 2: nhập mã OTP (chỉ kiểm tra, chưa tiêu thụ mã) ----------

    [HttpGet]
    public IActionResult VerifyOtp(string? email = null)
    {
        if (string.IsNullOrWhiteSpace(email))
            return RedirectToAction(nameof(ForgotPassword));

        return View(new VerifyOtpViewModel
        {
            Email = email.Trim(),
            CooldownSeconds = TempData[KeyCooldown] as int? ?? 0
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyOtp(VerifyOtpViewModel model)
    {
        model.CooldownSeconds = 0;
        model.MaOtp ??= string.Empty;

        if (!ModelState.IsValid)
        {
            model.MaOtp = string.Empty;
            return View(model);
        }

        var status = await _passwordReset.VerifyOtpAsync(model.Email, model.MaOtp, HttpContext.RequestAborted);

        switch (status)
        {
            case ResetPasswordStatus.Success:
                // Chuyển sang bước 3 qua TempData (cookie đã mã hóa), không đưa mã lên URL.
                TempData[KeyResetEmail] = model.Email.Trim();
                TempData[KeyResetOtp] = model.MaOtp;
                return RedirectToAction(nameof(ResetPassword));

            case ResetPasswordStatus.EmailNotFound:
                ModelState.AddModelError(string.Empty, "Email không tồn tại trong hệ thống.");
                break;

            case ResetPasswordStatus.AccountLocked:
                ModelState.AddModelError(string.Empty, "Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên.");
                break;

            case ResetPasswordStatus.TooManyAttempts:
                ModelState.AddModelError(string.Empty,
                    "Bạn đã nhập sai quá nhiều lần. Vui lòng bấm \"Gửi lại mã\" để nhận mã mới.");
                break;

            default:
                ModelState.AddModelError(nameof(model.MaOtp), "Mã OTP không đúng hoặc đã hết hạn.");
                break;
        }

        model.MaOtp = string.Empty;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendOtp(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return RedirectToAction(nameof(ForgotPassword));

        var result = await _passwordReset.RequestAsync(email, HttpContext.RequestAborted);

        if (result.Status == RequestResetStatus.Sent)
        {
            TempData["ThongBao"] = "Đã gửi lại mã OTP mới tới email của bạn.";
            TempData[KeyCooldown] = _otp.ResendCooldownSeconds;
        }
        else
        {
            TempData["Loi"] = DescribeRequestFailure(result);
            if (result.Status == RequestResetStatus.CooldownActive)
                TempData[KeyCooldown] = result.RetryAfterSeconds;
        }

        return RedirectToAction(nameof(VerifyOtp), new { email = email.Trim() });
    }

    // ---------- Quên mật khẩu, bước 3: đặt mật khẩu mới (kiểm tra lại và tiêu thụ OTP) ----------

    [HttpGet]
    public IActionResult ResetPassword()
    {
        var email = TempData.Peek(KeyResetEmail) as string;
        var otp = TempData.Peek(KeyResetOtp) as string;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(otp))
        {
            TempData["Loi"] = "Phiên đặt lại mật khẩu đã hết hạn. Vui lòng bắt đầu lại.";
            return RedirectToAction(nameof(ForgotPassword));
        }

        return View(new ResetPasswordViewModel { Email = email, MaOtp = otp });
    }

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
                TempData.Remove(KeyResetEmail);
                TempData.Remove(KeyResetOtp);
                TempData["ThongBao"] = "Đặt lại mật khẩu thành công. Vui lòng đăng nhập bằng mật khẩu mới.";
                return RedirectToAction(nameof(Login));

            case ResetPasswordStatus.EmailNotFound:
                ModelState.AddModelError(string.Empty, "Email không tồn tại trong hệ thống.");
                break;

            case ResetPasswordStatus.AccountLocked:
                ModelState.AddModelError(string.Empty, "Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên.");
                break;

            default:
                // Mã hết hạn hoặc bị vô hiệu giữa bước 2 và bước 3: quay lại bước nhập mã.
                TempData.Remove(KeyResetEmail);
                TempData.Remove(KeyResetOtp);
                TempData["Loi"] = status == ResetPasswordStatus.TooManyAttempts
                    ? "Bạn đã nhập sai quá nhiều lần. Vui lòng nhận mã mới."
                    : "Mã OTP không đúng hoặc đã hết hạn. Vui lòng nhập lại hoặc nhận mã mới.";
                return RedirectToAction(nameof(VerifyOtp), new { email = model.Email });
        }

        model.MatKhauMoi = model.XacNhanMatKhauMoi = string.Empty;
        return View(model);
    }

    private static string DescribeRequestFailure(RequestResetResult result) => result.Status switch
    {
        RequestResetStatus.EmailNotFound => "Email không tồn tại trong hệ thống.",
        RequestResetStatus.AccountLocked => "Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên.",
        RequestResetStatus.CooldownActive => $"Vui lòng đợi {result.RetryAfterSeconds} giây trước khi yêu cầu mã mới.",
        RequestResetStatus.HourlyLimitReached =>
            $"Bạn đã yêu cầu mã quá nhiều lần. Vui lòng thử lại sau khoảng {Math.Max(1, (result.RetryAfterSeconds + 59) / 60)} phút.",
        _ => "Không gửi được email. Vui lòng thử lại sau."
    };
}
