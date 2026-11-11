// Họ và tên: Vũ Ngọc Hương Giang
// Mã sinh viên: 23103100280
// Nội dung thực hiện: Controller gia hạn ; Thủ thư xem danh sách và duyệt/từ chối.

using Microsoft.AspNetCore.Mvc;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Auth;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Common;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;
using QuanLyThuVien_UNETI4_TI17A1CL.ViewModels;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Controllers;

public class GiaHanMuonController : Controller
{
    private readonly IGiaHanMuonService _service;
    private readonly ICurrentUserService _current;

    public GiaHanMuonController(IGiaHanMuonService service, ICurrentUserService current)
    {
        _service = service;
        _current = current;
    }
    private bool IsAjax => Request.Headers["X-Requested-With"] == "XMLHttpRequest";
    private IActionResult AjaxLoi(string? macDinh = null)
    {
        var loi = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
            .Where(m => !string.IsNullOrWhiteSpace(m)).ToList();
        var nhanXet = ModelState.TryGetValue(nameof(GiaHanMuonProcessViewModel.NhanXet), out var entry)
            ? entry.Errors.Select(e => e.ErrorMessage).ToArray()
            : Array.Empty<string>();

        return Json(new
        {
            success = false,
            message = loi.Count > 0 ? string.Join(" ", loi) : macDinh ?? "Không thể xử lý yêu cầu gia hạn.",
            errors = new { NhanXet = nhanXet }
        });
    }

// Bạn đọc gửi yêu cầu gia hạn 
    [HttpGet("/BanDoc/XinGiaHan/{maPhieuMuon:int}")]
    [AuthorizeRole(VaiTro.BanDoc)]
    [RequireActiveCard]
    public async Task<IActionResult> Create(int maPhieuMuon)
    {
        var maBanDoc = await _current.GetMaBanDocAsync(HttpContext.RequestAborted);
        if (maBanDoc is null)
            return RedirectToAction("AccessDenied", "TaiKhoan");
        var info = await _service.GetCreateInfoAsync(maPhieuMuon, maBanDoc.Value, HttpContext.RequestAborted);
        if (info is null)
            return NotFound();

        return View(new GiaHanMuonCreateViewModel
        {
            Info = info,
            HanTraMoi = info.HanTraMoiToiDa  
        });
    }
    [HttpPost("/BanDoc/XinGiaHan/{maPhieuMuon:int}")]
    [AuthorizeRole(VaiTro.BanDoc)]
    [RequireActiveCard]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int maPhieuMuon, GiaHanMuonCreateViewModel model)
    {
        var maBanDoc = await _current.GetMaBanDocAsync(HttpContext.RequestAborted);
        if (maBanDoc is null)
            return RedirectToAction("AccessDenied", "TaiKhoan");

        var info = await _service.GetCreateInfoAsync(maPhieuMuon, maBanDoc.Value, HttpContext.RequestAborted);
        if (info is null)
            return NotFound();
        model.Info = info;

        if (!ModelState.IsValid)
            return View(model);

        var result = await _service.CreateAsync(
            new CreateGiaHanInput(maPhieuMuon, model.HanTraMoi!.Value, model.LyDo),
            maBanDoc.Value,
            HttpContext.RequestAborted);

        if (!result.Succeeded)
        {
            result.AddTo(ModelState);
            return View(model);
        }

        TempData["ThongBao"] = "Đã gửi yêu cầu gia hạn, vui lòng chờ Thủ thư duyệt.";
        return RedirectToAction(nameof(Create), new { maPhieuMuon });
    }

//Thủ thư: danh sách yêu cầu
    [HttpGet("/ThuThu/YeuCauGiaHan")]
    [AuthorizeRole(VaiTro.Admin, VaiTro.ThuThu)]
    public async Task<IActionResult> Index(
        string? search,
        TrangThaiGiaHan? trangThai,
        int page = 1,
        int pageSize = PagingDefaults.DefaultPageSize)
    {
        if (trangThai.HasValue && !Enum.IsDefined(trangThai.Value)) trangThai = null;
        search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

        var result = await _service.GetListAsync(
            new GiaHanMuonFilter(search, trangThai, page, pageSize), HttpContext.RequestAborted);

        var routeValues = new Dictionary<string, string>();
        if (search is not null) routeValues["search"] = search;
        if (trangThai.HasValue) routeValues["trangThai"] = trangThai.Value.ToString();

        var vm = new GiaHanMuonListViewModel
        {
            Search = search,
            TrangThai = trangThai,
            PageSize = result.PageSize,
            Result = result,
            Pager = PagerViewModel.From(result, "GiaHanMuon", nameof(Index), routeValues)
        };

        // AJAX tìm/lọc/phân trang
        if (IsAjax)
            return PartialView("_BangGiaHan", vm);

        return View(vm);
    }

// Thủ thư: duyệt
    [HttpGet("/ThuThu/YeuCauGiaHan/Process/{id:int}")]
    [AuthorizeRole(VaiTro.Admin, VaiTro.ThuThu)]
    public async Task<IActionResult> Process(int id)
    {
        var detail = await _service.GetAsync(id, HttpContext.RequestAborted);
        if (detail is null)
            return NotFound();

        var vm = new GiaHanMuonProcessViewModel { Detail = detail };
        if (IsAjax)
            return PartialView("_ChiTietXuLy", vm);

        return View(vm);
    }
    [HttpPost("/ThuThu/YeuCauGiaHan/Process/{id:int}")]
    [AuthorizeRole(VaiTro.Admin, VaiTro.ThuThu)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Process(int id, GiaHanMuonProcessViewModel model, bool duyet)
    {
        var detail = await _service.GetAsync(id, HttpContext.RequestAborted);
        if (detail is null)
            return NotFound();
        model.Detail = detail;

        if (!ModelState.IsValid)
            return IsAjax ? AjaxLoi("Dữ liệu không hợp lệ.") : View(model);

        var result = await _service.ProcessAsync(new ProcessGiaHanInput(id, duyet, model.NhanXet), HttpContext.RequestAborted);
        if (!result.Succeeded)
        {
            result.AddTo(ModelState);
            return IsAjax ? AjaxLoi() : View(model);
        }

        var thongBao = duyet ? "Đã duyệt yêu cầu gia hạn." : "Đã từ chối yêu cầu gia hạn.";
        if (IsAjax)
            return Json(new { success = true, message = thongBao ,redirectUrl = Url.Action(nameof(Index)) //để JS biết nên chuyển tới đích đến nào sau khi xác nhận
            });

        TempData["ThongBao"] = thongBao;
        return RedirectToAction(nameof(Index));
    }
}