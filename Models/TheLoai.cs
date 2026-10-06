// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Xây dựng data model TheLoai

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Models;

[Table("TheLoai")]
public class TheLoai
{
    [Key] public int MaTheLoai { get; set; }

    [Required(ErrorMessage = "Tên thể loại là bắt buộc"), StringLength(255)]
    [Display(Name = "Tên thể loại")]
    public string TenTheLoai { get; set; } = string.Empty;

    [StringLength(255)]
    [Display(Name = "Mô tả")]
    public string? MoTa { get; set; }

    [StringLength(100, ErrorMessage = "Tối đa 100 ký tự")]
    [Display(Name = "Khu vực lưu trữ")]
    public string? KhuVucLuuTru { get; set; }

    [Display(Name = "Trạng thái")]
    public TrangThaiTheLoai TrangThai { get; set; } = TrangThaiTheLoai.HoatDong;

    public ICollection<Sach> Sachs { get; set; } = new List<Sach>();
}
