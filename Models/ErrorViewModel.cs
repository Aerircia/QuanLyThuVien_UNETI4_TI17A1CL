// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Bổ sung mã trạng thái HTTP cho model trang lỗi.

namespace QuanLyThuVien_UNETI4_TI17A1CL.Models;

public class ErrorViewModel
{
    public string? RequestId { get; set; }

    public int StatusCode { get; set; } = 500;

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
