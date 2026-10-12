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

// Bạn đọc gửi yêu cầu gia hạn 
    [HttpGet("GiaHanMuon/Create/{maPhieuMuon:int}")]
    [AuthorizeRole(VaiTro.BanDoc)]
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

    [HttpPost("GiaHanMuon/Create/{maPhieuMuon:int}")]
    [AuthorizeRole(VaiTro.BanDoc)]
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
    [HttpGet]
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

        return View(new GiaHanMuonListViewModel
        {
            Search = search,
            TrangThai = trangThai,
            PageSize = result.PageSize,
            Result = result,
            Pager = PagerViewModel.From(result, "GiaHanMuon", nameof(Index), routeValues)
        });
    }

// Thủ thư: duyệt
    [HttpGet("GiaHanMuon/Process/{id:int}")]
    [AuthorizeRole(VaiTro.Admin, VaiTro.ThuThu)]
    public async Task<IActionResult> Process(int id)
    {
        var detail = await _service.GetAsync(id, HttpContext.RequestAborted);
        if (detail is null)
            return NotFound();

        return View(new GiaHanMuonProcessViewModel { Detail = detail });
    }

    [HttpPost("GiaHanMuon/Process/{id:int}")]
    [AuthorizeRole(VaiTro.Admin, VaiTro.ThuThu)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Process(int id, GiaHanMuonProcessViewModel model, bool duyet)
    {
        var detail = await _service.GetAsync(id, HttpContext.RequestAborted);
        if (detail is null)
            return NotFound();
        model.Detail = detail;

        if (!ModelState.IsValid)
            return View(model);

        var result = await _service.ProcessAsync(new ProcessGiaHanInput(id, duyet, model.NhanXet), HttpContext.RequestAborted);
        if (!result.Succeeded)
        {
            result.AddTo(ModelState);
            return View(model);
        }

        TempData["ThongBao"] = duyet ? "Đã duyệt yêu cầu gia hạn." : "Đã từ chối yêu cầu gia hạn.";
        return RedirectToAction(nameof(Index));
    }
}