// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Xây dựng SessionExtensions để đọc/ghi thông tin đăng nhập trong Session.
// Thêm SetResetEmail / GetResetEmail / ClearResetEmail: giữ email đang đặt lại mật khẩu phía server (không đưa lên URL).

using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services.Auth;

public static class SessionExtensions
{
    private const string KeyMaTaiKhoan = "Auth.MaTaiKhoan";
    private const string KeyHoTen = "Auth.HoTen";
    private const string KeyVaiTro = "Auth.VaiTro";
    private const string KeyResetEmail = "Reset.Email";

    /// <summary>Ghi (hoặc làm mới) thông tin đăng nhập. Vai trò lưu dạng chuỗi (tên enum).</summary>
    public static void SignIn(this ISession session, CurrentUser user)
    {
        session.SetInt32(KeyMaTaiKhoan, user.MaTaiKhoan);
        session.SetString(KeyHoTen, user.HoTen);
        session.SetString(KeyVaiTro, user.VaiTro.ToString());
    }

    /// <summary>Xóa toàn bộ Session (đăng xuất / phiên không còn hợp lệ).</summary>
    public static void SignOut(this ISession session) => session.Clear();

    /// <summary>Trả về người dùng trong Session, hoặc null nếu chưa đăng nhập / dữ liệu hỏng.</summary>
    public static CurrentUser? GetCurrentUser(this ISession session)
    {
        var id = session.GetInt32(KeyMaTaiKhoan);
        var hoTen = session.GetString(KeyHoTen);
        var vaiTroText = session.GetString(KeyVaiTro);

        if (id is null || hoTen is null || vaiTroText is null)
            return null;

        if (!Enum.TryParse<VaiTro>(vaiTroText, out var vaiTro) || !Enum.IsDefined(vaiTro))
            return null;

        return new CurrentUser(id.Value, hoTen, vaiTro);
    }

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
