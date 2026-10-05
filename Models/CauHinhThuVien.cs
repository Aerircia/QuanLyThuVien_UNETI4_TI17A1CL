// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Cấu hình thư viện (số ngày mượn, số phiếu tối đa, số lần gia hạn, tiền phạt, tỷ lệ phạt hư hỏng/mất, ...)

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Models;

// Bảng cấu hình 1 dòng duy nhất (Admin chỉnh sửa được), thay cho hằng số rải rác trong code
[Table("CauHinhThuVien")]
public class CauHinhThuVien
{
    [Key] public int MaCauHinh { get; set; }

    [Display(Name = "Số ngày mượn - Sinh viên")] public int SoNgayMuonSinhVien { get; set; } = 14;
    [Display(Name = "Số ngày mượn - Giảng viên")] public int SoNgayMuonGiangVien { get; set; } = 30;
    [Display(Name = "Số ngày mượn - Khác")] public int SoNgayMuonKhac { get; set; } = 14;
    [Display(Name = "Số phiếu tối đa")] public int SoPhieuToiDa { get; set; } = 5;
    [Display(Name = "Số lần gia hạn tối đa")] public int SoLanGiaHanToiDa { get; set; } = 2;
    [Display(Name = "Số ngày gia hạn tối đa")] public int SoNgayGiaHanToiDa { get; set; } = 7;
    [Display(Name = "Tiền phạt mỗi ngày")] public decimal TienPhatMoiNgay { get; set; } = 2000m;
    [Display(Name = "Tỷ lệ phạt hư hỏng")] public decimal TyLePhatHuHong { get; set; } = 0.5m;
    [Display(Name = "Tỷ lệ phạt mất")] public decimal TyLePhatMat { get; set; } = 1.0m;
}
