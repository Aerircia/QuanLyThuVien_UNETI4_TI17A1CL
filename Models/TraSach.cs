// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Xây dựng data model TraSach

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Models;

[Table("TraSach")]
public class TraSach
{
    [Key] public int MaTra { get; set; }
    [ForeignKey(nameof(PhieuMuon))]
    public int MaPhieuMuon { get; set; }

    [DataType(DataType.Date)] [Display(Name = "Ngày trả")]
    public DateOnly NgayTra { get; set; }

    [Display(Name = "Tình trạng sách")]
    public TinhTrangSach TinhTrangSach { get; set; } = TinhTrangSach.BinhThuong;

    [Display(Name = "Số ngày trễ")]
    public int SoNgayTre { get; set; }

    [Display(Name = "Tiền phạt")]
    public decimal TienPhat { get; set; }

    [Display(Name = "Trạng thái phạt")]
    public TrangThaiPhat TrangThaiPhat { get; set; } = TrangThaiPhat.KhongPhat;

    [StringLength(100)] [DataType(DataType.MultilineText)] [Display(Name = "Ghi chú")]
    public string? GhiChu { get; set; }

    // Thủ thư nhận sách (lấy từ Session, không cho người dùng nhập)
    [ForeignKey(nameof(NguoiNhan))] [Display(Name = "Thủ thư nhận")]
    public int MaNguoiNhan { get; set; }

    public PhieuMuon? PhieuMuon { get; set; }
    public TaiKhoan? NguoiNhan { get; set; }
}
