// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Interface dịch vụ trang chủ và tra cứu kho sách cho khách/bạn đọc

using QuanLyThuVien_UNETI4_TI17A1CL.ViewModels;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;

public interface IHomeService
{
    Task<LandingViewModel> GetLandingAsync(CancellationToken ct = default);

    /// <summary>Tìm kiếm + lọc + sắp xếp + phân trang chạy trong SQL (Skip/Take).</summary>
    Task<CatalogViewModel> GetCatalogAsync(CatalogFilter filter, int page, int pageSize, CancellationToken ct = default);

    Task<BookDetailViewModel?> GetBookAsync(int maSach, CancellationToken ct = default);
}
