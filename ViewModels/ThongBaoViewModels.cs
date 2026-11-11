// Họ và tên: Phùng Ngọc Bảo
// Mã sinh viên: 23103100278
// Nội dung thực hiện: ViewModel trang xem tất cả thông báo

using QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;
using QuanLyThuVien_UNETI4_TI17A1CL.Models;

namespace QuanLyThuVien_UNETI4_TI17A1CL.ViewModels;

public sealed class ThongBaoIndexViewModel
{
    public IReadOnlyList<ThongBaoItem> DanhSach { get; set; } = Array.Empty<ThongBaoItem>();
    public string BoLoc { get; set; } = "all";
    public int TongSo { get; set; }
    public int ChuaDoc { get; set; }
    public int TongTheoBoLoc { get; set; }
    public int Trang { get; set; } = 1;
    public int SoTrang { get; set; } = 1;
    public CaiDatThongBao CaiDat { get; set; } = new();
}

public sealed class ThongBaoSettingsInputModel
{
    public bool BatThongBaoWeb { get; set; }
    public bool BatThongBaoEmail { get; set; }
    public bool NhomMuonTra { get; set; }
    public bool NhomGiaHan { get; set; }
    public bool NhomTienPhat { get; set; }
    public bool NhomHeThong { get; set; }
}
