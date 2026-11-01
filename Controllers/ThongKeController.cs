// Họ và tên: Phùng Ngọc Bảo
// Mã sinh viên: 23103100278
// Nội dung thực hiện: Controller thống kê

using Microsoft.AspNetCore.Mvc;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Auth;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;
using QuanLyThuVien_UNETI4_TI17A1CL.ViewModels;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Controllers;

[AuthorizeRole(VaiTro.Admin, VaiTro.ThuThu)]
public sealed class ThongKeController : Controller
{
    private readonly IThongKeService _service;

    public ThongKeController(IThongKeService service)
    {
        _service = service;
    }

    // ---------- 9 chỉ số Dashboard ----------

    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var ct = HttpContext.RequestAborted;

        var summary = await _service.GetDashboardAsync(ct);
        var report = await _service.GetReportAsync(ct);

        var model = new ThongKeDashboardViewModel(
            summary,
            report
        );

        return View(model);
    }

    // ---------- 11 nhóm thống kê LINQ ----------

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var model = await _service.GetReportAsync(HttpContext.RequestAborted);
        return View(model);
    }
}
