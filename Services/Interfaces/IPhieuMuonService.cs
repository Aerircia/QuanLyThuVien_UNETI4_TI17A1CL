// Họ và tên: Nguyễn Hữu Tuấn
// Mã sinh viên: 23103100136
// Nội dung thực hiện: Xây dựng interface cho dịch vụ quản lý phiếu mượn và đăng ký mượn sách

using QuanLyThuVien_UNETI4_TI17A1CL.Services.Common;
using QuanLyThuVien_UNETI4_TI17A1CL.ViewModels;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;

public sealed record PhieuMuonManageSummary(int ChoDuyet, int DaDuyet, int DangMuon, int QuaHan, int ChuaNhanSach, int HoanThanh = 0);

public static class LoanRules
{
    /// <summary>Phiếu Đã duyệt quá số ngày này mà bạn đọc chưa đến nhận sách thì được liệt kê ở danh sách "chưa nhận sách".</summary>
    public const int UncollectedDays = 7;
}

public interface IPhieuMuonService
{
    //Bạn đọc: Đăng ký & Theo dõi phiếu mượn
    
    /// <summary>
    /// Kiểm tra 7 điều kiện bằng EF Core / LINQ trước khi tạo phiếu mượn.
    /// </summary>
    Task<LoanEligibilityResult> CheckCanBorrowAsync(int maBanDoc, int maSach, CancellationToken ct = default);

    /// <summary>
    /// Đăng ký mượn sách: hệ thống ghi nhận NgayDangKy, trạng thái Chờ duyệt.
    /// </summary>
    Task<ServiceResult<int>> RegisterLoanAsync(int maBanDoc, int maSach, string? ghiChu, CancellationToken ct = default);

    /// <summary>
    /// Danh sách phiếu mượn của bạn đọc hiện tại (xác định trạng thái quá hạn bằng LINQ).
    /// </summary>
    Task<List<PhieuMuonItemViewModel>> GetMyLoansAsync(int maBanDoc, string? filter = null, CancellationToken ct = default);

    /// <summary>
    /// Xem chi tiết phiếu mượn. Nếu requiredMaBanDoc được truyền vào, bắt buộc phiếu phải thuộc về bạn đọc đó (chống lộ thông tin qua URL).
    /// </summary>
    Task<PhieuMuonDetailViewModel?> GetLoanDetailAsync(int maPhieuMuon, int? requiredMaBanDoc = null, CancellationToken ct = default);

    /// <summary>
    /// Bạn đọc hủy phiếu của chính mình khi ở trạng thái Chờ duyệt hoặc Đã duyệt nhưng chưa nhận sách.
    /// </summary>
    Task<ServiceResult> CancelLoanAsync(int maPhieuMuon, int maBanDoc, CancellationToken ct = default);

    /// <summary>
    /// Lấy danh sách tiền phạt của bạn đọc hiện tại (từ các bản ghi trả sách).
    /// </summary>
    Task<MyFinesViewModel> GetMyFinesAsync(int maBanDoc, CancellationToken ct = default);

    //Thủ thư / Quản trị viên: Quản lý & Xác nhận mượn

    /// <summary>
    /// Danh sách phiếu mượn quản trị (phân trang, lọc theo trạng thái, thể loại, từ khóa, quá hạn).
    /// </summary>
    Task<PagedResult<PhieuMuonItemViewModel>> GetManageListAsync(PhieuMuonManageFilter filter, CancellationToken ct = default);

    /// <summary>
    /// Thủ thư duyệt phiếu mượn (chuyển sang Đã duyệt, ghi nhận ngày xử lý và nhận xét).
    /// </summary>
    Task<ServiceResult> ApproveLoanAsync(int maPhieuMuon, int actingMaTaiKhoan, string? nhanXet = null, CancellationToken ct = default);

    /// <summary>
    /// Thủ thư từ chối phiếu mượn (chuyển sang Từ chối, ghi nhận ngày xử lý và lý do).
    /// </summary>
    Task<ServiceResult> RejectLoanAsync(int maPhieuMuon, int actingMaTaiKhoan, string? nhanXet = null, CancellationToken ct = default);

    /// <summary>
    /// Thủ thư giao sách cho bạn đọc (chuyển sang Đang mượn, ghi nhận NgayMuon và HanTra theo cấu hình thư viện).
    /// </summary>
    Task<ServiceResult> DeliverBookAsync(int maPhieuMuon, int actingMaTaiKhoan, CancellationToken ct = default);

    /// <summary>
    /// Thủ thư / Admin hủy phiếu mượn đã duyệt nhưng chưa giao sách (hoặc phiếu chờ duyệt).
    /// </summary>
    Task<ServiceResult> CancelLoanByStaffAsync(int maPhieuMuon, int actingMaTaiKhoan, string? lyDo = null, CancellationToken ct = default);

    /// <summary>
    /// Tự động hủy tất cả phiếu mượn đã duyệt nhưng chưa đến nhận sách quá 7 ngày.
    /// </summary>
    Task<int> AutoCancelOverdueApprovedLoansAsync(CancellationToken ct = default);

    /// <summary>Số liệu nhanh cho trang quản lý phiếu mượn.</summary>
    Task<PhieuMuonManageSummary> GetManageSummaryAsync(CancellationToken ct = default);

    /// <summary>Phiếu Đã duyệt quá LoanRules.UncollectedDays ngày chưa được nhận sách (vẫn giữ một bản).</summary>
    Task<List<PhieuMuonItemViewModel>> GetUncollectedAsync(CancellationToken ct = default);
}
