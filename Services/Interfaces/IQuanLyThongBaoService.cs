// Họ và tên: Vũ Ngọc Hương Giang
// Mã sinh viên: 23103100280
// Nội dung thực hiện: Interface quản lý thông báo của Admin/Thủ thư: xem mọi thông báo, ẩn/hiện, gửi thông báo chung.

using QuanLyThuVien_UNETI4_TI17A1CL.Services.Common;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;

public sealed record QuanLyThongBaoItem(
    int MaThongBao, string LoaiThongBao, string MucDo, string TieuDe, string NoiDung,
    string? DuongDan, bool DaDoc, bool DangHienThi, DateTime NgayTao, string NguoiNhan);

// NhomNhan: "BanDoc" | "NhanVien" | null (mọi người)
public sealed record QuanLyThongBaoFilter(string? Search, string? Loai, string? NhomNhan, bool? DangHienThi, int Page, int PageSize);

// NhomNhan: BanDoc | NhanVien | TatCa
public sealed record GuiThongBaoInput(string TieuDe, string NoiDung, string MucDo, string NhomNhan);

public interface IQuanLyThongBaoService
{
    /// <summary>Xem thông báo của mọi tài khoản (kể cả đã ẩn), lọc và phân trang.</summary>
    Task<PagedResult<QuanLyThongBaoItem>> GetQuanLyAsync(QuanLyThongBaoFilter filter, CancellationToken ct = default);

    Task<ServiceResult> DoiHienThiAsync(int maThongBao, bool hienThi, CancellationToken ct = default);

    /// <summary>Gửi thông báo hệ thống (sys) tới nhóm người nhận; trả về số người nhận.</summary>
    Task<ServiceResult<int>> GuiThongBaoChungAsync(GuiThongBaoInput input, CancellationToken ct = default);
}