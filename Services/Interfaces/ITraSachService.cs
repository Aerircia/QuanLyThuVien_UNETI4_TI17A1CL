// Họ và tên: Vũ Ngọc Hương Giang
// Mã sinh viên: 23103100280
// Nội dung thực hiện: interface nhận trả sách và tiền phạt.

using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Common;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;
public sealed record TraSachFilter(
    string? Search,
    TinhTrangSach? TinhTrang,
    TrangThaiPhat? TrangThaiPhat,
    int Page = 1,
    int PageSize = PagingDefaults.DefaultPageSize);
public sealed record TraSachListItem(
    int MaTra,
    int MaPhieuMuon,
    string TenBanDoc,
    string TenSach,
    DateOnly NgayTra,
    TinhTrangSach TinhTrang,
    int SoNgayTre,
    decimal TienPhat,
    TrangThaiPhat TrangThaiPhat);
public sealed record TraSachDetail(
    int MaTra,
    int MaPhieuMuon,
    string TenBanDoc,
    string? MaSoThe,
    string TenSach,
    decimal GiaBia,
    DateOnly? NgayMuon,
    DateOnly? HanTra,
    DateOnly NgayTra,
    TinhTrangSach TinhTrang,
    int SoNgayTre,
    decimal TienPhat,    
    TrangThaiPhat TrangThaiPhat,
    string? GhiChu,
    string TenNguoiNhan)
{
    public decimal PhatTre { get; init; }
    public decimal PhatTinhTrang { get; init; }
}
public sealed record TraSachPreview(
    int MaPhieuMuon,
    string TenBanDoc,
    string? MaSoThe,
    string TenSach,
    decimal GiaBia,
    DateOnly? NgayMuon,
    DateOnly? HanTra,
    DateOnly NgayTra,
    int SoNgayTre,
    decimal TienPhatTre,         
    decimal TongBinhThuong,
    decimal TongHuHong,
    decimal TongMat,
    IReadOnlyList<string> CanhBao)
{
    public bool DuocTra => CanhBao.Count == 0;
    public decimal PhatHuHong { get; set; }
    public decimal PhatMat { get; set; }
}
    public sealed record PhieuChoTraFilter( string? Search, bool ChiQuaHan = false, int Page = 1, int PageSize = PagingDefaults.DefaultPageSize);
    public sealed record PhieuChoTraItem(int MaPhieuMuon, string TenBanDoc, string? MaSoThe, string TenSach, DateOnly? NgayMuon, DateOnly? HanTra)
{
    public int SoNgayTre { get; set; }
}
public sealed record TraSachInput(int MaPhieuMuon, TinhTrangSach TinhTrang, string? GhiChu);

public interface ITraSachService
{
    Task<PagedResult<TraSachListItem>> GetListAsync(TraSachFilter filter, CancellationToken ct = default);
    Task<PagedResult<PhieuChoTraItem>> GetPhieuChoTraAsync(PhieuChoTraFilter filter, CancellationToken ct = default);
    Task<TraSachPreview?> GetPreviewAsync(int maPhieuMuon, CancellationToken ct = default);
    Task<ServiceResult<int>> CreateAsync(TraSachInput input, int actingMaTaiKhoan, CancellationToken ct = default);
    Task<TraSachDetail?> GetAsync(int maTra, CancellationToken ct = default);
    Task<ServiceResult> MarkPaidAsync(int maTra, CancellationToken ct = default);
}