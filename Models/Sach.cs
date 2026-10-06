// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Xây dựng data model Sach

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Models;

[Table("Sach")]
public class Sach
{
    [Key] public int MaSach { get; set; }

    [ForeignKey(nameof(TheLoai))]
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn thể loại")]
    [Display(Name = "Thể loại")]
    public int MaTheLoai { get; set; }

    [Required(ErrorMessage = "Tên sách là bắt buộc"), StringLength(255)]
    [Display(Name = "Tên sách")]
    public string TenSach { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tác giả là bắt buộc"), StringLength(255)]
    [Display(Name = "Tác giả")]
    public string TacGia { get; set; } = string.Empty;

    [StringLength(255)]
    [Display(Name = "Nhà xuất bản")]
    public string? NhaXuatBan { get; set; }

    // Giới hạn trên (năm hiện tại) kiểm tra ở service
    [Range(1900, 2100, ErrorMessage = "Năm xuất bản không hợp lệ")]
    [Display(Name = "Năm xuất bản")]
    public int NamXuatBan { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Số lượng tổng phải > 0")]
    [Display(Name = "Số lượng tổng")]
    public int SoLuongTong { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Giá bìa phải >= 0")]
    [Display(Name = "Giá bìa")]
    public decimal GiaBia { get; set; }

    [StringLength(255)]
    [Display(Name = "Vị trí kệ")]
    public string? ViTriKe { get; set; }

    [StringLength(2000)]
    [DataType(DataType.MultilineText)]
    [Display(Name = "Mô tả")]
    public string? MoTa { get; set; }

    [Display(Name = "Trạng thái")]
    public TrangThaiSach TrangThai { get; set; } = TrangThaiSach.DangPhucVu;

    [StringLength(255)]
    [Display(Name = "Ảnh bìa")]
    public string? ImgPath { get; set; }

    // KHÔNG có cột SoLuongConLai: tính bằng LINQ từ SoLuongTong và PhieuMuons
    public TheLoai? TheLoai { get; set; }
    public ICollection<PhieuMuon> PhieuMuons { get; set; } = new List<PhieuMuon>();
}
