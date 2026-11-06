// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Xây dựng IThanhToanService, định nghĩa các phương thức tạo giao dịch thanh toán tiền phạt (online hoặc tại quầy), đối soát kết quả từ cổng thanh toán, hủy giao dịch chờ, truy vấn danh sách giao dịch và thông tin bạn đọc có nợ.
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Common;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;

namespace QuanLyThuVien_UNETI4_TI17A1CL.ViewModels;

public sealed class ThanhToanListViewModel
{
    public string? Search { get; set; }
    public PhuongThucThanhToan? PhuongThuc { get; set; }
    public TrangThaiGiaoDich? TrangThai { get; set; }
    public DateOnly? Tu { get; set; }
    public DateOnly? Den { get; set; }
    public PagedResult<GiaoDichListItem> Result { get; set; } = default!;
    public PagerViewModel Pager { get; set; } = default!;
}

public sealed class DebtorListViewModel
{
    public string? Search { get; set; }
    public PagedResult<DebtorItem> Result { get; set; } = default!;
    public PagerViewModel Pager { get; set; } = default!;
}

public sealed class ThuPhatBanDocViewModel
{
    public int MaBanDoc { get; set; }
    public string HoTen { get; set; } = string.Empty;
    public string? MaSoThe { get; set; }
    public IReadOnlyList<UnpaidFine> Fines { get; set; } = Array.Empty<UnpaidFine>();
}

public static class ThanhToanUi
{
    public static string BadgeClass(TrangThaiGiaoDich t) => t switch
    {
        TrangThaiGiaoDich.ThanhCong => "badge-soft-success",
        TrangThaiGiaoDich.ChoThanhToan => "badge-soft-warning",
        TrangThaiGiaoDich.ThatBai => "badge-soft-danger",
        TrangThaiGiaoDich.CanDoiSoat => "badge-soft-danger",
        _ => "badge-soft-secondary"
    };
}
