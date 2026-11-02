// Họ và tên: Vũ Tiến Thành
// Mã sinh viên: 23103100257
// Nội dung thực hiện: Xây dựng service tải lên ảnh bìa sách (giới hạn loại file và dung lượng như mẫu thiết kế)
using Microsoft.AspNetCore.Http;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Common;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services;

public sealed class AnhBiaService : IAnhBiaService
{
    private const long DungLuongToiDa = 2 * 1024 * 1024; // 2MB, khớp mẫu thiết kế library_demo
    private static readonly string[] DinhDangChoPhep = { ".jpg", ".jpeg", ".png", ".webp" };

    // Đường dẫn tuyệt đối trên đĩa tính từ wwwroot; kết quả trả về cho view là đường dẫn tương đối.
    private readonly string thuMucBia;

    public AnhBiaService(IWebHostEnvironment env)
    {
        thuMucBia = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "uploads", "bia");
    }

    public async Task<ServiceResult<string>> SaveAsync(IFormFile file, CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
            return ServiceResult<string>.Error(string.Empty, "Vui lòng chọn tệp ảnh.");

        if (file.Length > DungLuongToiDa)
            return ServiceResult<string>.Error(string.Empty, "Ảnh không được vượt quá 2MB.");

        var duoi = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!DinhDangChoPhep.Contains(duoi))
            return ServiceResult<string>.Error(string.Empty, "Vui lòng chọn tệp ảnh (JPG, PNG hoặc WEBP).");

        Directory.CreateDirectory(thuMucBia);
        var tenFile = $"{Guid.NewGuid():N}{duoi}";
        await using var stream = File.Create(Path.Combine(thuMucBia, tenFile));
        await file.CopyToAsync(stream, ct);

        return ServiceResult<string>.Ok($"/uploads/bia/{tenFile}");
    }

    public void Delete(string? imgPath)
    {
        if (string.IsNullOrWhiteSpace(imgPath))
            return;

        // Chỉ xóa file nằm trong thư mục bìa do mình quản lý.
        var duongDan = imgPath.Replace('\\', '/');
        if (!duongDan.StartsWith("/uploads/bia/", StringComparison.OrdinalIgnoreCase))
            return;

        try
        {
            var fullPath = Path.Combine(thuMucBia, Path.GetFileName(duongDan));
            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }
        catch (IOException)
        {
            // Xóa ảnh cũ là thao tác phụ; bỏ qua nếu file đang bị khóa.
        }
    }
}
