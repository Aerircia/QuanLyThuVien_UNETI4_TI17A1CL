// Họ và tên: Vũ Ngọc Hương Giang
// Mã sinh viên: 23103100280
// Nội dung thực hiện: dữ liệu cho partial nút nhắc nhở 

namespace QuanLyThuVien_UNETI4_TI17A1CL.ViewModels;
public record NutNhacNhoViewModel(
    string Action, 
    int Id, 
    string TenBanDoc, 
    string TenSach, 
    string Nhan, 
    string Lop, 
    TimeOnly? DaNhacLuc = null
);