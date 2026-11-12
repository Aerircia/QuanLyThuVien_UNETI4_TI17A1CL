// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Controller xử lý quản lý tài khoản (Admin)

using Microsoft.AspNetCore.Mvc;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Auth;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Common;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;
using QuanLyThuVien_UNETI4_TI17A1CL.ViewModels;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Controllers;

/// <summary>
/// Quản lý tài khoản. Cả controller chỉ dành cho Admin (kiểm tra ở cấp class nên action mới thêm vào
/// không thể bị bỏ sót). Các quy tắc an toàn (tự khóa/xóa, Admin cuối cùng...) nằm trong ITaiKhoanService.
/// </summary>
[AuthorizeRole(VaiTro.Admin)]
public class QuanLyTaiKhoanController : Controller
{
    private readonly ITaiKhoanService _service;
    private readonly ICurrentUserService _current;

    public QuanLyTaiKhoanController(ITaiKhoanService service, ICurrentUserService current)
    {
        _service = service;
        _current = current;
    }

    private int ActingId => _current.User!.MaTaiKhoan;

    // ---------- Danh sách ----------
    [HttpGet("/Admin/QuanLyTaiKhoan")]
    public async Task<IActionResult> Index(
        string? search,
        VaiTro? vaiTro,
        TrangThaiTaiKhoan? trangThai,
        int page = 1,
        int pageSize = PagingDefaults.DefaultPageSize,
        string? sort = null)
    {
        if (vaiTro.HasValue && !Enum.IsDefined(vaiTro.Value)) vaiTro = null;
        if (trangThai.HasValue && !Enum.IsDefined(trangThai.Value)) trangThai = null;
        search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        sort = sort is "name" or "email" or "role" or "status" or "username_desc" ? sort : "username";

        var result = await _service.GetListAsync(
            new TaiKhoanFilter(search, vaiTro, trangThai, page, pageSize, sort), HttpContext.RequestAborted);

        var routeValues = new Dictionary<string, string>();
        if (search is not null) routeValues["search"] = search;
        if (vaiTro.HasValue) routeValues["vaiTro"] = vaiTro.Value.ToString();
        if (trangThai.HasValue) routeValues["trangThai"] = trangThai.Value.ToString();
        if (sort != "username") routeValues["sort"] = sort;

        var model = new TaiKhoanListViewModel
        {
            Search = search,
            VaiTro = vaiTro,
            TrangThai = trangThai,
            Sort = sort,
            PageSize = result.PageSize,
            CurrentMaTaiKhoan = ActingId,
            Result = result,
            Pager = PagerViewModel.From(result, "QuanLyTaiKhoan", nameof(Index), routeValues)
        };
        Response.Headers.Vary = "X-Requested-With";
        return Request.Headers["X-Requested-With"] == "XMLHttpRequest"
            ? PartialView("_IndexResults", model)
            : View(model);
    }

    // ---------- Thêm ----------

    [HttpGet("/Admin/QuanLyTaiKhoan/Create")]
    public IActionResult Create() => View(new TaiKhoanCreateViewModel());
    [HttpPost("/Admin/QuanLyTaiKhoan/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TaiKhoanCreateViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _service.CreateAsync(
            new CreateTaiKhoanInput(model.TenDangNhap, model.MatKhau, model.HoTen, model.Email, model.VaiTro, model.TrangThai),
            HttpContext.RequestAborted);

        if (!result.Succeeded)
        {
            result.AddTo(ModelState);
            model.MatKhau = model.XacNhanMatKhau = string.Empty;
            return View(model);
        }

        TempData["ThongBao"] = "Đã thêm tài khoản.";
        return RedirectToAction(nameof(Index));
    }

    // ---------- Sửa ----------
    [HttpGet("/Admin/QuanLyTaiKhoan/Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var detail = await _service.GetAsync(id, HttpContext.RequestAborted);
        if (detail is null) return NotFound();

        return View(new TaiKhoanEditViewModel
        {
            MaTaiKhoan = detail.MaTaiKhoan,
            TenDangNhap = detail.TenDangNhap,
            CoHoSoBanDoc = detail.CoHoSoBanDoc,
            HoTen = detail.HoTen,
            Email = detail.Email,
            VaiTro = detail.VaiTro,
            TrangThai = detail.TrangThai
        });
    }
    [HttpPost("/Admin/QuanLyTaiKhoan/Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(TaiKhoanEditViewModel model)
    {
        // Các trường chỉ hiển thị luôn lấy lại từ DB, không tin dữ liệu form.
        var detail = await _service.GetAsync(model.MaTaiKhoan, HttpContext.RequestAborted);
        if (detail is null) return NotFound();
        model.TenDangNhap = detail.TenDangNhap;
        model.CoHoSoBanDoc = detail.CoHoSoBanDoc;

        if (!ModelState.IsValid)
            return View(model);

        var result = await _service.UpdateAsync(
            new UpdateTaiKhoanInput(model.MaTaiKhoan, model.HoTen, model.Email, model.VaiTro, model.TrangThai),
            ActingId,
            HttpContext.RequestAborted);

        if (!result.Succeeded)
        {
            result.AddTo(ModelState);
            return View(model);
        }

        TempData["ThongBao"] = "Đã cập nhật tài khoản.";
        return RedirectToAction(nameof(Index));
    }

    // ---------- Khóa / mở khóa ----------
    [HttpPost("/Admin/QuanLyTaiKhoan/SetStatus/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStatus(int id, TrangThaiTaiKhoan? trangThai, string? returnUrl)
    {
        if (trangThai is null || !Enum.IsDefined(trangThai.Value) || !ModelState.IsValid)
        {
            TempData["Loi"] = "Trạng thái tài khoản không hợp lệ.";
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);
            return RedirectToAction(nameof(Index));
        }

        var status = trangThai.Value;
        var result = await _service.SetStatusAsync(id, status, ActingId, HttpContext.RequestAborted);

        if (result.Succeeded)
            TempData["ThongBao"] = status == TrangThaiTaiKhoan.BiKhoa
                ? "Đã khóa tài khoản."
                : "Đã mở khóa tài khoản.";
        else
            TempData["Loi"] = result.FirstError;

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return RedirectToAction(nameof(Index));
    }

    // ---------- Đặt lại mật khẩu (Admin) ----------
    [HttpGet("/Admin/QuanLyTaiKhoan/ResetPassword/{id:int}")]
    public async Task<IActionResult> ResetPassword(int id)
    {
        var detail = await _service.GetAsync(id, HttpContext.RequestAborted);
        if (detail is null) return NotFound();

        return View(new AdminResetPasswordViewModel { MaTaiKhoan = id, TenDangNhap = detail.TenDangNhap });
    }
    [HttpPost("/Admin/QuanLyTaiKhoan/ResetPassword/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(AdminResetPasswordViewModel model)
    {
        var detail = await _service.GetAsync(model.MaTaiKhoan, HttpContext.RequestAborted);
        if (detail is null) return NotFound();
        model.TenDangNhap = detail.TenDangNhap;

        if (!ModelState.IsValid)
        {
            model.MatKhauMoi = model.XacNhanMatKhauMoi = string.Empty;
            return View(model);
        }

        var result = await _service.ResetPasswordAsync(model.MaTaiKhoan, model.MatKhauMoi, HttpContext.RequestAborted);
        if (!result.Succeeded)
        {
            result.AddTo(ModelState);
            model.MatKhauMoi = model.XacNhanMatKhauMoi = string.Empty;
            return View(model);
        }

        TempData["ThongBao"] = $"Đã đặt lại mật khẩu cho tài khoản {detail.TenDangNhap}.";
        return RedirectToAction(nameof(Index));
    }

    // ---------- Xóa ----------
    [HttpGet("/Admin/QuanLyTaiKhoan/Delete/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var detail = await _service.GetAsync(id, HttpContext.RequestAborted);
        if (detail is null) return NotFound();

        return View(detail);
    }

    [HttpPost("/Admin/QuanLyTaiKhoan/Delete/{id:int}"), ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result = await _service.DeleteAsync(id, ActingId, HttpContext.RequestAborted);

        if (result.Succeeded)
        {
            TempData["ThongBao"] = "Đã xóa tài khoản.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Loi"] = result.FirstError;
        return RedirectToAction(nameof(Index));
    }
}
