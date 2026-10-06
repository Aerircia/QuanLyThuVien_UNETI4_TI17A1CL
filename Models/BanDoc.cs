// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Xây dựng data model BanDoc

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Models;

[Table("BanDoc")]
public class BanDoc
{
    [Key] public int MaBanDoc { get; set; }

    public int MaTaiKhoan { get; set; }

    [Required, StringLength(255)]
    [Display(Name = "Họ tên")]
    public string HoTen { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    [Display(Name = "Ngày sinh")]
    public DateOnly NgaySinh { get; set; }

    [Display(Name = "Giới tính")]
    public GioiTinh GioiTinh { get; set; } = GioiTinh.Nam;

    [Phone, StringLength(20)]
    [Display(Name = "Số điện thoại")]
    public string? SoDienThoai { get; set; }

    [EmailAddress, StringLength(255)]
    public string? Email { get; set; }

    [StringLength(255)]
    [Display(Name = "Địa chỉ")]
    public string? DiaChi { get; set; }

    // Do DB tự sinh (SEQUENCE SoTheSeq, dạng TV-000123); cấu hình trong AppDbContext. Để null khi thêm mới.
    [Display(Name = "Mã số thẻ")]
    public string? MaSoThe { get; set; }

    [Display(Name = "Loại bạn đọc")]
    public LoaiBanDoc LoaiBanDoc { get; set; } = LoaiBanDoc.SinhVien;

    [DataType(DataType.Date)]
    [Display(Name = "Ngày cấp thẻ")]
    public DateOnly NgayCapThe { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Ngày hết hạn thẻ")]
    public DateOnly NgayHetHanThe { get; set; }

    [Display(Name = "Trạng thái")]
    public TrangThaiBanDoc TrangThai { get; set; } = TrangThaiBanDoc.HoatDong;

    [StringLength(255)]
    public string? ImgPath { get; set; }

    public TaiKhoan? TaiKhoan { get; set; }
    public ICollection<PhieuMuon> PhieuMuons { get; set; } = new List<PhieuMuon>();
}
