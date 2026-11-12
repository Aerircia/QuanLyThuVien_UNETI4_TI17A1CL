// Họ và tên: Vũ Ngọc Hương Giang
// Mã sinh viên: 23103100280
// Nội dung thực hiện: Hàm hỗ trợ hiển thị trang quản lý thông báo: tên loại, tên mức độ, class badge.

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services.Notifications;

public static class ThongBaoHienThi
{
    public static readonly string[] CacLoai = { "loan", "extension", "due", "fine", "sys" };
    public static readonly string[] CacMucDo = { "in", "ok", "wa", "er" };

    public static string TenLoai(string? ma) => ma switch
    {
        "loan" => "Mượn trả",
        "extension" => "Gia hạn",
        "due" => "Hạn trả",
        "fine" => "Tiền phạt",
        "sys" => "Hệ thống",
        _ => "Khác"
    };

    public static string TenMucDo(string? ma) => ma switch
    {
        "er" => "Khẩn",
        "wa" => "Cảnh báo",
        "ok" => "Thành công",
        _ => "Thông tin"
    };

    public static string BadgeClass(string? mucDo) => mucDo switch
    {
        "er" => "badge-soft-danger",
        "wa" => "badge-soft-warning",
        "ok" => "badge-soft-success",
        _ => "badge-soft-secondary"
    };
}