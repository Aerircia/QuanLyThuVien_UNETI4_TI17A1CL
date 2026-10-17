// Họ và tên: Phùng Ngọc Bảo
// Mã sinh viên: 23103100278
// Nội dung thực hiện: Interface và DTO cho nghiệp vụ quản lý phiếu mượn

using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Common;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;

public sealed record TheLoaiOption(int MaTheLoai, string TenTheLoai);

// Điều kiện tìm kiếm, lọc, sắp xếp và phân trang
public sealed record PhieuMuonFilter(
    string? Search,
    int? MaTheLoai,
    TrangThaiPhieuMuon? TrangThai,
    DateOnly? TuNgay,
    DateOnly? DenNgay,
    bool QuaHan,
    string SapXep,
    int Page = 1,
    int PageSize = PagingDefaults.DefaultPageSize);

// Dữ liệu một dòng trên trang danh sách
public sealed record PhieuMuonListItem(
    int MaPhieuMuon,
    string HoTen,
    string? MaSoThe,
    string TenSach,
    string TenTheLoai,
    DateOnly NgayDangKy,
    DateOnly? NgayMuon,
    DateOnly? HanTra,
    TrangThaiPhieuMuon TrangThai);

// Một sự kiện trong dòng thời gian xử lý phiếu
public sealed record PhieuMuonHistoryItem(
    TrangThaiPhieuMuon? TrangThaiCu,
    TrangThaiPhieuMuon TrangThaiMoi,
    DateTime NgayThucHien,
    string TenNguoiThucHien,
    string? GhiChu);

// DTO chỉ đọc dùng trên trang chi tiết, không dùng trực tiếp làm form cập nhật Entity
public sealed record PhieuMuonDetail(
    int MaPhieuMuon,
    int MaBanDoc,
    string HoTen,
    string? MaSoThe,
    int MaSach,
    string TenSach,
    string TenTheLoai,
    int SoLuongTong,
    int SoLuongConLai,
    DateOnly NgayDangKy,
    DateOnly? NgayMuon,
    DateOnly? HanTra,
    string? GhiChu,
    TrangThaiPhieuMuon TrangThai,
    DateOnly? NgayXuLy,
    string? NhanXetThuThu,
    IReadOnlyList<string> CanhBao,
    IReadOnlyList<PhieuMuonHistoryItem> LichSu);

// Dữ liệu người dùng được phép gửi khi xử lý phiếu
// Trạng thái mới, ngày xử lý và người xử lý do server quyết định
public sealed record ProcessPhieuMuonInput(
    int MaPhieuMuon,
    bool Duyet,
    string? NhanXetThuThu);

public interface IPhieuMuonService
{
    Task<IReadOnlyList<TheLoaiOption>> GetTheLoaiOptionsAsync(CancellationToken ct = default);

    Task<PagedResult<PhieuMuonListItem>> GetListAsync(
        PhieuMuonFilter filter, CancellationToken ct = default);

    Task<PhieuMuonDetail?> GetDetailsAsync(
        int id, CancellationToken ct = default);

    Task<ServiceResult> ProcessAsync(
        ProcessPhieuMuonInput input, int maNguoiThucHien, CancellationToken ct = default);

    Task<ServiceResult> HandOverAsync(
        int id, int maNguoiThucHien, CancellationToken ct = default);

    Task<ServiceResult> CancelAsync(
        int id, int maNguoiThucHien, string? ghiChu, CancellationToken ct = default);
}
