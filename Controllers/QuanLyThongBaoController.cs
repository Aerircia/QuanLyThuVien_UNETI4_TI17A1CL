// Họ và tên: Vũ Ngọc Hương Giang
// Mã sinh viên: 23103100280
// Nội dung thực hiện: Controller quản lý thông báo cho Admin/Thủ thư: xem mọi thông báo, tìm/lọc (AJAX), ẩn/hiện, gửi thông báo chung.
using Microsoft.AspNetCore.Mvc;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Auth;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Common;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Notifications;
using QuanLyThuVien_UNETI4_TI17A1CL.ViewModels;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Controllers;

[AuthorizeRole(VaiTro.Admin, VaiTro.ThuThu)]
public class QuanLyThongBaoController : Controller
{
    private readonly IQuanLyThongBaoService _service;

    public QuanLyThongBaoController(IQuanLyThongBaoService service)
    {
        _service = service;
    }

    private bool IsAjax => Request.Headers["X-Requested-With"] == "XMLHttpRequest";
    [HttpGet]
    public async Task<IActionResult> Index(string? search, string? loai, string? nhomNhan, bool? hienThi,
        int page = 1, int pageSize = PagingDefaults.DefaultPageSize)
    {
        search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        if (!string.IsNullOrEmpty(loai) && !ThongBaoHienThi.CacLoai.Contains(loai)) loai = null;
        if (nhomNhan is not ("BanDoc" or "NhanVien")) nhomNhan = null;

        var result = await _service.GetQuanLyAsync(
            new QuanLyThongBaoFilter(search, loai, nhomNhan, hienThi, page, pageSize), HttpContext.RequestAborted);

        var routeValues = new Dictionary<string, string>();
        if (search is not null) routeValues["search"] = search;
        if (!string.IsNullOrEmpty(loai)) routeValues["loai"] = loai;
        if (nhomNhan is not null) routeValues["nhomNhan"] = nhomNhan;
        if (hienThi.HasValue) routeValues["hienThi"] = hienThi.Value.ToString().ToLowerInvariant();

        var vm = new QuanLyThongBaoViewModel
        {
            Search = search,
            Loai = loai,
            NhomNhan = nhomNhan,
            DangHienThi = hienThi,
            PageSize = result.PageSize,
            Result = result,
            Pager = PagerViewModel.From(result, "QuanLyThongBao", nameof(Index), routeValues)
        };
        if (IsAjax) return PartialView("_BangQuanLy", vm);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DoiHienThi(int id, bool hienThi, string? returnUrl)
    {
        var result = await _service.DoiHienThiAsync(id, hienThi, HttpContext.RequestAborted);
        if (result.Succeeded)
            TempData["ThongBao"] = hienThi ? "Đã hiện lại thông báo." : "Đã ẩn thông báo.";
        else
            TempData["Loi"] = result.FirstError;
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);
        return RedirectToAction(nameof(Index));
    }
    [HttpGet]
    public IActionResult Gui() => View(new GuiThongBaoViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Gui(GuiThongBaoViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _service.GuiThongBaoChungAsync(
            new GuiThongBaoInput(model.TieuDe, model.NoiDung, model.MucDo, model.NhomNhan), HttpContext.RequestAborted);
        if (!result.Succeeded)
        {
            result.AddTo(ModelState);
            return View(model);
        }

        TempData["ThongBao"] = $"Đã gửi thông báo tới {result.Value} người nhận.";
        return RedirectToAction(nameof(Index));
    }
}