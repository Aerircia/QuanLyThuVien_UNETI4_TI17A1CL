// Họ và tên: Vũ Ngọc Hương Giang
// Mã sinh viên: 23103100280
// Nội dung thực hiện: interface nhắc nhở bạn đọc từ trang Trả sách / Gia hạn: nhắc trả sách, nhắc gia hạn, nhắc nộp phạt.

using QuanLyThuVien_UNETI4_TI17A1CL.Services.Common;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;

public interface INhacNhoService
{
    //Nhắc bạn đọc trả sách cho phiếu đang mượn (quá hạn hoặc sắp đến hạn)
    Task<ServiceResult<string>> NhacTraSachAsync(int maPhieuMuon, CancellationToken ct = default);

    //Nhắc bạn đọc có thể gửi yêu cầu gia hạn thêm thời gian 
    Task<ServiceResult<string>> NhacGiaHanAsync(int maPhieuMuon, CancellationToken ct = default);

    //Nhắc bạn đọc nộp khoản phạt chưa nộp của một bản ghi trả sách
    Task<ServiceResult<string>> NhacNopPhatAsync(int maTra, CancellationToken ct = default);
    
    Task<Dictionary<string, TimeOnly>> GetThoiGianDaNhacHomNayAsync(IEnumerable<string> danhSachKhoa, CancellationToken ct = default);
}