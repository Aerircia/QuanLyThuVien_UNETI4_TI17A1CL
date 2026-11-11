// Họ và tên: Vũ Ngọc Hương Giang
// Mã sinh viên: 23103100280
// Nội dung thực hiện: Controller nhận yêu cầu nhắc trả sách, gia hạn,nộp phạt của thủ thư/admin.

using Microsoft.AspNetCore.Mvc;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Auth;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Common;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Controllers;

/// <summary>Chỉ Admin và Thủ thư; bạn đọc không gọi được kể cả khi biết đường dẫn.</summary>
[AuthorizeRole(VaiTro.Admin, VaiTro.ThuThu)]
public class NhacNhoController : Controller
{
    private readonly INhacNhoService _service;

    public NhacNhoController(INhacNhoService service)
    {
        _service = service;
    }

    [HttpPost("NhacNho/TraSach/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NhacTraSach(int id) =>
        Tra(await _service.NhacTraSachAsync(id, HttpContext.RequestAborted));

    [HttpPost("NhacNho/GiaHan/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NhacGiaHan(int id) =>
        Tra(await _service.NhacGiaHanAsync(id, HttpContext.RequestAborted));

    [HttpPost("NhacNho/NopPhat/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NhacNopPhat(int id) =>
        Tra(await _service.NhacNopPhatAsync(id, HttpContext.RequestAborted));

    private IActionResult Tra(ServiceResult<string> result)
    {
        var message = result.Succeeded ? result.Value! : result.FirstError;

        // nhac-nho.js luôn gửi header này; trả JSON { success, message } cho nó hiển thị
        if (Request.Headers["X-Requested-With"].ToString() == "XMLHttpRequest")
            return Json(new { success = result.Succeeded, message });

        // Dự phòng khi JS tắt: quay về trang chờ nhận trả kèm thông báo
        TempData[result.Succeeded ? "ThongBao" : "Loi"] = message;
        return RedirectToAction("ChoTra", "TraSach");
    }
}