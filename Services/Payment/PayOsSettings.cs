// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Xây dựng PayOsSettings, đọc cấu hình section "PayOS" từ appsettings.json / user-secrets / biến môi trường, lưu trữ các thông tin cần thiết để tạo liên kết thanh toán payOS.
namespace QuanLyThuVien_UNETI4_TI17A1CL.Services.Payment;

/// <summary>Section "PayOS" trong cấu hình. Khóa để trong user-secrets / biến môi trường PayOS__*, không commit.</summary>
public sealed class PayOsSettings
{
    public const string SectionName = "PayOS";

    public string? ClientId { get; set; }
    public string? ApiKey { get; set; }
    public string? ChecksumKey { get; set; }

    /// <summary>Địa chỉ gốc công khai của ứng dụng, vd https://localhost:7123 (cổng được chuyển tiếp nếu dùng webhook).</summary>
    public string? PublicBaseUrl { get; set; }

    public int ExpireMinutes { get; set; } = 10;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ApiKey) &&
        !string.IsNullOrWhiteSpace(ChecksumKey) && !string.IsNullOrWhiteSpace(PublicBaseUrl);
}
