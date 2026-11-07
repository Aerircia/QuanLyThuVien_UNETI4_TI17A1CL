// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Xây dựng entity GiaoDichThanhToan, lưu trữ thông tin một lần thanh toán tiền phạt (online hoặc tại quầy), có thể gồm nhiều khoản phạt.
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Models;

/// <summary>Một lần thanh toán tiền phạt (online hoặc tại quầy), có thể gồm nhiều khoản phạt.</summary>
public class GiaoDichThanhToan
{
    [Key]
    public int MaGiaoDich { get; set; }

    /// <summary>Mã đơn gửi payOS (số nguyên duy nhất, ngẫu nhiên). Null với thanh toán tại quầy.</summary>
    public long? OrderCode { get; set; }

    [ForeignKey(nameof(BanDoc))]
    public int MaBanDoc { get; set; }
    public BanDoc? BanDoc { get; set; }

    public decimal TongTien { get; set; }
    public PhuongThucThanhToan PhuongThuc { get; set; }
    public TrangThaiGiaoDich TrangThai { get; set; }

    [StringLength(50)] public string? NhaCungCap { get; set; }
    /// <summary>paymentLinkId của payOS.</summary>
    [StringLength(100)] public string? MaDonHangNhaCungCap { get; set; }
    /// <summary>Mã tham chiếu ngân hàng (nếu có).</summary>
    [StringLength(100)] public string? MaGiaoDichNhaCungCap { get; set; }
    [StringLength(500)] public string? CheckoutUrl { get; set; }

    public DateTime NgayTao { get; set; }
    public DateTime? NgayHetHan { get; set; }
    public DateTime? NgayThanhToan { get; set; }

    [ForeignKey(nameof(NguoiXacNhan))]
    public int? MaNguoiXacNhan { get; set; }
    public TaiKhoan? NguoiXacNhan { get; set; }

    [StringLength(100)] public string? GhiChu { get; set; }

    public ICollection<GiaoDichThanhToanChiTiet> ChiTiets { get; set; } = new List<GiaoDichThanhToanChiTiet>();
}
