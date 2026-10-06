// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Cấu hình các tham số cho gửi email

using System.ComponentModel.DataAnnotations;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services.Email;

/// <summary>
/// Cấu hình SMTP (mục "Email" trong appsettings). Mật khẩu để trong user-secrets / biến môi trường
/// (Email__Password), KHÔNG commit vào appsettings.json.
/// </summary>
public class EmailOptions : IValidatableObject
{
    public const string SectionName = "Email";

    /// <summary>Để trống = chưa cấu hình SMTP.</summary>
    public string Host { get; set; } = string.Empty;

    [Range(1, 65535)]
    public int Port { get; set; } = 587;

    /// <summary>true: STARTTLS (cổng 587). false: tự chọn theo cổng (465 = SSL ngay khi kết nối).</summary>
    public bool UseStartTls { get; set; } = true;

    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "Thư viện";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Host);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (IsConfigured && string.IsNullOrWhiteSpace(FromAddress))
            yield return new ValidationResult("Email:FromAddress là bắt buộc khi đã cấu hình Email:Host.",
                new[] { nameof(FromAddress) });
    }
}
