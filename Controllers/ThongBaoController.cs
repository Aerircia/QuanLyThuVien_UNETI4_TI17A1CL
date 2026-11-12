// Họ và tên: Phùng Ngọc Bảo
// Mã sinh viên: 23103100278
// Nội dung thực hiện: Controller thông báo

using Microsoft.AspNetCore.Mvc;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Auth;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Common;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;
using QuanLyThuVien_UNETI4_TI17A1CL.ViewModels;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Controllers;

[AuthorizeRole]
public sealed class ThongBaoController : Controller
{
    private readonly IThongBaoService _thongBao;
    private readonly ICurrentUserService _current;

    public ThongBaoController(IThongBaoService thongBao, ICurrentUserService current)
    {
        _thongBao = thongBao;
        _current = current;
    }
    [HttpGet("/BanDoc/ThongBao")]
    public async Task<IActionResult> Index(string? filter = "all", int page = 1, CancellationToken ct = default)
    {
        var user = _current.User;
        if (user is null) return Unauthorized();

        var allowedFilters = new[] { "all", "unread", "loan", "extension", "fine", "system" };
        filter = allowedFilters.Contains(filter, StringComparer.OrdinalIgnoreCase)
            ? filter!.ToLowerInvariant()
            : "all";

        var result = await _thongBao.GetPageAsync(user.MaTaiKhoan, filter, page, 12, ct);
        var settings = await _thongBao.GetSettingsAsync(user.MaTaiKhoan, ct);
        ViewData["Title"] = "Tất cả thông báo";
        return View(new ThongBaoIndexViewModel
        {
            DanhSach = result.DanhSach,
            BoLoc = filter,
            TongSo = result.TongSo,
            ChuaDoc = result.ChuaDoc,
            TongTheoBoLoc = result.TongTheoBoLoc,
            Trang = result.Trang,
            SoTrang = result.SoTrang,
            CaiDat = settings
        });
    }
    [HttpPost("/ThongBao/SaveSettings")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSettings(ThongBaoSettingsInputModel input, CancellationToken ct = default)
    {
        var user = _current.User;
        if (user is null) return Unauthorized();

        await _thongBao.UpdateSettingsAsync(user.MaTaiKhoan, settings =>
        {
            settings.BatThongBaoWeb = input.BatThongBaoWeb;
            settings.BatThongBaoEmail = input.BatThongBaoEmail;
            settings.NhomMuonTra = input.NhomMuonTra;
            settings.NhomGiaHan = input.NhomGiaHan;
            settings.NhomTienPhat = input.NhomTienPhat;
            settings.NhomHeThong = input.NhomHeThong;
        }, ct);

        return Json(new { success = true, message = "Đã lưu cài đặt thông báo." });
    }
    [HttpGet("/ThongBao/Open/{id:int}")]
    public async Task<IActionResult> Open(int id, CancellationToken ct)
    {
        var user = _current.User;
        if (user is null) return Unauthorized();

        var item = await _thongBao.GetByIdAsync(id, user.MaTaiKhoan, ct);
        if (item is null) return NotFound();

        await _thongBao.MarkReadAsync(id, user.MaTaiKhoan, ct);
        var fallbackUrl = _current.IsInRole(VaiTro.Admin, VaiTro.ThuThu)
            ? Url.Action("Index", "QuanLyPhieuMuon")
            : Url.Action("MyLoans", "PhieuMuon");
        var target = Url.IsLocalUrl(item.DuongDan) ? item.DuongDan : fallbackUrl;
        if (string.IsNullOrWhiteSpace(target))
            return RedirectToAction(nameof(Index));

        return LocalRedirect(target);
    }

    [HttpPost("/ThongBao/MarkAllReadPage")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllReadPage(string? filter = "all", CancellationToken ct = default)
    {
        var user = _current.User;
        if (user is null) return Unauthorized();

        await _thongBao.MarkAllReadAsync(user.MaTaiKhoan, ct);
        TempData["ThongBao"] = "Đã đánh dấu tất cả thông báo là đã đọc.";
        return RedirectToAction(nameof(Index), new { filter });
    }
    [HttpGet("/ThongBao/Bell")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Bell(CancellationToken ct)
    {
        var user = _current.User;
        if (user is null) return Unauthorized();

        var result = await _thongBao.GetBellAsync(user.MaTaiKhoan, 8, ct);
        var fallbackUrl = _current.IsInRole(VaiTro.Admin, VaiTro.ThuThu)
            ? Url.Action("Index", "QuanLyPhieuMuon")
            : Url.Action("MyLoans", "PhieuMuon");
        return Json(new
        {
            chuaDoc = result.ChuaDoc,
            danhSach = result.DanhSach.Select(x => new
            {
                id = x.MaThongBao,
                tieuDe = x.TieuDe,
                noiDung = x.NoiDung,
                loai = x.LoaiThongBao,
                icon = x.Icon,
                duongDan = Url.IsLocalUrl(x.DuongDan) ? x.DuongDan : fallbackUrl,
                openUrl = Url.Action(nameof(Open), new { id = x.MaThongBao }),
                ngayTao = LibraryClock.ToVietnamTime(x.NgayTao),
                daDoc = x.DaDoc
            })
        });
    }
    [HttpPost("/ThongBao/MarkRead/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(int id, CancellationToken ct)
    {
        var user = _current.User;
        if (user is null) return Unauthorized();

        var succeeded = await _thongBao.MarkReadAsync(id, user.MaTaiKhoan, ct);
        var unread = await _thongBao.GetUnreadCountAsync(user.MaTaiKhoan, ct);
        return Json(new { success = succeeded, chuaDoc = unread });
    }
    [HttpPost("/ThongBao/MarkAllRead")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        var user = _current.User;
        if (user is null) return Unauthorized();

        var affected = await _thongBao.MarkAllReadAsync(user.MaTaiKhoan, ct);
        return Json(new { success = true, soLuong = affected, chuaDoc = 0 });
    }
}
