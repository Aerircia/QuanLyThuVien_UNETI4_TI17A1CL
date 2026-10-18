// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Xây dựng ViewModel để quản lý cấu hình thư viện

using System.ComponentModel.DataAnnotations;

namespace QuanLyThuVien_UNETI4_TI17A1CL.ViewModels;

public class CauHinhThuVienViewModel
{
    [Display(Name = "Số ngày mượn - Sinh viên")]
    [Required(ErrorMessage = "{0} là bắt buộc")]
    [Range(1, 365, ErrorMessage = "{0} phải từ {1} đến {2}")]
    public int SoNgayMuonSinhVien { get; set; }

    [Display(Name = "Số ngày mượn - Giảng viên")]
    [Required(ErrorMessage = "{0} là bắt buộc")]
    [Range(1, 365, ErrorMessage = "{0} phải từ {1} đến {2}")]
    public int SoNgayMuonGiangVien { get; set; }

    [Display(Name = "Số ngày mượn - Khác")]
    [Required(ErrorMessage = "{0} là bắt buộc")]
    [Range(1, 365, ErrorMessage = "{0} phải từ {1} đến {2}")]
    public int SoNgayMuonKhac { get; set; }

    [Display(Name = "Số phiếu đang xử lý tối đa")]
    [Required(ErrorMessage = "{0} là bắt buộc")]
    [Range(1, 20, ErrorMessage = "{0} phải từ {1} đến {2}")]
    public int SoPhieuToiDa { get; set; }

    [Display(Name = "Số lần gia hạn tối đa")]
    [Required(ErrorMessage = "{0} là bắt buộc")]
    [Range(0, 10, ErrorMessage = "{0} phải từ {1} đến {2}")]
    public int SoLanGiaHanToiDa { get; set; }

    [Display(Name = "Số ngày gia hạn tối đa mỗi lần")]
    [Required(ErrorMessage = "{0} là bắt buộc")]
    [Range(1, 30, ErrorMessage = "{0} phải từ {1} đến {2}")]
    public int SoNgayGiaHanToiDa { get; set; }

    [Display(Name = "Tiền phạt trả trễ mỗi ngày (đồng)")]
    [Required(ErrorMessage = "{0} là bắt buộc")]
    [Range(typeof(decimal), "0", "1000000", ErrorMessage = "{0} phải từ {1} đến {2}")]
    public decimal TienPhatMoiNgay { get; set; }

    [Display(Name = "Phạt sách hư hỏng (% giá bìa)")]
    [Required(ErrorMessage = "{0} là bắt buộc")]
    [Range(typeof(decimal), "0", "100", ErrorMessage = "{0} phải từ {1} đến {2}")]
    public decimal PhanTramPhatHuHong { get; set; }

    [Display(Name = "Phạt sách mất (% giá bìa)")]
    [Required(ErrorMessage = "{0} là bắt buộc")]
    [Range(typeof(decimal), "0", "100", ErrorMessage = "{0} phải từ {1} đến {2}")]
    public decimal PhanTramPhatMat { get; set; }
}
