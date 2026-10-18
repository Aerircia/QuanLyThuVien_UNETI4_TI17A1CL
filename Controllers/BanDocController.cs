// Họ và tên: Nguyễn Hữu Tuấn
// Mã sinh viên: 23103100136
// Nội dung thực hiện: Controller xử lý quản lý bạn đọc và hồ sơ cá nhân của bạn đọc (Module 3)

using Microsoft.AspNetCore.Mvc;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Auth;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Common;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;
using QuanLyThuVien_UNETI4_TI17A1CL.ViewModels;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Controllers;

public class BanDocController : Controller
{
    private readonly IBanDocService _banDocService;
    private readonly ICurrentUserService _current;

    public BanDocController(IBanDocService banDocService, ICurrentUserService current)
    {
        _banDocService = banDocService;
        _current = current;
    }

    // 1. Dành cho BẠN ĐỌC: Xem và cập nhật thông tin cá nhân của chính mình
    [AuthorizeRole]
    [HttpGet]
    public async Task<IActionResult> MyProfile()
    {
        var user = _current.User!;
        var detail = await _banDocService.GetByTaiKhoanAsync(user.MaTaiKhoan, HttpContext.RequestAborted);

        if (detail is null)
        {
            // Tài khoản chưa có hồ sơ bạn đọc liên kết
            ViewBag.ThongBaoChuaCoHoSo = "Tài khoản của bạn chưa được liên kết với hồ sơ bạn đọc nào. Vui lòng liên hệ thủ thư để được cấp thẻ thư viện.";
            return View((BanDocProfileViewModel?)null);
        }

        var model = new BanDocProfileViewModel
        {
            MaBanDoc = detail.MaBanDoc,
            HoTen = detail.HoTen,
            TenDangNhap = detail.TenDangNhap,
            NgaySinh = detail.NgaySinh,
            GioiTinh = detail.GioiTinh,
            SoDienThoai = detail.SoDienThoai,
            Email = detail.Email,
            DiaChi = detail.DiaChi,
            MaSoThe = detail.MaSoThe,
            LoaiBanDoc = detail.LoaiBanDoc,
            NgayCapThe = detail.NgayCapThe,
            NgayHetHanThe = detail.NgayHetHanThe,
            TrangThai = detail.TrangThai,
            SoPhieuDangMuon = detail.SoPhieuDangMuon,
            SoPhieuQuaHan = detail.SoPhieuQuaHan,
            TongTienPhatChuaNop = detail.TongTienPhatChuaNop
        };

        return View(model);
    }

    [AuthorizeRole]
    [HttpGet]
    public async Task<IActionResult> EditProfile()
    {
        var user = _current.User!;
        var detail = await _banDocService.GetByTaiKhoanAsync(user.MaTaiKhoan, HttpContext.RequestAborted);

        if (detail is null)
            return RedirectToAction(nameof(MyProfile));

        var model = new BanDocEditProfileViewModel
        {
            MaBanDoc = detail.MaBanDoc,
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
        };

        return View(model);
    }

    [AuthorizeRole]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProfile(BanDocEditProfileViewModel model)
    {
        var user = _current.User!;
        var detail = await _banDocService.GetByTaiKhoanAsync(user.MaTaiKhoan, HttpContext.RequestAborted);
        if (detail is null)
            return RedirectToAction(nameof(MyProfile));

        // Bảo mật: nạp lại các trường chỉ đọc để hiển thị nếu validate lỗi
        model.MaBanDoc = detail.MaBanDoc;
        model.MaSoThe = detail.MaSoThe;
        model.LoaiBanDoc = detail.LoaiBanDoc;
        model.NgayCapThe = detail.NgayCapThe;
        model.NgayHetHanThe = detail.NgayHetHanThe;
        model.TrangThai = detail.TrangThai;

        if (!ModelState.IsValid)
            return View(model);

        var result = await _banDocService.UpdateProfileAsync(
            new UpdateProfileInput(model.HoTen, model.NgaySinh, model.GioiTinh, model.SoDienThoai, model.Email, model.DiaChi),
            user.MaTaiKhoan,
            HttpContext.RequestAborted);

        if (!result.Succeeded)
        {
            result.AddTo(ModelState);
            return View(model);
        }

        TempData["ThongBao"] = "Cập nhật thông tin cá nhân thành công.";
        return RedirectToAction(nameof(MyProfile));
    }

    // 2. Dành cho THỦ THƯ & ADMIN: Quản lý bạn đọc
    [AuthorizeRole(VaiTro.Admin, VaiTro.ThuThu)]
    [HttpGet]
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

        return View(new BanDocListViewModel
        {
            Search = search,
            LoaiBanDoc = loaiBanDoc,
            TrangThai = trangThai,
            PageSize = result.PageSize,
            Result = result,
            Pager = PagerViewModel.From(result, "BanDoc", nameof(Index), routeValues)
        });
    }

    [AuthorizeRole(VaiTro.Admin, VaiTro.ThuThu)]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewBag.UnlinkedAccounts = await _banDocService.GetUnlinkedAccountsAsync(null, HttpContext.RequestAborted);
        return View(new BanDocCreateViewModel());
    }

    [AuthorizeRole(VaiTro.Admin, VaiTro.ThuThu)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BanDocCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.UnlinkedAccounts = await _banDocService.GetUnlinkedAccountsAsync(null, HttpContext.RequestAborted);
            return View(model);
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
            return View(model);
        }

        TempData["ThongBao"] = "Thêm hồ sơ bạn đọc thành công.";
        return RedirectToAction(nameof(Index));
    }

    [AuthorizeRole(VaiTro.Admin, VaiTro.ThuThu)]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var detail = await _banDocService.GetAsync(id, HttpContext.RequestAborted);
        if (detail is null) return NotFound();

        return View(new BanDocEditViewModel
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
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(BanDocEditViewModel model)
    {
        var detail = await _banDocService.GetAsync(model.MaBanDoc, HttpContext.RequestAborted);
        if (detail is null) return NotFound();
        model.TenDangNhap = detail.TenDangNhap;

        if (!ModelState.IsValid)
            return View(model);

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
            return View(model);
        }

        TempData["ThongBao"] = "Cập nhật hồ sơ bạn đọc thành công.";
        return RedirectToAction(nameof(Index));
    }

    [AuthorizeRole(VaiTro.Admin, VaiTro.ThuThu)]
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var detail = await _banDocService.GetAsync(id, HttpContext.RequestAborted);
        if (detail is null) return NotFound();

        return View(detail);
    }

    [AuthorizeRole(VaiTro.Admin, VaiTro.ThuThu)]
    [HttpPost]
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
