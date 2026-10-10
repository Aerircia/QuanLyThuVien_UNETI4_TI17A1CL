// Họ và tên: Vũ Ngọc Hương Giang
// Mã sinh viên: 23103100280
// Nội dung thực hiện: Tính số ngày trễ và tiền phạt trả trễ .

using QuanLyThuVien_UNETI4_TI17A1CL.Models;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services;
public class TienPhatTreHanService : ITienPhatTreHanService
{
    public int TinhSoNgayTre(DateOnly? hanTra, DateOnly ngayTra) =>
        hanTra is { } h ? Math.Max(0, ngayTra.DayNumber - h.DayNumber) : 0;

    public decimal TinhTienPhat(CauHinhThuVien cfg, int soNgayTre) =>
        Math.Round(Math.Max(0, soNgayTre) * cfg.TienPhatMoiNgay, 0, MidpointRounding.AwayFromZero);

    public PhatTreHanResult Tinh(CauHinhThuVien cfg, DateOnly? hanTra, DateOnly ngayTra)
    {
        var soNgayTre = TinhSoNgayTre(hanTra, ngayTra);
        return new PhatTreHanResult(soNgayTre, TinhTienPhat(cfg, soNgayTre));
    }
}