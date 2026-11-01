// Họ và tên: Phùng Ngọc Bảo
// Mã sinh viên: 23103100278
// Nội dung thực hiện: ViewModel thống kê
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;

namespace QuanLyThuVien_UNETI4_TI17A1CL.ViewModels;

public sealed record ThongKeDashboardViewModel(
    DashboardSummary Summary,
    ThongKeReport Report);
