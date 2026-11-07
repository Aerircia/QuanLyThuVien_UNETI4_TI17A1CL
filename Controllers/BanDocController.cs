// Họ và tên: Nguyễn Hữu Tuấn
// Mã sinh viên: 23103100136
// Nội dung thực hiện: Controller xử lý quản lý bạn đọc và hồ sơ cá nhân của bạn đọc (Module 3)

using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien_UNETI4_TI17A1CL.Data;
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
    private readonly AppDbContext _db;

    public BanDocController(IBanDocService banDocService, ICurrentUserService current, AppDbContext db)
    {
        _banDocService = banDocService;
        _current = current;
        _db = db;
    }

    // 1. Dành cho BẠN ĐỌC: Xem và cập nhật thông tin cá nhân của chính mình
    [AuthorizeRole(VaiTro.BanDoc)]
    [HttpGet]
    public async Task<IActionResult> MyProfile()
    {
        var user = _current.User!;
        var detail = await _banDocService.GetByTaiKhoanAsync(user.MaTaiKhoan, HttpContext.RequestAborted);

        if (detail is null)
        {
            // Tài khoản chưa có hồ sơ bạn đọc: mở form đăng ký thẻ bạn đọc
            var emptyModel = new BanDocProfileViewModel
            {
                HoTen = user.HoTen,
                TenDangNhap = user.TenDangNhap,
                Email = user.Email,
                DangKyForm = new DangKyTheBanDocViewModel
                {
                    HoTen = user.HoTen,
                    Email = user.Email
                }
            };
            return View(emptyModel);
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
            TongTienPhatChuaNop = detail.TongTienPhatChuaNop,
            DangKyForm = detail.TrangThai == TrangThaiBanDoc.TuChoi ? new DangKyTheBanDocViewModel
            {
                HoTen = detail.HoTen,
                NgaySinh = detail.NgaySinh,
                GioiTinh = detail.GioiTinh,
                SoDienThoai = detail.SoDienThoai ?? string.Empty,
                Email = detail.Email ?? string.Empty,
                DiaChi = detail.DiaChi ?? string.Empty,
                LoaiBanDoc = detail.LoaiBanDoc
            } : null
        };

        return View(model);
    }

    [AuthorizeRole(VaiTro.BanDoc)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DangKyThe(DangKyTheBanDocViewModel? model)
    {
        var user = _current.User!;
        var form = model ?? new DangKyTheBanDocViewModel();

        // Đảm bảo không bị thiếu trường do đặt tên form
        var hoTen = Request.Form["HoTen"].FirstOrDefault() ?? Request.Form["DangKyForm.HoTen"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(hoTen)) form.HoTen = hoTen.Trim();

        var email = Request.Form["Email"].FirstOrDefault() ?? Request.Form["DangKyForm.Email"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(email)) form.Email = email.Trim();

        var soDienThoai = Request.Form["SoDienThoai"].FirstOrDefault() ?? Request.Form["DangKyForm.SoDienThoai"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(soDienThoai))
        {
            form.SoDienThoai = Regex.Replace(soDienThoai.Trim(), @"[\s\.\-]+", "");
        }

        var diaChi = Request.Form["DiaChi"].FirstOrDefault() ?? Request.Form["DangKyForm.DiaChi"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(diaChi)) form.DiaChi = diaChi.Trim();

        var ngaySinhStr = Request.Form["NgaySinh"].FirstOrDefault() ?? Request.Form["DangKyForm.NgaySinh"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(ngaySinhStr))
        {
            if (DateOnly.TryParse(ngaySinhStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var ns) ||
                DateOnly.TryParse(ngaySinhStr, out ns))
            {
                form.NgaySinh = ns;
            }
        }

        var gioiTinhStr = Request.Form["GioiTinh"].FirstOrDefault() ?? Request.Form["DangKyForm.GioiTinh"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(gioiTinhStr) && Enum.TryParse<GioiTinh>(gioiTinhStr, out var gt))
            form.GioiTinh = gt;

        var loaiBanDocStr = Request.Form["LoaiBanDoc"].FirstOrDefault() ?? Request.Form["DangKyForm.LoaiBanDoc"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(loaiBanDocStr) && Enum.TryParse<LoaiBanDoc>(loaiBanDocStr, out var lbd))
            form.LoaiBanDoc = lbd;

        // Tự động điền họ tên / email từ tài khoản người dùng nếu để trống
        if (string.IsNullOrWhiteSpace(form.HoTen) && !string.IsNullOrWhiteSpace(user.HoTen))
            form.HoTen = user.HoTen.Trim();
        if (string.IsNullOrWhiteSpace(form.Email) && !string.IsNullOrWhiteSpace(user.Email))
            form.Email = user.Email.Trim();

        ModelState.Clear();
        TryValidateModel(form);

        if (!ModelState.IsValid)
        {
            var firstErr = ModelState.Values.SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault(m => !string.IsNullOrEmpty(m)) ?? "Thông tin đăng ký thẻ chưa hợp lệ. Vui lòng kiểm tra lại.";
            TempData["Loi"] = firstErr;

            // Đồng bộ lỗi sang tiền tố DangKyForm.<tên trường> để các thẻ span asp-validation-for="DangKyForm!..." hiển thị chính xác
            foreach (var key in ModelState.Keys.ToList())
            {
                if (!key.StartsWith("DangKyForm.") && !string.IsNullOrEmpty(key))
                {
                    var errors = ModelState[key]?.Errors.ToList();
                    if (errors != null)
                    {
                        foreach (var err in errors)
                        {
                            ModelState.AddModelError($"DangKyForm.{key}", err.ErrorMessage);
                        }
                    }
                }
            }

            var profileModel = new BanDocProfileViewModel
            {
                HoTen = user.HoTen,
                TenDangNhap = user.TenDangNhap,
                Email = user.Email,
                DangKyForm = form
            };
            return View(nameof(MyProfile), profileModel);
        }

        var input = new DangKyTheBanDocInput(
            form.HoTen,
            form.NgaySinh,
            form.GioiTinh,
            form.SoDienThoai,
            form.Email,
            form.DiaChi,
            form.LoaiBanDoc);

        var result = await _banDocService.DangKyTheAsync(input, user.MaTaiKhoan, HttpContext.RequestAborted);
        if (!result.Succeeded)
        {
            TempData["Loi"] = result.FirstError;
            result.AddTo(ModelState);

            foreach (var (k, msgs) in result.Errors)
            {
                foreach (var m in msgs)
                {
                    if (!string.IsNullOrEmpty(k))
                        ModelState.AddModelError($"DangKyForm.{k}", m);
                    else
                        ModelState.AddModelError(string.Empty, m);
                }
            }

            var profileModel = new BanDocProfileViewModel
            {
                HoTen = user.HoTen,
                TenDangNhap = user.TenDangNhap,
                Email = user.Email,
                DangKyForm = form
            };
            return View(nameof(MyProfile), profileModel);
        }

        TempData["ThongBao"] = "Đăng ký thẻ bạn đọc thành công! Hồ sơ của bạn đã được gửi và đang chờ thủ thư duyệt thẻ.";
        return RedirectToAction(nameof(MyProfile));
    }

    [AuthorizeRole(VaiTro.BanDoc)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HuyDangKyThe()
    {
        var user = _current.User!;
        var result = await _banDocService.HuyDangKyTheAsync(user.MaTaiKhoan, HttpContext.RequestAborted);
        if (result.Succeeded)
            TempData["ThongBao"] = "Đã hủy yêu cầu đăng ký thẻ bạn đọc.";
        else
            TempData["Loi"] = result.FirstError;

        return RedirectToAction(nameof(MyProfile));
    }

    [AuthorizeRole(VaiTro.BanDoc)]
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

    [AuthorizeRole(VaiTro.BanDoc)]
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

        var tongSo = await _db.BanDocs.CountAsync(HttpContext.RequestAborted);
        var choDuyet = await _db.BanDocs.CountAsync(b => b.TrangThai == TrangThaiBanDoc.ChoDuyet, HttpContext.RequestAborted);
        var hoatDong = await _db.BanDocs.CountAsync(b => b.TrangThai == TrangThaiBanDoc.HoatDong, HttpContext.RequestAborted);
        var ngungHoatDong = await _db.BanDocs.CountAsync(b => b.TrangThai == TrangThaiBanDoc.NgungHoatDong || b.TrangThai == TrangThaiBanDoc.TuChoi, HttpContext.RequestAborted);

        return View(new BanDocListViewModel
        {
            Search = search,
            LoaiBanDoc = loaiBanDoc,
            TrangThai = trangThai,
            PageSize = result.PageSize,
            Result = result,
            Pager = PagerViewModel.From(result, "BanDoc", nameof(Index), routeValues),
            TongSoBanDoc = tongSo,
            SoBanDocChoDuyet = choDuyet,
            SoBanDocHoatDong = hoatDong,
            SoBanDocNgungHoatDong = ngungHoatDong
        });
    }

    [AuthorizeRole(VaiTro.Admin, VaiTro.ThuThu)]
    [HttpGet]
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

        var tongSo = await _db.BanDocs.CountAsync(HttpContext.RequestAborted);
        var choDuyet = await _db.BanDocs.CountAsync(b => b.TrangThai == TrangThaiBanDoc.ChoDuyet, HttpContext.RequestAborted);
        var hoatDong = await _db.BanDocs.CountAsync(b => b.TrangThai == TrangThaiBanDoc.HoatDong, HttpContext.RequestAborted);
        var ngungHoatDong = await _db.BanDocs.CountAsync(b => b.TrangThai == TrangThaiBanDoc.NgungHoatDong || b.TrangThai == TrangThaiBanDoc.TuChoi, HttpContext.RequestAborted);

        return View(new BanDocListViewModel
        {
            Search = search,
            LoaiBanDoc = null,
            TrangThai = TrangThaiBanDoc.ChoDuyet,
            PageSize = result.PageSize,
            Result = result,
            Pager = PagerViewModel.From(result, "BanDoc", nameof(DuyetThe), routeValues),
            TongSoBanDoc = tongSo,
            SoBanDocChoDuyet = choDuyet,
            SoBanDocHoatDong = hoatDong,
            SoBanDocNgungHoatDong = ngungHoatDong
        });
    }

    [AuthorizeRole(VaiTro.Admin, VaiTro.ThuThu)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Duyet(int id, string? returnUrl)
    {
        var result = await _banDocService.DuyetTheAsync(id, HttpContext.RequestAborted);
        if (result.Succeeded)
            TempData["ThongBao"] = "Đã phê duyệt và kích hoạt thẻ bạn đọc thành công.";
        else
            TempData["Loi"] = result.FirstError;

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return RedirectToAction(nameof(DuyetThe));
    }

    [AuthorizeRole(VaiTro.Admin, VaiTro.ThuThu)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TuChoi(int id, string? returnUrl)
    {
        var result = await _banDocService.TuChoiTheAsync(id, null, HttpContext.RequestAborted);
        if (result.Succeeded)
            TempData["ThongBao"] = "Đã từ chối yêu cầu đăng ký thẻ bạn đọc.";
        else
            TempData["Loi"] = result.FirstError;

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return RedirectToAction(nameof(DuyetThe));
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
