// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Xây dựng interface ICurrentUserService để định nghĩa các phương thức truy xuất thông tin người dùng đang đăng nhập.

using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Auth;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;

/// <summary>Cho service/controller biết ai đang đăng nhập mà không phải đụng tới HttpContext.</summary>
public interface ICurrentUserService
{
    /// <summary>Người dùng trong Session, null nếu chưa đăng nhập.</summary>
    CurrentUser? User { get; }

    bool IsAuthenticated { get; }

    /// <summary>True nếu đã đăng nhập và vai trò nằm trong danh sách.</summary>
    bool IsInRole(params VaiTro[] roles);

    /// <summary>
    /// MaBanDoc của tài khoản hiện tại (null nếu chưa đăng nhập hoặc tài khoản chưa có hồ sơ bạn đọc).
    /// Dùng cho quy tắc sở hữu: truy vấn theo id VÀ chủ sở hữu.
    /// </summary>
    Task<int?> GetMaBanDocAsync(CancellationToken ct = default);
}
