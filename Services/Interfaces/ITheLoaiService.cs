// Họ và tên: Vũ Tiến Thành
// Mã sinh viên: 23103100257
// Nội dung thực hiện: Xây dựng interface cho việc quản lý thể loại
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Common;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;

public sealed record TheLoaiFilter(
    string? Search,
    TrangThaiTheLoai? TrangThai,
    int Page = 1,
    int PageSize = PagingDefaults.DefaultPageSize);

public sealed record TheLoaiListItem(
    int MaTheLoai,
    string TenTheLoai,
    string? MoTa,
    string? KhuVucLuuTru,
    TrangThaiTheLoai TrangThai,
    int SoLuongSach);

public sealed record TheLoaiDetail(
    int MaTheLoai,
    string TenTheLoai,
    string? MoTa,
    string? KhuVucLuuTru,
    TrangThaiTheLoai TrangThai);

public sealed record TheLoaiOption(int MaTheLoai, string TenTheLoai);

public sealed record CreateTheLoaiInput(
    string TenTheLoai,
    string? MoTa,
    string? KhuVucLuuTru,
    TrangThaiTheLoai TrangThai);

public sealed record UpdateTheLoaiInput(
    int MaTheLoai,
    string TenTheLoai,
    string? MoTa,
    string? KhuVucLuuTru,
    TrangThaiTheLoai TrangThai);

public interface ITheLoaiService
{
    Task<PagedResult<TheLoaiListItem>> GetListAsync(TheLoaiFilter filter, CancellationToken ct = default);

    Task<IReadOnlyList<TheLoaiOption>> GetActiveOptionsAsync(CancellationToken ct = default);

    Task<TheLoaiDetail?> GetAsync(int maTheLoai, CancellationToken ct = default);

    Task<ServiceResult<int>> CreateAsync(CreateTheLoaiInput input, CancellationToken ct = default);

    Task<ServiceResult> UpdateAsync(UpdateTheLoaiInput input, CancellationToken ct = default);

    Task<ServiceResult> DeleteAsync(int maTheLoai, CancellationToken ct = default);
}
