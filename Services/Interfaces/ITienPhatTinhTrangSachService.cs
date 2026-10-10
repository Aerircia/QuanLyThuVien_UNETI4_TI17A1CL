// Họ và tên: Vũ Ngọc Hương Giang
// Mã sinh viên: 23103100280
// Nội dung thực hiện: interface tính tiền phạt theo tình trạng sách

using QuanLyThuVien_UNETI4_TI17A1CL.Models;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;

public interface ITienPhatTinhTrangSachService
{
    decimal TinhTienPhat(CauHinhThuVien cfg, TinhTrangSach tinhTrang, decimal giaBia);
}