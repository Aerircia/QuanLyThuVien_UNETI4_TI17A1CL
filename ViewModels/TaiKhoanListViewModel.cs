// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Tạo ViewModel cho việc liệt kê tài khoản người dùng trong ứng dụng quản lý thư viện.

using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Common;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;

namespace QuanLyThuVien_UNETI4_TI17A1CL.ViewModels;

public class TaiKhoanListViewModel
{
    public string? Search { get; set; }
    public VaiTro? VaiTro { get; set; }
    public TrangThaiTaiKhoan? TrangThai { get; set; }
    public string Sort { get; set; } = "username";
    public int PageSize { get; set; } = PagingDefaults.DefaultPageSize;

    /// <summary>Tài khoản đang đăng nhập (View dùng để ẩn nút khóa/xóa chính mình; service vẫn kiểm tra lại).</summary>
    public int CurrentMaTaiKhoan { get; set; }

    public PagedResult<TaiKhoanListItem> Result { get; set; } = default!;
    public PagerViewModel Pager { get; set; } = default!;
}
