// Họ và tên: Vũ Tiến Thành
// Mã sinh viên: 23103100257
// Nội dung thực hiện: Xây dựng interface cho việc tải lên ảnh bìa sách
using Microsoft.AspNetCore.Http;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Common;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;

public interface IAnhBiaService
{
    /// <summary>Lưu ảnh bìa mới, trả về đường dẫn tương đối (vd. /uploads/bia/xxx.jpg).</summary>
    Task<ServiceResult<string>> SaveAsync(IFormFile file, CancellationToken ct = default);

    /// <summary>Xóa file ảnh bìa theo đường dẫn tương đối (best-effort, dùng khi thay/xa ảnh).</summary>
    void Delete(string? imgPath);
}
