// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Giữ email của luồng đặt lại mật khẩu phía server (không đưa lên URL).
// Quên mật khẩu không đăng nhập, sử dụng session lưu email giữa nhiều thao tác
namespace QuanLyThuVien_UNETI4_TI17A1CL.Services.Auth;

public static class SessionExtensions
{
    private const string KeyResetEmail = "Reset.Email";

    // ---------- Quên mật khẩu: email đang được đặt lại ----------

    /// <summary>Ghi email vừa được gửi mã OTP để các bước sau lấy từ Session thay vì từ URL.</summary>
    public static void SetResetEmail(this ISession session, string email) =>
        session.SetString(KeyResetEmail, email);

    /// <summary>Email đang đặt lại mật khẩu, hoặc null nếu chưa bắt đầu / phiên đã hết hạn.</summary>
    public static string? GetResetEmail(this ISession session)
    {
        var email = session.GetString(KeyResetEmail);
        return string.IsNullOrWhiteSpace(email) ? null : email;
    }

    public static void ClearResetEmail(this ISession session) => session.Remove(KeyResetEmail);
}
