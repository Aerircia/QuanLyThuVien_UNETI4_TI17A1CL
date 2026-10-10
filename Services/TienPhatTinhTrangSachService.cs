// Họ và tên: Vũ Ngọc Hương Giang
// Mã sinh viên: 23103100280
// Nội dung thực hiện: tính tiền phạt theo tình trạng sách

using QuanLyThuVien_UNETI4_TI17A1CL.Models;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services;
public class TienPhatTinhTrangSachService : ITienPhatTinhTrangSachService
{
    public decimal TinhTienPhat(CauHinhThuVien cfg, TinhTrangSach tinhTrang, decimal giaBia)
    {
        var phat = tinhTrang switch
        {
            TinhTrangSach.HuHong => giaBia * cfg.TyLePhatHuHong,
            TinhTrangSach.Mat => giaBia * cfg.TyLePhatMat,
            _ => 0m
        };
        return Math.Round(phat, 0, MidpointRounding.AwayFromZero);
    }
}