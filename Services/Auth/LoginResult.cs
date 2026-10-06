// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Kết quả đăng nhập / xác thực lại phiên.

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services.Auth;

/// <summary>Lý do đăng nhập / xác thực phiên thất bại.</summary>
public enum LoginFailure
{
    None,
    /// <summary>Không tồn tại tài khoản HOẶC sai mật khẩu (cố ý gộp chung để không lộ tên đăng nhập).</summary>
    InvalidCredentials,
    /// <summary>Tài khoản bị khóa (chỉ báo sau khi mật khẩu đúng khi đăng nhập).</summary>
    AccountLocked
}

/// <summary>Kết quả đổi mật khẩu.</summary>
public enum ChangePasswordResult
{
    Success,
    AccountNotFound,
    WrongOldPassword,
    SameAsOldPassword
}

public sealed class LoginResult
{
    public bool Succeeded => Failure == LoginFailure.None;
    public LoginFailure Failure { get; }
    public CurrentUser? User { get; }

    private LoginResult(LoginFailure failure, CurrentUser? user)
    {
        Failure = failure;
        User = user;
    }

    public static LoginResult Success(CurrentUser user) => new(LoginFailure.None, user);
    public static LoginResult Fail(LoginFailure failure) => new(failure, null);
}