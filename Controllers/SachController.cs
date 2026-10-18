// Họ và tên: Vũ Tiến Thành
// Mã sinh viên: 23103100257
// Nội dung thực hiện: Xây dựng controller cho việc quản lý sách (Admin, Thủ thư)
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using QuanLyThuVien_UNETI4_TI17A1CL.Models;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Auth;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Common;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;
using QuanLyThuVien_UNETI4_TI17A1CL.ViewModels;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Controllers;

public class SachController : Controller
{
    private readonly ISachService _sachService;
    private readonly ITheLoaiService _theLoaiService;

    public SachController(ISachService sachService, ITheLoaiService theLoaiService)
    {
        _sachService = sachService;
        _theLoaiService = theLoaiService;
    }

    [HttpGet]
    [AuthorizeRole(VaiTro.Admin, VaiTro.ThuThu)]
    public async Task<IActionResult> Index(
        string? search,
        int? maTheLoai,
        TrangThaiSach? trangThai,
        int page = 1,
        int pageSize = PagingDefaults.DefaultPageSize)
    {
        if (trangThai.HasValue && !Enum.IsDefined(trangThai.Value))
            trangThai = null;
        search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

        var result = await _sachService.GetListAsync(
            new SachFilter(search, maTheLoai, trangThai, page, pageSize), HttpContext.RequestAborted);
        var routeValues = new Dictionary<string, string>();
        if (search is not null) routeValues["search"] = search;
        if (maTheLoai.HasValue) routeValues["maTheLoai"] = maTheLoai.Value.ToString();
        if (trangThai.HasValue) routeValues["trangThai"] = trangThai.Value.ToString();

        ViewBag.Search = search;
        ViewBag.MaTheLoai = maTheLoai;
        ViewBag.TrangThai = trangThai;
        ViewBag.TheLoaiOptions = await GetCategoryOptionsAsync(maTheLoai);
        ViewBag.Pager = PagerViewModel.From(result, "Sach", nameof(Index), routeValues);
        return View(result);
    }

    [HttpGet]
    [AuthorizeRole(VaiTro.Admin, VaiTro.ThuThu, VaiTro.BanDoc)]
    public async Task<IActionResult> Details(int id)
    {
        var detail = await _sachService.GetAsync(id, HttpContext.RequestAborted);
        return detail is null ? NotFound() : View(detail);
    }

    [HttpGet]
    [AuthorizeRole(VaiTro.Admin, VaiTro.ThuThu)]
    public async Task<IActionResult> Create()
    {
        ViewBag.TheLoaiOptions = await GetCategoryOptionsAsync(null);
        return View(new Sach());
    }

    [HttpPost]
    [AuthorizeRole(VaiTro.Admin, VaiTro.ThuThu)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Sach model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.TheLoaiOptions = await GetCategoryOptionsAsync(model.MaTheLoai);
            return View(model);
        }

        var result = await _sachService.CreateAsync(new CreateSachInput(
            model.MaTheLoai,
            model.TenSach,
            model.TacGia,
            model.NhaXuatBan,
            model.NamXuatBan,
            model.SoLuongTong,
            model.GiaBia,
            model.ViTriKe,
            model.MoTa,
            model.TrangThai,
            model.ImgPath), HttpContext.RequestAborted);

        if (!result.Succeeded)
        {
            result.AddTo(ModelState);
            ViewBag.TheLoaiOptions = await GetCategoryOptionsAsync(model.MaTheLoai);
            return View(model);
        }

        TempData["ThongBao"] = "Đã thêm sách.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [AuthorizeRole(VaiTro.Admin, VaiTro.ThuThu)]
    public async Task<IActionResult> Edit(int id)
    {
        var detail = await _sachService.GetAsync(id, HttpContext.RequestAborted);
        if (detail is null)
            return NotFound();

        ViewBag.TheLoaiOptions = await GetCategoryOptionsAsync(detail.MaTheLoai);
        return View(ToModel(detail));
    }

    [HttpPost]
    [AuthorizeRole(VaiTro.Admin, VaiTro.ThuThu)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Sach model)
    {
        var detail = await _sachService.GetAsync(id, HttpContext.RequestAborted);
        if (detail is null)
            return NotFound();
        model.MaSach = id;

        if (!ModelState.IsValid)
        {
            ViewBag.TheLoaiOptions = await GetCategoryOptionsAsync(model.MaTheLoai);
            return View(model);
        }

        var result = await _sachService.UpdateAsync(new UpdateSachInput(
            id,
            model.MaTheLoai,
            model.TenSach,
            model.TacGia,
            model.NhaXuatBan,
            model.NamXuatBan,
            model.SoLuongTong,
            model.GiaBia,
            model.ViTriKe,
            model.MoTa,
            model.TrangThai,
            model.ImgPath), HttpContext.RequestAborted);

        if (!result.Succeeded)
        {
            result.AddTo(ModelState);
            ViewBag.TheLoaiOptions = await GetCategoryOptionsAsync(model.MaTheLoai);
            return View(model);
        }

        TempData["ThongBao"] = "Đã cập nhật sách.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    [AuthorizeRole(VaiTro.Admin, VaiTro.ThuThu)]
    public async Task<IActionResult> Delete(int id)
    {
        var detail = await _sachService.GetAsync(id, HttpContext.RequestAborted);
        return detail is null ? NotFound() : View(detail);
    }

    [HttpPost, ActionName("Delete")]
    [AuthorizeRole(VaiTro.Admin, VaiTro.ThuThu)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result = await _sachService.DeleteAsync(id, HttpContext.RequestAborted);
        if (result.Succeeded)
        {
            TempData["ThongBao"] = "Đã xóa sách.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Loi"] = result.FirstError;
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<IEnumerable<SelectListItem>> GetCategoryOptionsAsync(int? selectedMaTheLoai)
    {
        var categories = (await _theLoaiService.GetActiveOptionsAsync()).ToList();
        if (selectedMaTheLoai is { } selected && categories.All(t => t.MaTheLoai != selected))
        {
            var category = await _theLoaiService.GetAsync(selected, HttpContext.RequestAborted);
            if (category is not null)
                categories.Add(new TheLoaiOption(category.MaTheLoai, $"{category.TenTheLoai} (ngừng hoạt động)"));
        }

        return new SelectList(categories, nameof(TheLoaiOption.MaTheLoai),
            nameof(TheLoaiOption.TenTheLoai), selectedMaTheLoai);
    }

    private static Sach ToModel(SachDetail detail) => new()
    {
        MaSach = detail.MaSach,
        MaTheLoai = detail.MaTheLoai,
        TenSach = detail.TenSach,
        TacGia = detail.TacGia,
        NhaXuatBan = detail.NhaXuatBan,
        NamXuatBan = detail.NamXuatBan,
        SoLuongTong = detail.SoLuongTong,
        GiaBia = detail.GiaBia,
        ViTriKe = detail.ViTriKe,
        MoTa = detail.MoTa,
        TrangThai = detail.TrangThai,
        ImgPath = detail.ImgPath
    };
}
