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
    private readonly IAnhBiaService _anhBiaService;
    private readonly ICurrentUserService _current;

    public SachController(ISachService sachService, ITheLoaiService theLoaiService, IAnhBiaService anhBiaService, ICurrentUserService current)
    {
        _sachService = sachService;
        _theLoaiService = theLoaiService;
        _anhBiaService = anhBiaService;
        _current = current;
    }

    [HttpGet]
    [AuthorizeRole(VaiTro.Admin, VaiTro.ThuThu)]
    public async Task<IActionResult> Index(
        string? search,
        int? maTheLoai,
        TrangThaiSach? trangThai,
        int page = 1,
        int pageSize = PagingDefaults.DefaultPageSize,
        string? tacGia = null,
        int? namTu = null,
        int? namDen = null)
    {
        if (trangThai.HasValue && !Enum.IsDefined(trangThai.Value))
            trangThai = null;
        search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        tacGia = string.IsNullOrWhiteSpace(tacGia) ? null : tacGia.Trim();

        var result = await _sachService.GetListAsync(
            new SachFilter(search, maTheLoai, trangThai, page, pageSize, tacGia, namTu, namDen),
            HttpContext.RequestAborted);
        var routeValues = new Dictionary<string, string>();
        if (search is not null) routeValues["search"] = search;
        if (maTheLoai.HasValue) routeValues["maTheLoai"] = maTheLoai.Value.ToString();
        if (trangThai.HasValue) routeValues["trangThai"] = trangThai.Value.ToString();
        if (tacGia is not null) routeValues["tacGia"] = tacGia;
        if (namTu.HasValue) routeValues["namTu"] = namTu.Value.ToString();
        if (namDen.HasValue) routeValues["namDen"] = namDen.Value.ToString();
        ViewBag.Pager = PagerViewModel.From(result, "Sach", nameof(Index), routeValues);

        // Lọc/tìm kiếm/phân trang qua AJAX: chỉ trả về partial bảng + phân trang.
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return PartialView("_BangSach", result);

        ViewBag.Search = search;
        ViewBag.MaTheLoai = maTheLoai;
        ViewBag.TrangThai = trangThai;
        ViewBag.TacGia = tacGia;
        ViewBag.NamTu = namTu;
        ViewBag.NamDen = namDen;
        ViewBag.TheLoaiOptions = await GetCategoryOptionsAsync(maTheLoai);
        ViewBag.TacGiaOptions = await _sachService.GetAuthorOptionsAsync(HttpContext.RequestAborted);
        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var detail = await _sachService.GetAsync(id, HttpContext.RequestAborted);
        if (detail is null)
        {
            // Không có id hoặc sách không tồn tại/đã xóa: về danh sách (thủ thư) hoặc kho sách (khách, bạn đọc)
            TempData["Loi"] = "Không tìm thấy sách hoặc sách đã bị xóa.";
            return _current.IsInRole(VaiTro.Admin, VaiTro.ThuThu)
                ? RedirectToAction(nameof(Index))
                : RedirectToAction("Catalog", "Home");
        }
        return View(detail);
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
    public async Task<IActionResult> Create(Sach model, IFormFile? anhBia, bool xoaAnh = false)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.TheLoaiOptions = await GetCategoryOptionsAsync(model.MaTheLoai);
            if (IsAjaxRequest())
                return PartialView(nameof(Create), model);
            return View(model);
        }

        var imgPath = await XuLyAnhBiaAsync(anhBia, xoaAnh, null);
        if (imgPath == null && anhBia is { Length: > 0 })
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
            imgPath ?? model.ImgPath), HttpContext.RequestAborted);

        if (!result.Succeeded)
        {
            result.AddTo(ModelState);
            ViewBag.TheLoaiOptions = await GetCategoryOptionsAsync(model.MaTheLoai);
            if (IsAjaxRequest())
                return PartialView(nameof(Create), model);
            return View(model);
        }

        TempData["ThongBao"] = "Đã thêm sách.";
        if (IsAjaxRequest())
            return Json(new { success = true, redirectUrl = Url.Action(nameof(Index)), message = "Đã thêm sách." });
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
    public async Task<IActionResult> Edit(int id, Sach model, IFormFile? anhBia, bool xoaAnh = false)
    {
        var detail = await _sachService.GetAsync(id, HttpContext.RequestAborted);
        if (detail is null)
            return NotFound();
        model.MaSach = id;
        model.ImgPath = detail.ImgPath;

        if (!ModelState.IsValid)
        {
            ViewBag.TheLoaiOptions = await GetCategoryOptionsAsync(model.MaTheLoai);
            if (IsAjaxRequest())
                return PartialView(nameof(Edit), model);
            return View(model);
        }

        var imgPath = await XuLyAnhBiaAsync(anhBia, xoaAnh, detail.ImgPath);
        if (imgPath == null && anhBia is { Length: > 0 })
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
            imgPath ?? (xoaAnh ? null : detail.ImgPath)), HttpContext.RequestAborted);

        if (!result.Succeeded)
        {
            result.AddTo(ModelState);
            ViewBag.TheLoaiOptions = await GetCategoryOptionsAsync(model.MaTheLoai);
            if (IsAjaxRequest())
                return PartialView(nameof(Edit), model);
            return View(model);
        }

        TempData["ThongBao"] = "Đã cập nhật sách.";
        if (IsAjaxRequest())
            return Json(new { success = true, redirectUrl = Url.Action(nameof(Index)), message = "Đã cập nhật sách." });
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [AuthorizeRole(VaiTro.Admin, VaiTro.ThuThu)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleTrangThai(int id, string? returnUrl)
    {
        var result = await _sachService.ToggleTrangThaiAsync(id, HttpContext.RequestAborted);
        if (result.Succeeded)
        {
            var detail = await _sachService.GetAsync(id, HttpContext.RequestAborted);
            TempData["ThongBao"] = detail?.TrangThai == TrangThaiSach.DangPhucVu
                ? "Đã mở lại sách."
                : "Đã chuyển sách sang Tạm ngưng.";
        }
        else
        {
            TempData["Loi"] = result.FirstError;
        }

        if (IsAjaxRequest())
        {
            return Json(new
            {
                success = result.Succeeded,
                redirectUrl = result.Succeeded ? Url.Action(nameof(Index)) : Url.Action(nameof(Details), new { id }),
                message = result.Succeeded ? TempData["ThongBao"]?.ToString() : result.FirstError
            });
        }
        return Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction(nameof(Index));
    }

    [HttpPost, ActionName("Delete")]
    [AuthorizeRole(VaiTro.Admin, VaiTro.ThuThu)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        // Đọc đường dẫn ảnh TRƯỚC khi xóa, vì xóa cứng rồi thì GetAsync không còn dữ liệu để lấy ImgPath
        var detail = await _sachService.GetAsync(id, HttpContext.RequestAborted);
        var result = await _sachService.DeleteAsync(id, HttpContext.RequestAborted);
        if (!result.Succeeded)
        {
            TempData["Loi"] = result.FirstError;
            if (IsAjaxRequest())
                return Json(new { success = false, redirectUrl = Url.Action(nameof(Details), new { id }), message = result.FirstError });
            return RedirectToAction(nameof(Details), new { id });
        }

        if (result.Value)
        {
            if (detail?.ImgPath is { } imgPath)
                _anhBiaService.Delete(imgPath);
            TempData["ThongBao"] = "Đã xóa sách.";
        }
        else
        {
            TempData["ThongBao"] = "Sách đã có lịch sử mượn → chuyển Ngừng phục vụ, giữ dữ liệu.";
        }
        if (IsAjaxRequest())
            return Json(new { success = true, redirectUrl = Url.Action(nameof(Index)), message = TempData["ThongBao"]?.ToString() });
        return RedirectToAction(nameof(Index));
    }

    private bool IsAjaxRequest() =>
        string.Equals(Request.Headers.XRequestedWith, "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);

    // Trả về đường dẫn ảnh mới (hoặc null khi xóa/không đổi); lỗi upload được nạp vào ModelState.
    private async Task<string?> XuLyAnhBiaAsync(IFormFile? anhBia, bool xoaAnh, string? imgPathCu)
    {
        if (xoaAnh)
        {
            _anhBiaService.Delete(imgPathCu);
            return null;
        }

        if (anhBia is not { Length: > 0 })
            return null;

        var upload = await _anhBiaService.SaveAsync(anhBia, HttpContext.RequestAborted);
        if (!upload.Succeeded)
        {
            ModelState.AddModelError("ImgPath", upload.FirstError);
            return null;
        }

        _anhBiaService.Delete(imgPathCu);
        return upload.Value;
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
