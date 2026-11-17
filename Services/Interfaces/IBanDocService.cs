// Họ và tên: Nguyễn Hữu Tuấn
// Mã sinh viên: 23103100136
// Nội dung thực hiện: Xây dựng interface cho dịch vụ quản lý bạn đọc

using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Common;
using QuanLyThuVien_UNETI4_TI17A1CL.ViewModels;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;

public sealed record CreateBanDocInput(
    int? MaTaiKhoan,
    string HoTen,
    DateOnly NgaySinh,
    GioiTinh GioiTinh,
    string SoDienThoai,
    string Email,
    string DiaChi,
    string? MaSoThe,
    LoaiBanDoc LoaiBanDoc,
    DateOnly NgayCapThe,
    DateOnly NgayHetHanThe,
    TrangThaiBanDoc TrangThai);

public sealed record UpdateBanDocInput(
    int MaBanDoc,
    string HoTen,
    DateOnly NgaySinh,
    GioiTinh GioiTinh,
    string SoDienThoai,
    string Email,
    string DiaChi,
    string? MaSoThe,
    LoaiBanDoc LoaiBanDoc,
    DateOnly NgayCapThe,
    DateOnly NgayHetHanThe,
    TrangThaiBanDoc TrangThai);

public sealed record UpdateProfileInput(
    string HoTen,
    DateOnly NgaySinh,
    GioiTinh GioiTinh,
    string SoDienThoai,
    string Email,
    string DiaChi);

public sealed record DangKyTheBanDocInput(
    string HoTen,
    DateOnly NgaySinh,
    GioiTinh GioiTinh,
    string SoDienThoai,
    string Email,
    string DiaChi,
    LoaiBanDoc LoaiBanDoc);

public sealed record BanDocStatusCounts(int TongSo, int ChoDuyet, int HoatDong, int NgungHoatDong, int TuChoi);

public interface IBanDocService
{
    Task<PagedResult<BanDocListItem>> GetListAsync(BanDocFilter filter, CancellationToken ct = default);

    Task<BanDocStatusCounts> GetStatusCountsAsync(CancellationToken ct = default);

    Task<BanDocDetail?> GetAsync(int maBanDoc, CancellationToken ct = default);

    Task<BanDocDetail?> GetByTaiKhoanAsync(int maTaiKhoan, CancellationToken ct = default);

    Task<ServiceResult<int>> CreateAsync(CreateBanDocInput input, CancellationToken ct = default);

    Task<ServiceResult> UpdateAsync(UpdateBanDocInput input, CancellationToken ct = default);

    Task<ServiceResult> UpdateProfileAsync(UpdateProfileInput input, int maTaiKhoan, CancellationToken ct = default);

    Task<ServiceResult> SetStatusAsync(int maBanDoc, TrangThaiBanDoc trangThai, CancellationToken ct = default);

    Task<bool> IsMaSoTheAvailableAsync(string maSoThe, int? excludeMaBanDoc = null, CancellationToken ct = default);

    Task<List<TaiKhoanLookupItem>> GetUnlinkedAccountsAsync(int? currentMaTaiKhoan = null, CancellationToken ct = default);

    Task<ServiceResult<int>> DangKyTheAsync(DangKyTheBanDocInput input, int maTaiKhoan, CancellationToken ct = default);

    Task<ServiceResult> DuyetTheAsync(int maBanDoc, CancellationToken ct = default);

    Task<ServiceResult> TuChoiTheAsync(int maBanDoc, string? lyDo = null, CancellationToken ct = default);

    Task<ServiceResult> HuyDangKyTheAsync(int maTaiKhoan, CancellationToken ct = default);
}
