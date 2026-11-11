// Họ và tên: Phùng Ngọc Bảo
// Mã sinh viên: 23103100278
// Nội dung thực hiện: Xây dựng data model ThongBao
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Models;

[Table("ThongBao")]
public class ThongBao
{
    [Key] public int MaThongBao { get; set; }
    [Required] public int MaTaiKhoan { get; set; }
    [Required, StringLength(120)] public string TieuDe { get; set; } = string.Empty;
    [Required, StringLength(1000)] public string NoiDung { get; set; } = string.Empty;
    [Required, StringLength(30)] public string LoaiThongBao { get; set; } = "loan";
    [Required, StringLength(20)] public string MucDo { get; set; } = "in";
    [StringLength(40)] public string? Icon { get; set; }
    [StringLength(40)] public string? LoaiDoiTuong { get; set; }
    public int? MaDoiTuong { get; set; }
    [StringLength(500)] public string? DuongDan { get; set; }
    [Required, StringLength(160)] public string KhoaSuKien { get; set; } = string.Empty;
    public DateTime NgayTao { get; set; } = DateTime.UtcNow;
    public bool DaDoc { get; set; }
    public DateTime? NgayDoc { get; set; }
    public bool DangHienThi { get; set; } = true;
    public TaiKhoan? TaiKhoan { get; set; }
}
