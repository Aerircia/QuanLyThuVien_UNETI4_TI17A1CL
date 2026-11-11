// Họ và tên: Phùng Ngọc Bảo
// Mã sinh viên: 23103100278
// Nội dung thực hiện: Xây dựng interface IThongBaoService
using QuanLyThuVien_UNETI4_TI17A1CL.Models;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;

public sealed record ThongBaoItem(
    int MaThongBao, string TieuDe, string NoiDung, string LoaiThongBao,
    string MucDo, string? Icon, string? DuongDan, DateTime NgayTao, bool DaDoc);

public sealed record ThongBaoBellResult(int ChuaDoc, IReadOnlyList<ThongBaoItem> DanhSach);

public sealed record ThongBaoPageResult(
    IReadOnlyList<ThongBaoItem> DanhSach,
    int TongSo,
    int ChuaDoc,
    int TongTheoBoLoc,
    int Trang,
    int SoTrang);

// Hop dong dung chung cho ca hai nhanh Admin/Thu thu va Ban doc
public interface IThongBaoService
{
    // Ghi: tao moi hoac cap nhat theo khoa su kien, khong tao ban ghi trung
    Task CreateAsync(int maTaiKhoan, string tieuDe, string noiDung, string khoaSuKien,
        string loaiThongBao = "loan", string mucDo = "in", string? duongDan = null,
        string? loaiDoiTuong = null, int? maDoiTuong = null, CancellationToken ct = default);
    Task UpdateAsync(int maThongBao, int maTaiKhoan, string tieuDe, string noiDung,
        string? duongDan = null, CancellationToken ct = default);

    // Doc: dem chua doc, danh sach, danh dau da doc, an thong bao da xu ly
    Task<int> GetUnreadCountAsync(int maTaiKhoan, CancellationToken ct = default);
    Task<IReadOnlyList<ThongBaoItem>> GetListAsync(int maTaiKhoan, int take = 20, CancellationToken ct = default);
    Task<ThongBaoBellResult> GetBellAsync(int maTaiKhoan, int take = 8, CancellationToken ct = default);
    Task<ThongBaoPageResult> GetPageAsync(int maTaiKhoan, string filter = "all", int page = 1,
        int pageSize = 12, CancellationToken ct = default);
    Task<ThongBaoItem?> GetByIdAsync(int maThongBao, int maTaiKhoan, CancellationToken ct = default);
    Task<bool> MarkReadAsync(int maThongBao, int maTaiKhoan, CancellationToken ct = default);
    Task<int> MarkAllReadAsync(int maTaiKhoan, CancellationToken ct = default);
    Task<bool> HideAsync(int maThongBao, int maTaiKhoan, CancellationToken ct = default);

    Task<CaiDatThongBao> GetSettingsAsync(int maTaiKhoan, CancellationToken ct = default);
    Task UpdateSettingsAsync(int maTaiKhoan, Action<CaiDatThongBao> update, CancellationToken ct = default);

    // Ham tien ich phat thong bao theo nghiep vu
    Task NotifyReaderForLoanAsync(int maPhieuMuon, string tieuDe, string noiDung, string khoaSuKien,
        string? duongDan = null, string loaiThongBao = "loan", CancellationToken ct = default);
    Task NotifyReaderForExtensionAsync(int maGiaHan, string tieuDe, string noiDung, string khoaSuKien,
        string? duongDan = null, string loaiThongBao = "extension", CancellationToken ct = default);
    Task NotifyReaderForFineAsync(int maTraSach, string tieuDe, string noiDung, string khoaSuKien,
        string? duongDan = null, CancellationToken ct = default);
    Task NotifyStaffAsync(string tieuDe, string noiDung, string khoaSuKien,
        string? duongDan = null, string loaiThongBao = "loan", CancellationToken ct = default);
}
