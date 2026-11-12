// Họ và tên: Nguyễn Hữu Tuấn
// Mã sinh viên: 23103100136
// Nội dung thực hiện: HomeController hiển thị trang chủ và danh mục tra cứu sách mượn

// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Làm lại trang chủ (landing), thêm trang tra cứu kho sách và chi tiết sách công khai cho khách/bạn đọc

using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using QuanLyThuVien_UNETI4_TI17A1CL.Models;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Common;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;
using QuanLyThuVien_UNETI4_TI17A1CL.ViewModels;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Controllers;

public class HomeController : Controller
{
    private readonly IHomeService _home;
    private readonly ICurrentUserService _current;

    public HomeController(IHomeService home, ICurrentUserService current)
    {
        _home = home;
        _current = current;
    }

    // Trang chủ công khai. Nhân viên có giao diện riêng nên đưa thẳng về Dashboard.
    [HttpGet("/TrangChu")]
    public async Task<IActionResult> Index()
    {
        if (_current.IsInRole(VaiTro.Admin, VaiTro.ThuThu))
            return RedirectToAction("Dashboard", "ThongKe");

        return View(await _home.GetLandingAsync(HttpContext.RequestAborted));
    }

    // Tra cứu kho sách: khách và mọi vai trò đều xem được (chỉ đăng ký mượn mới cần đăng nhập).
    [HttpGet("/TrangChu/KhoSach")]
    public async Task<IActionResult> Catalog(
        [FromQuery] CatalogFilter filter,
        int page = 1,
        int pageSize = PagingDefaults.DefaultPageSize)
    {
        filter.Normalize();

        // Yêu cầu AJAX (X-Requested-With) chỉ nhận phần kết quả; tải trang bình thường nhận cả trang.
        var ajax = Request.Headers.XRequestedWith == "XMLHttpRequest";

        // Cùng một URL có hai dạng trả về: Vary giúp nút Back không bao giờ hiện nhầm mảnh HTML trần.
        Response.Headers.Append("Vary", "X-Requested-With");

        var vm = await _home.GetCatalogAsync(filter, page, pageSize, includeSidebar: !ajax, HttpContext.RequestAborted);
        return ajax ? PartialView("_CatalogResults", vm) : View(vm);
    }
    [HttpGet("/TrangChu/ChiTietSach/{id:int}")]
    public async Task<IActionResult> Book(int id)
    {
        var detail = await _home.GetBookAsync(id, HttpContext.RequestAborted);
        return detail is null ? NotFound() : View(detail);
    }

    [HttpGet("/TrangChu/QuyenRiengTu")]
    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [HttpGet("/Loi")]
    public IActionResult Error(int? statusCode = null)
    {
        var code = statusCode is >= 400 and <= 599 ? statusCode.Value : 500;
        Response.StatusCode = code;
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            StatusCode = code
        });
    }
}
