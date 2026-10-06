// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Xây dựng data model GiaHanMuon

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Models;

[Table("GiaHanMuon")]
public class GiaHanMuon
{
    [Key] public int MaGiaHan { get; set; }
    public int MaPhieuMuon { get; set; }

    [DataType(DataType.Date)] [Display(Name = "Hạn trả cũ")]
    public DateOnly HanTraCu { get; set; }

    [DataType(DataType.Date)] [Display(Name = "Hạn trả mới")]
    public DateOnly HanTraMoi { get; set; }

    [StringLength(100)] [Display(Name = "Lý do")]
    public string? LyDo { get; set; }

    [DataType(DataType.Date)] [Display(Name = "Ngày yêu cầu")]
    public DateOnly NgayYeuCau { get; set; }

    [Display(Name = "Trạng thái")]
    public TrangThaiGiaHan TrangThai { get; set; } = TrangThaiGiaHan.ChoDuyet;

    // Do hệ thống ghi khi Thủ thư duyệt/từ chối yêu cầu gia hạn
    [DataType(DataType.Date)] [Display(Name = "Ngày xử lý")]
    public DateOnly? NgayXuLy { get; set; }

    [StringLength(100)] [Display(Name = "Nhận xét")]
    public string? NhanXet { get; set; }

    public PhieuMuon? PhieuMuon { get; set; }
}
