// Họ và tên: Vũ Tiến Thành
// Mã sinh viên: 23103100257
// Nội dung thực hiện: Xây dựng controller cho việc quản lý thể loại sách (Admin)
using Microsoft.AspNetCore.Mvc;
using QuanLyThuVien_UNETI4_TI17A1CL.Models;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Auth;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Common;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;
using QuanLyThuVien_UNETI4_TI17A1CL.ViewModels;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Controllers;

public class TheLoaiController : Controller
{
    private readonly ITheLoaiService _service;

    public TheLoaiController(ITheLoaiService service)
    {
        _service = service;
    }

    [HttpGet]
    [AuthorizeRole(VaiTro.Admin)]
    public async Task<IActionResult> Index(
        string? search,
        TrangThaiTheLoai? trangThai,
        int page = 1,
        int pageSize = PagingDefaults.DefaultPageSize)
    {
        if (trangThai.HasValue && !Enum.IsDefined(trangThai.Value))
            trangThai = null;
        search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

        var result = await _service.GetListAsync(
            new TheLoaiFilter(search, trangThai, page, pageSize), HttpContext.RequestAborted);
        var routeValues = new Dictionary<string, string>();
        if (search is not null) routeValues["search"] = search;
        if (trangThai.HasValue) routeValues["trangThai"] = trangThai.Value.ToString();
        ViewBag.Pager = PagerViewModel.From(result, "TheLoai", nameof(Index), routeValues);

        // Lọc/tìm kiếm/phân trang qua AJAX: chỉ trả về partial bảng + phân trang.
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return PartialView("_BangTheLoai", result);

        ViewBag.Search = search;
        ViewBag.TrangThai = trangThai;
        return View(result);
    }

    [HttpGet]
    [AuthorizeRole(VaiTro.Admin, VaiTro.ThuThu, VaiTro.BanDoc)]
    public async Task<IActionResult> Details(int id)
    {
        var detail = await _service.GetAsync(id, HttpContext.RequestAborted);
        return detail is null ? NotFound() : View(detail);
    }

    [HttpGet]
    [AuthorizeRole(VaiTro.Admin)]
    public IActionResult Create() => View(new TheLoai());

    [HttpPost]
    [AuthorizeRole(VaiTro.Admin)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TheLoai model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _service.CreateAsync(new CreateTheLoaiInput(
            model.TenTheLoai, model.MoTa, model.KhuVucLuuTru, model.TrangThai), HttpContext.RequestAborted);
        if (!result.Succeeded)
        {
            result.AddTo(ModelState);
            return View(model);
        }

        TempData["ThongBao"] = "Đã thêm thể loại.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [AuthorizeRole(VaiTro.Admin)]
    public async Task<IActionResult> Edit(int id)
    {
        var detail = await _service.GetAsync(id, HttpContext.RequestAborted);
        if (detail is null)
            return NotFound();

        return View(ToModel(detail));
    }

    [HttpPost]
    [AuthorizeRole(VaiTro.Admin)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TheLoai model)
    {
        if (await _service.GetAsync(id, HttpContext.RequestAborted) is null)
            return NotFound();
        model.MaTheLoai = id;

        if (!ModelState.IsValid)
            return View(model);

        var result = await _service.UpdateAsync(new UpdateTheLoaiInput(
            id, model.TenTheLoai, model.MoTa, model.KhuVucLuuTru, model.TrangThai),
            HttpContext.RequestAborted);
        if (!result.Succeeded)
        {
            result.AddTo(ModelState);
            return View(model);
        }

        TempData["ThongBao"] = "Đã cập nhật thể loại.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [AuthorizeRole(VaiTro.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        var detail = await _service.GetAsync(id, HttpContext.RequestAborted);
        return detail is null ? NotFound() : View(detail);
    }

    [HttpPost, ActionName("Delete")]
    [AuthorizeRole(VaiTro.Admin)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result = await _service.DeleteAsync(id, HttpContext.RequestAborted);
        if (result.Succeeded)
        {
            TempData["ThongBao"] = "Đã xóa thể loại.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Loi"] = result.FirstError;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [AuthorizeRole(VaiTro.Admin)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleTrangThai(int id, string? returnUrl)
    {
        var detail = await _service.GetAsync(id, HttpContext.RequestAborted);
        if (detail is null)
            return NotFound();

        var trangThaiMoi = detail.TrangThai == TrangThaiTheLoai.HoatDong
            ? TrangThaiTheLoai.NgungHoatDong
            : TrangThaiTheLoai.HoatDong;
        var result = await _service.SetTrangThaiAsync(id, trangThaiMoi, HttpContext.RequestAborted);
        if (result.Succeeded)
            TempData["ThongBao"] = trangThaiMoi == TrangThaiTheLoai.HoatDong
                ? "Đã kích hoạt lại thể loại."
                : "Đã ngưng hoạt động thể loại.";
        else
            TempData["Loi"] = result.FirstError;

        return LocalRedirectOrIndex(returnUrl);
    }

    private IActionResult LocalRedirectOrIndex(string? returnUrl) =>
        Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction(nameof(Index));

    private static TheLoai ToModel(TheLoaiDetail detail) => new()
    {
        MaTheLoai = detail.MaTheLoai,
        TenTheLoai = detail.TenTheLoai,
        MoTa = detail.MoTa,
        KhuVucLuuTru = detail.KhuVucLuuTru,
        TrangThai = detail.TrangThai
    };
}
