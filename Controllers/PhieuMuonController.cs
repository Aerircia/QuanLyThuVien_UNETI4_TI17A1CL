// Họ và tên: Nguyễn Hữu Tuấn
// Mã sinh viên: 23103100136
// Nội dung thực hiện: Controller phía bạn đọc: đăng ký mượn sách, hủy phiếu, theo dõi phiếu mượn và tiền phạt (Module 3). Phần quản lý của thủ thư nằm ở QuanLyPhieuMuonController.

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien_UNETI4_TI17A1CL.Data;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Auth;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Common;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;
using QuanLyThuVien_UNETI4_TI17A1CL.ViewModels;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Controllers;

public class PhieuMuonController : Controller
{
    private readonly IPhieuMuonService _phieuMuonService;
    private readonly IThanhToanService _thanhToanService;
    private readonly ICurrentUserService _current;
    private readonly AppDbContext _db;

    public PhieuMuonController(IPhieuMuonService phieuMuonService, IThanhToanService thanhToanService, ICurrentUserService current, AppDbContext db)
    {
        _phieuMuonService = phieuMuonService;
        _thanhToanService = thanhToanService;
        _current = current;
        _db = db;
    }

    // 7.3. ĐĂNG KÝ MƯỢN SÁCH (Dành cho bạn đọc)
    [AuthorizeRole(VaiTro.BanDoc)]
    [HttpGet]
    public async Task<IActionResult> Register(int id)
    {
        var maSach = id;
        var user = _current.User!;
        var banDoc = await _db.BanDocs
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.MaTaiKhoan == user.MaTaiKhoan, HttpContext.RequestAborted);

        if (banDoc is null)
        {
            TempData["ThongBao"] = "Bạn chưa có thẻ bạn đọc. Vui lòng hoàn tất đăng ký thẻ trước khi mượn sách.";
            return RedirectToAction("MyProfile", "BanDoc");
        }

        if (banDoc.TrangThai == TrangThaiBanDoc.ChoDuyet)
        {
            TempData["ThongBao"] = "Thẻ bạn đọc của bạn đang chờ thủ thư duyệt thẻ. Vui lòng chờ thẻ được kích hoạt trước khi đăng ký mượn sách.";
            return RedirectToAction("MyProfile", "BanDoc");
        }

        if (banDoc.TrangThai != TrangThaiBanDoc.HoatDong)
        {
            TempData["Loi"] = "Thẻ bạn đọc của bạn hiện không hoạt động. Vui lòng kiểm tra lại thông tin hồ sơ.";
            return RedirectToAction("MyProfile", "BanDoc");
        }

        var sach = await _db.Sachs
            .Include(s => s.TheLoai)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.MaSach == maSach, HttpContext.RequestAborted);

        if (sach is null)
            return NotFound("Không tìm thấy đầu sách.");

        var cauHinh = await _db.CauHinhThuViens.FirstOrDefaultAsync(HttpContext.RequestAborted) ?? new Models.CauHinhThuVien();

        var check = await _phieuMuonService.CheckCanBorrowAsync(banDoc.MaBanDoc, maSach, HttpContext.RequestAborted);

        var soPhieuDangXuLy = await _db.PhieuMuons.CountAsync(
            p => p.MaBanDoc == banDoc.MaBanDoc &&
                (p.TrangThai == TrangThaiPhieuMuon.ChoDuyet ||
                 p.TrangThai == TrangThaiPhieuMuon.DaDuyet ||
                 p.TrangThai == TrangThaiPhieuMuon.DangMuon),
            HttpContext.RequestAborted);

        var thongTinHopLe = !string.IsNullOrWhiteSpace(banDoc.HoTen)
                         && !string.IsNullOrWhiteSpace(banDoc.SoDienThoai)
                         && !string.IsNullOrWhiteSpace(banDoc.Email)
                         && !string.IsNullOrWhiteSpace(banDoc.DiaChi);

        var model = new PhieuMuonRegisterViewModel
        {
            MaSach = sach.MaSach,
            TenSach = sach.TenSach,
            TacGia = sach.TacGia,
            TheLoai = sach.TheLoai?.TenTheLoai,
            NamXuatBan = sach.NamXuatBan,
            GiaBia = sach.GiaBia,
            ViTriKe = sach.ViTriKe,
            NhaXuatBan = sach.NhaXuatBan,
            MoTa = sach.MoTa,
            ImgPath = sach.ImgPath,
            SoLuongTong = sach.SoLuongTong,
            SoLuongConLai = check.SoLuongConLai,

            MaBanDoc = banDoc.MaBanDoc,
            HoTenBanDoc = banDoc.HoTen,
            MaSoThe = banDoc.MaSoThe,
            LoaiBanDoc = banDoc.LoaiBanDoc,
            NgayHetHanThe = banDoc.NgayHetHanThe,
            SoPhieuDangXuLy = soPhieuDangXuLy,
            SoPhieuToiDa = cauHinh.SoPhieuToiDa,
            ThongTinBatBuocHopLe = thongTinHopLe,

            DuDieuKienMuon = check.CanBorrow,
            LyDoKhongDuDieuKien = check.Errors
        };

        return View(model);
    }

    [AuthorizeRole(VaiTro.BanDoc)]
    [HttpPost]
    [ActionName("Register")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterPost(int id, PhieuMuonRegisterViewModel model)
    {
        var maSach = id;
        var user = _current.User!;
        var banDoc = await _db.BanDocs
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.MaTaiKhoan == user.MaTaiKhoan, HttpContext.RequestAborted);

        if (banDoc is null || banDoc.TrangThai != TrangThaiBanDoc.HoatDong)
        {
            TempData["Loi"] = "Bạn cần có thẻ bạn đọc đang hoạt động để mượn sách.";
            return RedirectToAction("MyProfile", "BanDoc");
        }

        var result = await _phieuMuonService.RegisterLoanAsync(banDoc.MaBanDoc, maSach, model.GhiChu, HttpContext.RequestAborted);

        if (!result.Succeeded)
        {
            // Nạp lại thông tin để hiển thị lỗi
            var sach = await _db.Sachs.Include(s => s.TheLoai).AsNoTracking().FirstOrDefaultAsync(s => s.MaSach == maSach, HttpContext.RequestAborted);
            var cauHinh = await _db.CauHinhThuViens.FirstOrDefaultAsync(HttpContext.RequestAborted) ?? new Models.CauHinhThuVien();
            var check = await _phieuMuonService.CheckCanBorrowAsync(banDoc.MaBanDoc, maSach, HttpContext.RequestAborted);

            model.MaSach = sach?.MaSach ?? maSach;
            model.TenSach = sach?.TenSach ?? string.Empty;
            model.TacGia = sach?.TacGia ?? string.Empty;
            model.TheLoai = sach?.TheLoai?.TenTheLoai;
            model.NamXuatBan = sach?.NamXuatBan ?? 0;
            model.GiaBia = sach?.GiaBia ?? 0;
            model.ViTriKe = sach?.ViTriKe;
            model.ImgPath = sach?.ImgPath;
            model.SoLuongTong = sach?.SoLuongTong ?? 0;
            model.SoLuongConLai = check.SoLuongConLai;
            model.HoTenBanDoc = banDoc.HoTen;
            model.MaSoThe = banDoc.MaSoThe;
            model.LoaiBanDoc = banDoc.LoaiBanDoc;
            model.NgayHetHanThe = banDoc.NgayHetHanThe;
            model.SoPhieuToiDa = cauHinh.SoPhieuToiDa;
            model.DuDieuKienMuon = false;
            model.LyDoKhongDuDieuKien = result.Errors.Values.SelectMany(x => x).ToList();

            result.AddTo(ModelState);
            return View(model);
        }

        return RedirectToAction(nameof(RegisterSuccess), new { id = result.Value });
    }

    [AuthorizeRole(VaiTro.BanDoc)]
    [HttpGet]
    public async Task<IActionResult> RegisterSuccess(int id)
    {
        var user = _current.User!;
        var banDoc = await _db.BanDocs.AsNoTracking()
            .FirstOrDefaultAsync(b => b.MaTaiKhoan == user.MaTaiKhoan, HttpContext.RequestAborted);
        if (banDoc is null)
            return NotFound();

        var phieu = await _db.PhieuMuons
            .AsNoTracking()
            .Include(p => p.Sach)
            .FirstOrDefaultAsync(p => p.MaPhieuMuon == id && p.MaBanDoc == banDoc.MaBanDoc, HttpContext.RequestAborted);
        if (phieu is null)
            return NotFound();

        return View(new PhieuMuonRegisterSuccessViewModel
        {
            MaPhieuMuon = phieu.MaPhieuMuon,
            TenSach = phieu.Sach?.TenSach ?? string.Empty,
            ViTriKe = phieu.Sach?.ViTriKe
        });
    }

    // 7.5. THEO DÕI PHIẾU MƯỢN CỦA BẠN ĐỌC (cần thẻ đang hoạt động)
    [AuthorizeRole(VaiTro.BanDoc)]
    [RequireActiveCard]
    [HttpGet]
    public async Task<IActionResult> MyLoans(string? filter = "all")
    {
        var ct = HttpContext.RequestAborted;
        var maBanDoc = await _current.GetMaBanDocAsync(ct);
        if (maBanDoc is null)
            return RedirectToAction("MyProfile", "BanDoc");

        var allLoans = await _phieuMuonService.GetMyLoansAsync(maBanDoc.Value, "all", ct);
        var loans = (filter == "all" || string.IsNullOrWhiteSpace(filter))
            ? allLoans
            : await _phieuMuonService.GetMyLoansAsync(maBanDoc.Value, filter, ct);

        var fines = await _phieuMuonService.GetMyFinesAsync(maBanDoc.Value, ct);

        var model = new MyLoansViewModel
        {
            Loans = loans,
            Filter = filter ?? "all",
            TongSoPhieu = allLoans.Count,
            SoPhieuChoDuyet = allLoans.Count(l => l.TrangThai == TrangThaiPhieuMuon.ChoDuyet),
            SoPhieuDaDuyet = allLoans.Count(l => l.TrangThai == TrangThaiPhieuMuon.DaDuyet),
            SoPhieuDangMuon = allLoans.Count(l => l.TrangThai == TrangThaiPhieuMuon.DangMuon),
            SoPhieuQuaHan = allLoans.Count(l => l.IsOverdue),
            SoPhieuDaTra = allLoans.Count(l => l.TrangThai == TrangThaiPhieuMuon.DaTra),
            SoPhieuKetThuc = allLoans.Count(l => l.TrangThai is TrangThaiPhieuMuon.DaTra
                or TrangThaiPhieuMuon.DaHuy or TrangThaiPhieuMuon.TuChoi),
            TongTienPhatChuaNop = fines.ChuaNop
        };

        var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Query.ContainsKey("ajax");
        if (isAjax)
            return PartialView("_MyLoansListPartial", model);

        return View(model);
    }

    // 7.5. CHI TIẾT PHIẾU MƯỢN (bạn đọc xem phiếu của chính mình; thủ thư được chuyển sang trang quản lý)
    [AuthorizeRole]
    [RequireActiveCard]
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        if (_current.IsInRole(VaiTro.Admin, VaiTro.ThuThu))
            return RedirectToAction("Details", "QuanLyPhieuMuon", new { id });

        var maBanDoc = await _current.GetMaBanDocAsync(HttpContext.RequestAborted);
        if (!maBanDoc.HasValue)
            return NotFound();

        // Bắt buộc phiếu thuộc về bạn đọc này: đổi mã trên URL không làm lộ dữ liệu người khác.
        var detail = await _phieuMuonService.GetLoanDetailAsync(id, maBanDoc, HttpContext.RequestAborted);
        return detail is null ? NotFound("Không tìm thấy phiếu mượn hoặc bạn không có quyền xem phiếu mượn này.") : View(detail);
    }

    // 7.4. HỦY PHIẾU MƯỢN (POST only)
    [AuthorizeRole(VaiTro.BanDoc)]
    [RequireActiveCard]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string? returnUrl = null)
    {
        var maBanDoc = await _current.GetMaBanDocAsync(HttpContext.RequestAborted);
        var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        if (!maBanDoc.HasValue)
        {
            if (isAjax) return Json(new { success = false, message = "Bạn chưa có hồ sơ bạn đọc." });
            return RedirectToAction("MyProfile", "BanDoc");
        }

        var result = await _phieuMuonService.CancelLoanAsync(id, maBanDoc.Value, HttpContext.RequestAborted);

        if (result.Succeeded)
        {
            var msg = "Đã hủy phiếu mượn thành công.";
            if (isAjax) return Json(new { success = true, message = msg });
            TempData["ThongBao"] = msg;
        }
        else
        {
            var msg = result.FirstError;
            if (isAjax) return Json(new { success = false, message = msg });
            TempData["Loi"] = msg;
        }

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return RedirectToAction(nameof(MyLoans));
    }

    // 7.5. TIỀN PHẠT CỦA BẠN ĐỌC (cần thẻ đang hoạt động)
    [AuthorizeRole(VaiTro.BanDoc)]
    [RequireActiveCard]
    [HttpGet]
    public async Task<IActionResult> MyFines()
    {
        var maBanDoc = await _current.GetMaBanDocAsync(HttpContext.RequestAborted);
        if (maBanDoc is null)
        {
            TempData["ThongBao"] = "Bạn chưa có thẻ thư viện đang hoạt động.";
            return RedirectToAction("MyProfile", "BanDoc");
        }

        var model = await _phieuMuonService.GetMyFinesAsync(maBanDoc.Value, HttpContext.RequestAborted);
        ViewBag.PendingFineIds = await _thanhToanService.GetPendingFineIdsAsync(maBanDoc.Value, HttpContext.RequestAborted);
        ViewBag.PayEnabled = _thanhToanService.IsOnlineEnabled;

        return View(model);
    }
}
