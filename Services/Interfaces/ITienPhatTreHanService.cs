// Họ và tên: Vũ Ngọc Hương Giang
// Mã sinh viên: 23103100280
// Nội dung thực hiện: interface tính số ngày trễ và tiền phạt trả trễ 

using QuanLyThuVien_UNETI4_TI17A1CL.Models;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;
public sealed record PhatTreHanResult(int SoNgayTre, decimal TienPhat);

public interface ITienPhatTreHanService
{
    int TinhSoNgayTre(DateOnly? hanTra, DateOnly ngayTra);
    decimal TinhTienPhat(CauHinhThuVien cfg, int soNgayTre);
    PhatTreHanResult Tinh(CauHinhThuVien cfg, DateOnly? hanTra, DateOnly ngayTra);
}