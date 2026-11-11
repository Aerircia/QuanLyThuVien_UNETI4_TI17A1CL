// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Xây dựng các ViewModel cho màn hình quản lý thanh toán tiền phạt, bao gồm danh sách giao dịch, danh sách bạn đọc có nợ và chi tiết giao dịch.
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
    public string Sort { get; set; } = "newest";
    public PagedResult<GiaoDichListItem> Result { get; set; } = default!;
    public PagerViewModel Pager { get; set; } = default!;
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
