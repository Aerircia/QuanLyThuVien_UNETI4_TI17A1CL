// Họ và tên: Phùng Ngọc Bảo
// Mã sinh viên: 23103100278
// Nội dung thực hiện: Xây dựng data model CaiDatThongBao
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Models;

[Table("CaiDatThongBao")]
public class CaiDatThongBao
{
    [Key] public int MaCaiDat { get; set; }
    [Required] public int MaTaiKhoan { get; set; }
    public bool BatThongBaoWeb { get; set; } = true;
    public bool BatThongBaoEmail { get; set; } = true;
    public bool NhomMuonTra { get; set; } = true;
    public bool NhomGiaHan { get; set; } = true;
    public bool NhomTienPhat { get; set; } = true;
    public bool NhomHeThong { get; set; } = true;
    public DateTime NgayCapNhat { get; set; } = DateTime.UtcNow;
    public TaiKhoan? TaiKhoan { get; set; }
}
