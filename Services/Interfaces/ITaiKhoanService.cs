// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Xây dựng interface cho dịch vụ quản lý tài khoản

using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Common;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;

public sealed record TaiKhoanFilter(
    string? Search,
    VaiTro? VaiTro,
    TrangThaiTaiKhoan? TrangThai,
    int Page = 1,
    int PageSize = PagingDefaults.DefaultPageSize,
    string? Sort = null);

public sealed record TaiKhoanListItem(
    int MaTaiKhoan,
    string TenDangNhap,
    string HoTen,
    string Email,
    VaiTro VaiTro,
    TrangThaiTaiKhoan TrangThai,
    bool CoHoSoBanDoc);

public sealed record TaiKhoanDetail(
    int MaTaiKhoan,
    string TenDangNhap,
    string HoTen,
    string Email,
    VaiTro VaiTro,
    TrangThaiTaiKhoan TrangThai,
    bool CoHoSoBanDoc);

public sealed record CreateTaiKhoanInput(
    string TenDangNhap,
    string MatKhau,
    string HoTen,
    string Email,
    VaiTro VaiTro,
    TrangThaiTaiKhoan TrangThai);

public sealed record UpdateTaiKhoanInput(
    int MaTaiKhoan,
    string HoTen,
    string Email,
    VaiTro VaiTro,
    TrangThaiTaiKhoan TrangThai);

/// <summary>
/// Quản lý tài khoản (Admin). Mọi quy tắc an toàn nằm ở đây, không phụ thuộc controller/View nào gọi.
/// Các thao tác thay đổi tài khoản nhận mã người đang thao tác (actingMaTaiKhoan).
/// </summary>
public interface ITaiKhoanService
{
    Task<PagedResult<TaiKhoanListItem>> GetListAsync(TaiKhoanFilter filter, CancellationToken ct = default);

    Task<TaiKhoanDetail?> GetAsync(int maTaiKhoan, CancellationToken ct = default);

    Task<ServiceResult<int>> CreateAsync(CreateTaiKhoanInput input, CancellationToken ct = default);

    /// <summary>Sửa họ tên, email, vai trò, trạng thái. Họ tên/email được đồng bộ sang BanDoc (nếu có).</summary>
    Task<ServiceResult> UpdateAsync(UpdateTaiKhoanInput input, int actingMaTaiKhoan, CancellationToken ct = default);

    /// <summary>Khóa / mở khóa tài khoản.</summary>
    Task<ServiceResult> SetStatusAsync(int maTaiKhoan, TrangThaiTaiKhoan trangThai, int actingMaTaiKhoan, CancellationToken ct = default);

    /// <summary>Admin đặt mật khẩu mới cho tài khoản.</summary>
    Task<ServiceResult> ResetPasswordAsync(int maTaiKhoan, string matKhauMoi, CancellationToken ct = default);

    /// <summary>Xóa tài khoản (và hồ sơ BanDoc liên kết nếu chưa có phiếu mượn).</summary>
    Task<ServiceResult> DeleteAsync(int maTaiKhoan, int actingMaTaiKhoan, CancellationToken ct = default);

    /// <summary>Email chưa thuộc tài khoản nào khác? Module Bạn đọc dùng lại khi lưu hồ sơ.</summary>
    Task<bool> IsEmailAvailableAsync(string email, int? excludeMaTaiKhoan = null, CancellationToken ct = default);
}
