// Họ và tên: Phùng Ngọc Bảo
// Mã sinh viên: 23103100278
// Nội dung thực hiện: DTO và interface cho Dashboard và báo cáo thống kê LINQ

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;

public sealed record DashboardSummary(
    int TongTheLoai,
    int TongDauSach,
    int TongCuonSach,
    int TongBanDoc,
    int TongPhieuMuon,
    int SoPhieuChoDuyet,
    int SoPhieuDangMuon,
    int SoPhieuQuaHan,
    decimal TienPhatThangNay);

public sealed record ThongKeTheLoaiItem(
    int MaTheLoai, string TenTheLoai, int SoDauSach);

// Dùng cho lượt mượn theo sách và số lượt gia hạn theo sách
public sealed record ThongKeSachItem(
    int MaSach, string TenSach, int SoLuot);

public sealed record ThongKeTrangThaiItem(
    string TrangThai, int SoPhieu);

public sealed record ThongKeBanDocItem(
    int MaBanDoc, string HoTen, int SoLuot);

public sealed record ThongKeTheoThangItem(
    int Nam, int Thang, string Ky, int SoLuong);

public sealed record ThongKeTienPhatTheoThangItem(
    int Nam, int Thang, string Ky, decimal TienPhat);

public sealed record ThongKeTyLeMuonTheoTheLoaiItem(
    int MaTheLoai,
    string TenTheLoai,
    int SoCuonDangMuon,
    int TongSoCuon,
    decimal TyLePhanTram);

public sealed record ThongKeReport(
    IReadOnlyList<ThongKeTheLoaiItem> DauSachTheoTheLoai,
    IReadOnlyList<ThongKeSachItem> LuotMuonTheoSach,
    IReadOnlyList<ThongKeTrangThaiItem> PhieuMuonTheoTrangThai,
    IReadOnlyList<ThongKeSachItem> Top10Sach,
    IReadOnlyList<ThongKeBanDocItem> BanDocMuonNhieu,
    IReadOnlyList<ThongKeTheoThangItem> LuotMuonTheoThang,
    IReadOnlyList<ThongKeTheoThangItem> LuotTraTheoThang,
    IReadOnlyList<ThongKeTienPhatTheoThangItem> TienPhatTheoThang,
    IReadOnlyList<ThongKeSachItem> GiaHanTheoSach,
    decimal TyLeTraTrePhanTram,
    IReadOnlyList<ThongKeTyLeMuonTheoTheLoaiItem> TyLeMuonTheoTheLoai);

public interface IThongKeService
{
    Task<DashboardSummary> GetDashboardAsync(CancellationToken ct = default);
    Task<ThongKeReport> GetReportAsync(CancellationToken ct = default);
}
