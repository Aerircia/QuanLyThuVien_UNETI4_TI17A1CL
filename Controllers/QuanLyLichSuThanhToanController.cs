// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Xây dựng Controller QuanLyLichSuThanhToanController dành cho Admin và Thủ thư để quản lý lịch sử thanh toán của bạn đọc
using Microsoft.AspNetCore.Mvc;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Auth;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Common;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;
using QuanLyThuVien_UNETI4_TI17A1CL.ViewModels;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Controllers;

[AuthorizeRole(VaiTro.Admin, VaiTro.ThuThu)]
public class QuanLyLichSuThanhToanController : Controller
{
    private readonly IThanhToanService _service;
    private readonly IBanDocService _banDocService;
    private readonly ICurrentUserService _current;

    public QuanLyLichSuThanhToanController(IThanhToanService service, IBanDocService banDocService, ICurrentUserService current)
    {
        _service = service;
        _banDocService = banDocService;
        _current = current;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search, PhuongThucThanhToan? phuongThuc, TrangThaiGiaoDich? trangThai,
        DateOnly? tu, DateOnly? den, int page = 1, int pageSize = PagingDefaults.DefaultPageSize)
    {
        if (phuongThuc.HasValue && !Enum.IsDefined(phuongThuc.Value)) phuongThuc = null;
        if (trangThai.HasValue && !Enum.IsDefined(trangThai.Value)) trangThai = null;
        search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

        var result = await _service.GetListAsync(
            new ThanhToanFilter(search, phuongThuc, trangThai, tu, den, page, pageSize), null, HttpContext.RequestAborted);

        var route = new Dictionary<string, string>();
        if (search is not null) route["search"] = search;
        if (phuongThuc.HasValue) route["phuongThuc"] = phuongThuc.Value.ToString();
        if (trangThai.HasValue) route["trangThai"] = trangThai.Value.ToString();
        if (tu.HasValue) route["tu"] = tu.Value.ToString("yyyy-MM-dd");
        if (den.HasValue) route["den"] = den.Value.ToString("yyyy-MM-dd");

        return View(new ThanhToanListViewModel
        {
            Search = search, PhuongThuc = phuongThuc, TrangThai = trangThai, Tu = tu, Den = den,
            Result = result,
            Pager = PagerViewModel.From(result, "QuanLyLichSuThanhToan", nameof(Index), route)
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var detail = await _service.GetAsync(id, null, HttpContext.RequestAborted);
        return detail is null ? NotFound() : View(detail);
    }

    // Danh sách bạn đọc còn nợ phạt để thu tại quầy.
    [HttpGet]
    public async Task<IActionResult> ThuPhat(string? search, int page = 1, int pageSize = PagingDefaults.DefaultPageSize)
    {
        search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        var result = await _service.GetDebtorsAsync(search, page, pageSize, HttpContext.RequestAborted);
        var route = new Dictionary<string, string>();
        if (search is not null) route["search"] = search;
        return View(new DebtorListViewModel
        {
            Search = search, Result = result,
            Pager = PagerViewModel.From(result, "QuanLyLichSuThanhToan", nameof(ThuPhat), route)
        });
    }

    [HttpGet]
    public async Task<IActionResult> ThuPhatBanDoc(int id)
    {
        var banDoc = await _banDocService.GetAsync(id, HttpContext.RequestAborted);
        if (banDoc is null) return NotFound();
        var fines = await _service.GetUnpaidFinesAsync(id, HttpContext.RequestAborted);
        return View(new ThuPhatBanDocViewModel
        {
            MaBanDoc = banDoc.MaBanDoc, HoTen = banDoc.HoTen, MaSoThe = banDoc.MaSoThe, Fines = fines
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> XacNhanThuPhat(int id, int[]? maTras)
    {
        var result = await _service.RecordCounterAsync(
            maTras ?? Array.Empty<int>(), _current.User!.MaTaiKhoan, HttpContext.RequestAborted);
        if (!result.Succeeded)
        {
            TempData["Loi"] = result.FirstError;
            return RedirectToAction(nameof(ThuPhatBanDoc), new { id });
        }
        TempData["ThongBao"] = "Đã ghi nhận thu phạt tại quầy.";
        return RedirectToAction(nameof(Details), new { id = result.Value });
    }
}
