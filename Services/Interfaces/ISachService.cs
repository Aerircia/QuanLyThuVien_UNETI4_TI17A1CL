// Họ và tên: Vũ Tiến Thành
// Mã sinh viên: 23103100257
// Nội dung thực hiện: Xây dựng interface cho việc quản lý sách
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Common;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;

public sealed record SachFilter(
    string? Search,
    int? MaTheLoai,
    TrangThaiSach? TrangThai,
    int Page = 1,
    int PageSize = PagingDefaults.DefaultPageSize,
    string? TacGia = null,
    int? NamTu = null,
    int? NamDen = null);

public sealed record SachListItem(
    int MaSach,
    string TenSach,
    string TacGia,
    string? NhaXuatBan,
    int NamXuatBan,
    int MaTheLoai,
    string TenTheLoai,
    int SoLuongTong,
    int SoLuongConLai,
    decimal GiaBia,
    TrangThaiSach TrangThai,
    string? ImgPath);

public sealed record SachDetail(
    int MaSach,
    int MaTheLoai,
    string TenTheLoai,
    string TenSach,
    string TacGia,
    string? NhaXuatBan,
    int NamXuatBan,
    int SoLuongTong,
    int SoLuongConLai,
    decimal GiaBia,
    string? ViTriKe,
    string? MoTa,
    TrangThaiSach TrangThai,
    string? ImgPath);

public sealed record CreateSachInput(
    int MaTheLoai,
    string TenSach,
    string TacGia,
    string? NhaXuatBan,
    int NamXuatBan,
    int SoLuongTong,
    decimal GiaBia,
    string? ViTriKe,
    string? MoTa,
    TrangThaiSach TrangThai,
    string? ImgPath);

public sealed record UpdateSachInput(
    int MaSach,
    int MaTheLoai,
    string TenSach,
    string TacGia,
    string? NhaXuatBan,
    int NamXuatBan,
    int SoLuongTong,
    decimal GiaBia,
    string? ViTriKe,
    string? MoTa,
    TrangThaiSach TrangThai,
    string? ImgPath);

public interface ISachService
{
    Task<PagedResult<SachListItem>> GetListAsync(SachFilter filter, CancellationToken ct = default);

    Task<IReadOnlyList<string>> GetAuthorOptionsAsync(CancellationToken ct = default);

    Task<SachDetail?> GetAsync(int maSach, CancellationToken ct = default);

    Task<ServiceResult<int>> CreateAsync(CreateSachInput input, CancellationToken ct = default);

    Task<ServiceResult> UpdateAsync(UpdateSachInput input, CancellationToken ct = default);

    /// <summary>true = sách đã bị xóa thật; false = còn lịch sử mượn nên đã chuyển Ngừng phục vụ.</summary>
    Task<ServiceResult<bool>> DeleteAsync(int maSach, CancellationToken ct = default);

    Task<ServiceResult> ToggleTrangThaiAsync(int maSach, CancellationToken ct = default);
}
