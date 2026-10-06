// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Xây dựng data model PhieuMuonLichSu

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Models;

// Nhật ký đổi trạng thái phiếu mượn (audit trail)
[Table("PhieuMuonLichSu")]
public class PhieuMuonLichSu
{
    [Key] public int MaLichSu { get; set; }
    [ForeignKey(nameof(PhieuMuon))]
    public int MaPhieuMuon { get; set; }

    public TrangThaiPhieuMuon? TrangThaiCu { get; set; }
    public TrangThaiPhieuMuon TrangThaiMoi { get; set; }

    // Gồm cả giờ để sắp xếp các lần đổi trạng thái trong cùng một ngày (datetime2)
    public DateTime NgayThucHien { get; set; }

    // MaTaiKhoan của người thực hiện (FK tới TaiKhoan)
    [ForeignKey(nameof(NguoiThucHien))]
    public int MaNguoiThucHien { get; set; }

    [StringLength(100)] [Display(Name = "Ghi chú")]
    public string? GhiChu { get; set; }

    public PhieuMuon? PhieuMuon { get; set; }
    public TaiKhoan? NguoiThucHien { get; set; }
}
