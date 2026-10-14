// Họ và tên: Vũ Tiến Thành
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Tạo action giữ chỗ đăng ký mượn, chuyển khách đến đăng nhập và trả thông báo placeholder cho bạn đọc.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Controllers;

public sealed class PhieuMuonController : Controller
{
    [AllowAnonymous]
    [HttpPost("/PhieuMuon/Register/{id:int}")]
    [ValidateAntiForgeryToken]
    public IActionResult Register(int id)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            var returnUrl = Url.Action("Details", "Sach", new { id });
            return RedirectToAction("Login", "TaiKhoan", new { returnUrl });
        }

        if (!User.IsInRole(nameof(VaiTro.BanDoc)))
            return Forbid();

        TempData["ThongBao"] = "Chức năng đăng ký mượn sẽ được bổ sung trong mô-đun Phiếu mượn.";
        return RedirectToAction("Details", "Sach", new { id });
    }
}
