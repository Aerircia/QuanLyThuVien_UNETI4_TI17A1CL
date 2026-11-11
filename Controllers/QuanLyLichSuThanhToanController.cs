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

    public QuanLyLichSuThanhToanController(IThanhToanService service)
    {
        _service = service;
    }
    [HttpGet("/ThuThu/LichSuThanhToan")]
    public async Task<IActionResult> Index(
        string? search, PhuongThucThanhToan? phuongThuc, TrangThaiGiaoDich? trangThai,
        DateOnly? tu, DateOnly? den, int page = 1, int pageSize = PagingDefaults.DefaultPageSize, string? sort = null)
    {
        if (phuongThuc.HasValue && !Enum.IsDefined(phuongThuc.Value)) phuongThuc = null;
        if (trangThai.HasValue && !Enum.IsDefined(trangThai.Value)) trangThai = null;
        search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        sort = sort is "oldest" or "amount_asc" or "amount_desc" ? sort : "newest";

        var result = await _service.GetListAsync(
            new ThanhToanFilter(search, phuongThuc, trangThai, tu, den, page, pageSize, sort), null, HttpContext.RequestAborted);

        var route = new Dictionary<string, string>();
        if (search is not null) route["search"] = search;
        if (phuongThuc.HasValue) route["phuongThuc"] = phuongThuc.Value.ToString();
        if (trangThai.HasValue) route["trangThai"] = trangThai.Value.ToString();
        if (tu.HasValue) route["tu"] = tu.Value.ToString("yyyy-MM-dd");
        if (den.HasValue) route["den"] = den.Value.ToString("yyyy-MM-dd");
        if (sort != "newest") route["sort"] = sort;

        var model = new ThanhToanListViewModel
        {
            Search = search, PhuongThuc = phuongThuc, TrangThai = trangThai, Tu = tu, Den = den, Sort = sort,
            Result = result,
            Pager = PagerViewModel.From(result, "QuanLyLichSuThanhToan", nameof(Index), route)
        };
        ViewData["Ctl"] = "QuanLyLichSuThanhToan";
        ViewData["Act"] = "Details";
        ViewData["ShowReader"] = true;
        Response.Headers.Vary = "X-Requested-With";
        return Request.Headers["X-Requested-With"] == "XMLHttpRequest"
            ? PartialView("_IndexResults", model)
            : View(model);
    }
    [HttpGet("/ThuThu/LichSuThanhToan/Details/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var detail = await _service.GetAsync(id, null, HttpContext.RequestAborted);
        return detail is null ? NotFound() : View(detail);
    }

}
