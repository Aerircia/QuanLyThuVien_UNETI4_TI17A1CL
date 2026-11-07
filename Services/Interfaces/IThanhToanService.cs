// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Xây dựng IThanhToanService, định nghĩa các phương thức tạo giao dịch thanh toán tiền phạt (online hoặc tại quầy), đối soát kết quả từ cổng thanh toán, hủy giao dịch chờ, truy vấn danh sách giao dịch và thông tin bạn đọc có nợ.
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Common;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;

public sealed record ThanhToanFilter(
    string? Search,
    PhuongThucThanhToan? PhuongThuc,
    TrangThaiGiaoDich? TrangThai,
    DateOnly? Tu,
    DateOnly? Den,
    int Page = 1,
    int PageSize = PagingDefaults.DefaultPageSize);

public sealed record GiaoDichListItem(
    int MaGiaoDich,
    long? OrderCode,
    int MaBanDoc,
    string HoTen,
    string? MaSoThe,
    decimal TongTien,
    int SoKhoan,
    PhuongThucThanhToan PhuongThuc,
    TrangThaiGiaoDich TrangThai,
    DateTime NgayTao,
    DateTime? NgayThanhToan,
    string? NguoiXacNhan);

public sealed record GiaoDichLine(
    int MaTra,
    int MaPhieuMuon,
    string TenSach,
    decimal SoTien,
    DateOnly NgayTra,
    TinhTrangSach TinhTrangSach,
    int SoNgayTre,
    TrangThaiPhat TrangThaiPhat);   // trạng thái HIỆN TẠI của khoản phạt, để truy vết từng khoản

public sealed record GiaoDichDetail(
    int MaGiaoDich,
    long? OrderCode,
    int MaBanDoc,
    string HoTen,
    string? MaSoThe,
    decimal TongTien,
    PhuongThucThanhToan PhuongThuc,
    TrangThaiGiaoDich TrangThai,
    string? NhaCungCap,
    string? CheckoutUrl,
    DateTime NgayTao,
    DateTime? NgayHetHan,
    DateTime? NgayThanhToan,
    string? NguoiXacNhan,
    string? GhiChu,
    IReadOnlyList<GiaoDichLine> Lines);

public sealed record DebtorItem(int MaBanDoc, string HoTen, string? MaSoThe, int SoKhoan, decimal TongNo);

public sealed record UnpaidFine(
    int MaTra,
    int MaPhieuMuon,
    string TenSach,
    DateOnly NgayTra,
    TinhTrangSach TinhTrangSach,
    int SoNgayTre,
    decimal TienPhat,
    bool DangCho);

public interface IThanhToanService
{
    bool IsOnlineEnabled { get; }

    /// <summary>Tạo giao dịch online cho các khoản phạt đã chọn; trả về URL thanh toán payOS.</summary>
    Task<ServiceResult<string>> CreateOnlineAsync(int maBanDoc, IReadOnlyCollection<int> maTras, CancellationToken ct = default);

    /// <summary>Hỏi payOS và cập nhật giao dịch (idempotent). Dùng cho Return URL, Status và webhook.</summary>
    Task<ServiceResult<TrangThaiGiaoDich>> SettleAsync(long orderCode, CancellationToken ct = default);

    /// <summary>Bạn đọc tự hủy giao dịch đang chờ của mình.</summary>
    Task<ServiceResult> CancelOnlineAsync(long orderCode, int maBanDoc, CancellationToken ct = default);

    /// <summary>Thu phạt tại quầy cho một hoặc nhiều khoản phạt của cùng một bạn đọc; trả về MaGiaoDich.</summary>
    Task<ServiceResult<int>> RecordCounterAsync(IReadOnlyCollection<int> maTras, int maNguoiXacNhan, CancellationToken ct = default);

    /// <summary>false nếu sai chữ ký; true nếu chữ ký hợp lệ (kể cả khi đơn không tồn tại).</summary>
    Task<bool> HandleWebhookAsync(string rawBody, CancellationToken ct = default);

    /// <summary>Đối soát giao dịch đang chờ của bạn đọc, rồi trả về các MaTra đang có giao dịch chờ.</summary>
    Task<HashSet<int>> GetPendingFineIdsAsync(int maBanDoc, CancellationToken ct = default);

    Task<PagedResult<GiaoDichListItem>> GetListAsync(ThanhToanFilter filter, int? maBanDoc, CancellationToken ct = default);

    Task<GiaoDichDetail?> GetAsync(int maGiaoDich, int? maBanDoc, CancellationToken ct = default);

    Task<GiaoDichDetail?> GetByOrderCodeAsync(long orderCode, int? maBanDoc, CancellationToken ct = default);

    Task<PagedResult<DebtorItem>> GetDebtorsAsync(string? search, int page, int pageSize, CancellationToken ct = default);

    Task<IReadOnlyList<UnpaidFine>> GetUnpaidFinesAsync(int maBanDoc, CancellationToken ct = default);
}
