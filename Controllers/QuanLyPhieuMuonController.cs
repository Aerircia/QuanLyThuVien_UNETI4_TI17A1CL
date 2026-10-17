// Họ và tên: Phùng Ngọc Bảo
// Mã sinh viên: 23103100278
// Nội dung thực hiện: Controller quản lý phiếu mượn

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Auth;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Common;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;
using QuanLyThuVien_UNETI4_TI17A1CL.ViewModels;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Controllers;

[AuthorizeRole(VaiTro.Admin, VaiTro.ThuThu)]
public sealed class QuanLyPhieuMuonController : Controller
{
    private readonly IPhieuMuonService _service;
    private readonly ICurrentUserService _current;

    public QuanLyPhieuMuonController(
        IPhieuMuonService service,
        ICurrentUserService current)
    {
        _service = service;
        _current = current;
    }

    private int ActingId => _current.User!.MaTaiKhoan;

    // ---------- Danh sách ----------

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        int? maTheLoai,
        TrangThaiPhieuMuon? trangThai,
        DateOnly? tuNgay,
        DateOnly? denNgay,
        bool quaHan = false,
        string sapXep = "ngay_giam",
        int page = 1,
        int pageSize = PagingDefaults.DefaultPageSize)
    {
        var ct = HttpContext.RequestAborted;
        search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        if (maTheLoai is <= 0) maTheLoai = null;
        if (trangThai.HasValue && !Enum.IsDefined(trangThai.Value))
            trangThai = null;

        if (tuNgay.HasValue && denNgay.HasValue && tuNgay.Value > denNgay.Value)
        {
            TempData["Loi"] = "Ngày bắt đầu lớn hơn ngày kết thúc; hệ thống đã đổi vị trí hai ngày để tiếp tục lọc.";
            (tuNgay, denNgay) = (denNgay, tuNgay);
        }

        var filter = new PhieuMuonFilter(
            search, maTheLoai, trangThai, tuNgay, denNgay, quaHan,
            sapXep, page, pageSize);
        var result = await _service.GetListAsync(filter, ct);
        var categoryRows = await _service.GetTheLoaiOptionsAsync(ct);
        var categoryOptions = categoryRows.Select(t => new SelectListItem
        {
            Value = t.MaTheLoai.ToString(),
            Text = t.TenTheLoai,
            Selected = maTheLoai == t.MaTheLoai
        }).ToList();

        var routeValues = new Dictionary<string, string>();
        if (search is not null) routeValues["search"] = search;
        if (maTheLoai.HasValue) routeValues["maTheLoai"] = maTheLoai.Value.ToString();
        if (trangThai.HasValue) routeValues["trangThai"] = trangThai.Value.ToString();
        if (tuNgay.HasValue) routeValues["tuNgay"] = tuNgay.Value.ToString("yyyy-MM-dd");
        if (denNgay.HasValue) routeValues["denNgay"] = denNgay.Value.ToString("yyyy-MM-dd");
        if (quaHan) routeValues["quaHan"] = "true";
        if (!string.IsNullOrWhiteSpace(sapXep)) routeValues["sapXep"] = sapXep;

        return View(new QuanLyPhieuMuonIndexViewModel
        {
            Search = search,
            MaTheLoai = maTheLoai,
            TrangThai = trangThai,
            TuNgay = tuNgay,
            DenNgay = denNgay,
            QuaHan = quaHan,
            SapXep = sapXep,
            PageSize = result.PageSize,
            Result = result,
            TheLoaiOptions = categoryOptions,
            Pager = PagerViewModel.From(result, "QuanLyPhieuMuon", nameof(Index), routeValues)
        });
    }

    // ---------- Chi tiết ----------

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var detail = await _service.GetDetailsAsync(id, HttpContext.RequestAborted);
        return detail is null ? NotFound() : View(detail);
    }

    // ---------- Duyệt / từ chối ----------

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Process(int id, bool duyet, string? nhanXetThuThu)
    {
        var result = await _service.ProcessAsync(
            new ProcessPhieuMuonInput(id, duyet, nhanXetThuThu),
            ActingId,
            HttpContext.RequestAborted);

        if (!result.Succeeded)
            TempData["Loi"] = result.FirstError;
        else
            TempData["ThongBao"] = duyet
                ? "Đã duyệt phiếu mượn."
                : "Đã từ chối phiếu mượn.";

        return RedirectToAction(nameof(Details), new { id });
    }

    // ---------- Giao sách ----------

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HandOver(int id)
    {
        var result = await _service.HandOverAsync(
            id, ActingId, HttpContext.RequestAborted);

        if (!result.Succeeded)
            TempData["Loi"] = result.FirstError;
        else
            TempData["ThongBao"] = "Đã giao sách và cập nhật hạn trả.";

        return RedirectToAction(nameof(Details), new { id });
    }

    // ---------- Hủy phiếu (không xóa lịch sử) ----------

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string? ghiChu)
    {
        var result = await _service.CancelAsync(
            id, ActingId, ghiChu, HttpContext.RequestAborted);

        if (!result.Succeeded)
            TempData["Loi"] = result.FirstError;
        else
            TempData["ThongBao"] = "Đã hủy phiếu mượn.";

        return RedirectToAction(nameof(Details), new { id });
    }
}
