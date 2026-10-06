// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Xây dựng data model PhieuMuon

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Models;

[Table("PhieuMuon")]
public class PhieuMuon
{
    [Key] public int MaPhieuMuon { get; set; }

    public int MaBanDoc { get; set; }
    public int MaSach { get; set; }

    [DataType(DataType.Date)] [Display(Name = "Ngày đăng ký")]
    public DateOnly NgayDangKy { get; set; }

    [DataType(DataType.Date)] [Display(Name = "Ngày mượn")]
    public DateOnly? NgayMuon { get; set; }

    [DataType(DataType.Date)] [Display(Name = "Hạn trả")]
    public DateOnly? HanTra { get; set; }

    [StringLength(100)] [Display(Name = "Ghi chú")]
    public string? GhiChu { get; set; }

    [Display(Name = "Trạng thái")]
    public TrangThaiPhieuMuon TrangThai { get; set; } = TrangThaiPhieuMuon.ChoDuyet;

    [DataType(DataType.Date)] [Display(Name = "Ngày xử lý")]
    public DateOnly? NgayXuLy { get; set; }

    [StringLength(100)] [Display(Name = "Nhận xét thủ thư")]
    public string? NhanXetThuThu { get; set; }

    public BanDoc? BanDoc { get; set; }
    public Sach? Sach { get; set; }
    public ICollection<GiaHanMuon> GiaHanMuons { get; set; } = new List<GiaHanMuon>();
    public TraSach? TraSach { get; set; }
    public ICollection<PhieuMuonLichSu> LichSus { get; set; } = new List<PhieuMuonLichSu>();
}
