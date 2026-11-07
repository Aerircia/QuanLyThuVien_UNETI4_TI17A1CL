// Họ và tên: Vũ Ngọc Hương Giang
// Mã sinh viên: 23103100280
// Nội dung thực hiện: interface  gia hạn phiếu mượn 

using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Common;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;

public sealed record GiaHanMuonFilter(
    string? Search,
    TrangThaiGiaHan? TrangThai, 
    int Page = 1, 
    int PageSize = PagingDefaults.DefaultPageSize);

public sealed record GiaHanMuonListItem(
    int MaGiaHan,
    int MaPhieuMuon,
    string TenBanDoc,
    string TenSach,
    DateOnly HanTraCu,
    DateOnly HanTraMoi,
    string? LyDo,
    DateOnly NgayYeuCau,
    TrangThaiGiaHan TrangThai,
    DateOnly? NgayXuLy);

public sealed record GiaHanCreateInfo(
    int MaPhieuMuon,
    string TenSach,
    DateOnly? HanTraHienTai,
    int SoLanDaGiaHan,
    int SoLanToiDa,
    int SoNgayToiDa,
    IReadOnlyList<string> CanhBao)
{
    public bool DuocGiaHan => CanhBao.Count == 0;
    public DateOnly? HanTraMoiToiDa => HanTraHienTai?.AddDays(SoNgayToiDa);
}

public sealed record CreateGiaHanInput(int MaPhieuMuon, DateOnly HanTraMoi, string? LyDo);

public sealed record GiaHanMuonDetail(
    int MaGiaHan,
    int MaPhieuMuon,
    int MaBanDoc,
    string TenBanDoc,
    string TenSach,
    TrangThaiPhieuMuon TrangThaiPhieu,
    DateOnly? HanTraHienTai,
    DateOnly HanTraCu,
    DateOnly HanTraMoi,
    string? LyDo,
    DateOnly NgayYeuCau,
    TrangThaiGiaHan TrangThai,
    DateOnly? NgayXuLy,
    string? NhanXet,
    int SoLanDaGiaHan,
    int SoLanToiDa,
    int SoNgayToiDa,
    int SoLuongConLai,
    IReadOnlyList<string> CanhBao)
{
    public bool DuDieuKienDuyet => TrangThai == TrangThaiGiaHan.ChoDuyet && CanhBao.Count == 0;
}
public sealed record ProcessGiaHanInput(int MaGiaHan, bool Duyet, string? NhanXet);
public interface IGiaHanMuonService
{
    Task<PagedResult<GiaHanMuonListItem>> GetListAsync(GiaHanMuonFilter filter, CancellationToken ct = default);

    Task<GiaHanCreateInfo?> GetCreateInfoAsync(int maPhieuMuon, int maBanDoc, CancellationToken ct = default);

    Task<ServiceResult<int>> CreateAsync(CreateGiaHanInput input, int maBanDoc, CancellationToken ct = default);

    Task<GiaHanMuonDetail?> GetAsync(int maGiaHan, CancellationToken ct = default);

    Task<ServiceResult> ProcessAsync(ProcessGiaHanInput input, CancellationToken ct = default);
}