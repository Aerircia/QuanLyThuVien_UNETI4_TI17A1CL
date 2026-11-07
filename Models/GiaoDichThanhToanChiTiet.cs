// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Xây dựng entity GiaoDichThanhToanChiTiet, lưu trữ thông tin một khoản phạt (TraSach) nằm trong một giao dịch thanh toán.
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Models;

/// <summary>Một khoản phạt (TraSach) nằm trong một giao dịch thanh toán.</summary>
public class GiaoDichThanhToanChiTiet
{
    [Key]
    public int MaChiTiet { get; set; }

    [ForeignKey(nameof(GiaoDich))]
    public int MaGiaoDich { get; set; }
    public GiaoDichThanhToan? GiaoDich { get; set; }

    [ForeignKey(nameof(TraSach))]
    public int MaTra { get; set; }
    public TraSach? TraSach { get; set; }

    /// <summary>Số tiền tại thời điểm tạo giao dịch (snapshot của TraSach.TienPhat).</summary>
    public decimal SoTien { get; set; }

    /// <summary>True khi giao dịch cha đang ChoThanhToan; chỉ cho phép một dòng DangCho cho mỗi khoản phạt.</summary>
    public bool DangCho { get; set; }
}
