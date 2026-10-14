// Họ và tên: Vũ Tiến Thành
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Khai báo dữ liệu form thêm/sửa sách, xác thực tiếng Việt, thông tin ảnh bìa, phiên bản đồng thời và lựa chọn thể loại.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace QuanLyThuVien_UNETI4_TI17A1CL.ViewModels;

public sealed class SachFormViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tên sách")]
    [StringLength(255, ErrorMessage = "Tên sách không được vượt quá 255 ký tự")]
    [Display(Name = "Tên sách")]
    public string TenSach { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập tác giả")]
    [StringLength(255, ErrorMessage = "Tên tác giả không được vượt quá 255 ký tự")]
    [Display(Name = "Tác giả")]
    public string TacGia { get; set; } = string.Empty;

    [StringLength(20, ErrorMessage = "ISBN không được vượt quá 20 ký tự")]
    [Display(Name = "ISBN / Mã sách")]
    public string? ISBN { get; set; }

    [StringLength(255, ErrorMessage = "Nhà xuất bản không được vượt quá 255 ký tự")]
    [Display(Name = "Nhà xuất bản")]
    public string? NhaXuatBan { get; set; }

    [Range(1450, 2100, ErrorMessage = "Năm xuất bản không hợp lệ")]
    [Display(Name = "Năm xuất bản")]
    public int NamXuatBan { get; set; } = DateTime.Now.Year;

    [Range(0, int.MaxValue, ErrorMessage = "Số lượng tổng phải lớn hơn hoặc bằng 0")]
    [Display(Name = "Số lượng tổng")]
    public int SoLuongTong { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Giá bìa phải lớn hơn hoặc bằng 0")]
    [Display(Name = "Giá bìa")]
    public decimal GiaBia { get; set; }

    [StringLength(255, ErrorMessage = "Vị trí kệ không được vượt quá 255 ký tự")]
    [Display(Name = "Vị trí kệ")]
    public string? ViTriKe { get; set; }

    [StringLength(2000, ErrorMessage = "Mô tả không được vượt quá 2000 ký tự")]
    [Display(Name = "Mô tả")]
    public string? MoTa { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn thể loại")]
    [Display(Name = "Thể loại")]
    public int MaTheLoai { get; set; }

    [Display(Name = "Ảnh bìa")]
    public IFormFile? AnhBia { get; set; }

    public string? AnhBiaHienTai { get; set; }
    public string? RowVersion { get; set; }
    public IReadOnlyList<TheLoaiOptionViewModel> TheLoaiOptions { get; set; } = [];
}
