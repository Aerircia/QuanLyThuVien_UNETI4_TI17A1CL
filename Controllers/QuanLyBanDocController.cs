// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Controller quản lý bạn đọc (Admin và Thủ thư). Tách từ BanDocController phía bạn đọc.

using Microsoft.AspNetCore.Mvc;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Auth;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Common;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;
using QuanLyThuVien_UNETI4_TI17A1CL.ViewModels;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Controllers;

public class QuanLyBanDocController : Controller
{
    private readonly IBanDocService _banDocService;

    public QuanLyBanDocController(IBanDocService banDocService)
    {
        _banDocService = banDocService;
    }

    private ViewResult BanDocView(string viewName, object? model = null) => View($"~/Views/BanDoc/{viewName}.cshtml", model);

    // Quản lý bạn đọc dành cho Thủ thư và Admin.
    [AuthorizeRole(VaiTro.Admin, VaiTro.ThuThu)]
    [HttpGet("/ThuThu/QuanLyBanDoc")]
    public async Task<IActionResult> Index(
        string? search,
        LoaiBanDoc? loaiBanDoc,
        TrangThaiBanDoc? trangThai,
        int page = 1,
        int pageSize = PagingDefaults.DefaultPageSize)
    {
        if (loaiBanDoc.HasValue && !Enum.IsDefined(loaiBanDoc.Value)) loaiBanDoc = null;
        if (trangThai.HasValue && !Enum.IsDefined(trangThai.Value)) trangThai = null;
        search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

        var result = await _banDocService.GetListAsync(
            new BanDocFilter(search, loaiBanDoc, trangThai, page, pageSize), HttpContext.RequestAborted);

        var routeValues = new Dictionary<string, string>();
        if (search is not null) routeValues["search"] = search;
        if (loaiBanDoc.HasValue) routeValues["loaiBanDoc"] = loaiBanDoc.Value.ToString();
        if (trangThai.HasValue) routeValues["trangThai"] = trangThai.Value.ToString();

        var counts = await _banDocService.GetStatusCountsAsync(HttpContext.RequestAborted);

        return BanDocView(nameof(Index), new BanDocListViewModel
        {
            Search = search,
            LoaiBanDoc = loaiBanDoc,
            TrangThai = trangThai,
            PageSize = result.PageSize,
            Result = result,
            Pager = PagerViewModel.From(result, "QuanLyBanDoc", nameof(Index), routeValues),
            TongSoBanDoc = counts.TongSo,
            SoBanDocChoDuyet = counts.ChoDuyet,
            SoBanDocHoatDong = counts.HoatDong,
            SoBanDocNgungHoatDong = counts.NgungHoatDong,
            SoBanDocTuChoi = counts.TuChoi
        });
    }

    [AuthorizeRole(VaiTro.Admin, VaiTro.ThuThu)]
    [HttpGet("/ThuThu/DuyetTheBanDoc")]
    public async Task<IActionResult> DuyetThe(
        string? search,
        int page = 1,
        int pageSize = PagingDefaults.DefaultPageSize)
    {
        search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

        var result = await _banDocService.GetListAsync(
            new BanDocFilter(search, null, TrangThaiBanDoc.ChoDuyet, page, pageSize), HttpContext.RequestAborted);

        var routeValues = new Dictionary<string, string>();
        if (search is not null) routeValues["search"] = search;

        var counts = await _banDocService.GetStatusCountsAsync(HttpContext.RequestAborted);

        var model = new BanDocListViewModel
        {
            Search = search,
            LoaiBanDoc = null,
            TrangThai = TrangThaiBanDoc.ChoDuyet,
            PageSize = result.PageSize,
            Result = result,
            Pager = PagerViewModel.From(result, "QuanLyBanDoc", nameof(DuyetThe), routeValues),
            TongSoBanDoc = counts.TongSo,
            SoBanDocChoDuyet = counts.ChoDuyet,
            SoBanDocHoatDong = counts.HoatDong,
            SoBanDocNgungHoatDong = counts.NgungHoatDong,
            SoBanDocTuChoi = counts.TuChoi
        };

        var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Query.ContainsKey("ajax");
        if (isAjax)
            return PartialView("~/Views/BanDoc/_DuyetTheContentPartial.cshtml", model);

        return BanDocView(nameof(DuyetThe), model);
    }

    [AuthorizeRole(VaiTro.Admin, VaiTro.ThuThu)]
    [HttpPost("/ThuThu/QuanLyBanDoc/Duyet/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Duyet(int id, string? returnUrl)
    {
        var result = await _banDocService.DuyetTheAsync(id, HttpContext.RequestAborted);
        var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        if (result.Succeeded)
        {
            var msg = "Đã phê duyệt và kích hoạt thẻ bạn đọc thành công.";
            if (isAjax) return Json(new { success = true, message = msg });
            TempData["ThongBao"] = msg;
        }
        else
        {
            var msg = result.FirstError;
            if (isAjax) return Json(new { success = false, message = msg });
            TempData["Loi"] = msg;
        }

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return RedirectToAction(nameof(DuyetThe));
    }

    [AuthorizeRole(VaiTro.Admin, VaiTro.ThuThu)]
    [HttpPost("/ThuThu/QuanLyBanDoc/TuChoi/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TuChoi(int id, string? returnUrl)
    {
        var result = await _banDocService.TuChoiTheAsync(id, null, HttpContext.RequestAborted);
        var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        if (result.Succeeded)
        {
            var msg = "Đã từ chối yêu cầu đăng ký thẻ bạn đọc.";
            if (isAjax) return Json(new { success = true, message = msg });
            TempData["ThongBao"] = msg;
        }
        else
        {
            var msg = result.FirstError;
            if (isAjax) return Json(new { success = false, message = msg });
            TempData["Loi"] = msg;
        }

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return RedirectToAction(nameof(DuyetThe));
    }

    [AuthorizeRole(VaiTro.Admin, VaiTro.ThuThu)]
    [HttpGet("/ThuThu/QuanLyBanDoc/Create")]
    public async Task<IActionResult> Create()
    {
        ViewBag.UnlinkedAccounts = await _banDocService.GetUnlinkedAccountsAsync(null, HttpContext.RequestAborted);
        return BanDocView(nameof(Create), new BanDocCreateViewModel());
    }

    [AuthorizeRole(VaiTro.Admin, VaiTro.ThuThu)]
    [HttpPost("/ThuThu/QuanLyBanDoc/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BanDocCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.UnlinkedAccounts = await _banDocService.GetUnlinkedAccountsAsync(null, HttpContext.RequestAborted);
            return BanDocView(nameof(Create), model);
        }

        var result = await _banDocService.CreateAsync(
            new CreateBanDocInput(
                model.MaTaiKhoan,
                model.HoTen,
                model.NgaySinh,
                model.GioiTinh,
                model.SoDienThoai,
                model.Email,
                model.DiaChi,
                model.MaSoThe,
                model.LoaiBanDoc,
                model.NgayCapThe,
                model.NgayHetHanThe,
                model.TrangThai),
            HttpContext.RequestAborted);

        if (!result.Succeeded)
        {
            result.AddTo(ModelState);
            ViewBag.UnlinkedAccounts = await _banDocService.GetUnlinkedAccountsAsync(null, HttpContext.RequestAborted);
            return BanDocView(nameof(Create), model);
        }

        TempData["ThongBao"] = "Thêm hồ sơ bạn đọc thành công.";
        return RedirectToAction(nameof(Index));
    }

    [AuthorizeRole(VaiTro.Admin, VaiTro.ThuThu)]
    [HttpGet("/ThuThu/QuanLyBanDoc/Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var detail = await _banDocService.GetAsync(id, HttpContext.RequestAborted);
        if (detail is null) return NotFound();

        return BanDocView(nameof(Edit), new BanDocEditViewModel
        {
            MaBanDoc = detail.MaBanDoc,
            MaTaiKhoan = detail.MaTaiKhoan,
            TenDangNhap = detail.TenDangNhap,
            HoTen = detail.HoTen,
            NgaySinh = detail.NgaySinh,
            GioiTinh = detail.GioiTinh,
            SoDienThoai = detail.SoDienThoai ?? string.Empty,
            Email = detail.Email ?? string.Empty,
            DiaChi = detail.DiaChi ?? string.Empty,
            MaSoThe = detail.MaSoThe,
            LoaiBanDoc = detail.LoaiBanDoc,
            NgayCapThe = detail.NgayCapThe,
            NgayHetHanThe = detail.NgayHetHanThe,
            TrangThai = detail.TrangThai
        });
    }

    [AuthorizeRole(VaiTro.Admin, VaiTro.ThuThu)]
    [HttpPost("/ThuThu/QuanLyBanDoc/Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(BanDocEditViewModel model)
    {
        var detail = await _banDocService.GetAsync(model.MaBanDoc, HttpContext.RequestAborted);
        if (detail is null) return NotFound();
        model.TenDangNhap = detail.TenDangNhap;

        if (!ModelState.IsValid)
            return BanDocView(nameof(Edit), model);

        var result = await _banDocService.UpdateAsync(
            new UpdateBanDocInput(
                model.MaBanDoc,
                model.HoTen,
                model.NgaySinh,
                model.GioiTinh,
                model.SoDienThoai,
                model.Email,
                model.DiaChi,
                model.MaSoThe,
                model.LoaiBanDoc,
                model.NgayCapThe,
                model.NgayHetHanThe,
                model.TrangThai),
            HttpContext.RequestAborted);

        if (!result.Succeeded)
        {
            result.AddTo(ModelState);
            return BanDocView(nameof(Edit), model);
        }

        TempData["ThongBao"] = "Cập nhật hồ sơ bạn đọc thành công.";
        return RedirectToAction(nameof(Index));
    }

    [AuthorizeRole(VaiTro.Admin, VaiTro.ThuThu)]
    [HttpGet("/ThuThu/QuanLyBanDoc/Details/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var detail = await _banDocService.GetAsync(id, HttpContext.RequestAborted);
        if (detail is null)
        {
            // Không có id hoặc bạn đọc không tồn tại: về danh sách thay vì 404 thô (trang chỉ dành cho Thủ thư/Admin)
            TempData["Loi"] = "Không tìm thấy bạn đọc hoặc bạn đọc đã bị xóa.";
            return RedirectToAction(nameof(Index));
        }

        return BanDocView(nameof(Details), detail);
    }

    [AuthorizeRole(VaiTro.Admin, VaiTro.ThuThu)]
    [HttpPost("/ThuThu/QuanLyBanDoc/SetStatus/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStatus(int id, TrangThaiBanDoc trangThai, string? returnUrl)
    {
        var result = await _banDocService.SetStatusAsync(id, trangThai, HttpContext.RequestAborted);
        if (result.Succeeded)
            TempData["ThongBao"] = trangThai == TrangThaiBanDoc.HoatDong
                ? "Đã kích hoạt hoạt động cho bạn đọc."
                : "Đã tạm dừng hoạt động của bạn đọc.";
        else
            TempData["Loi"] = result.FirstError;

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return RedirectToAction(nameof(Index));
    }
}
