// Họ và tên: Nguyễn Hữu Tuấn
// Mã sinh viên: 23103100136
// Nội dung thực hiện: Controller xử lý quy trình đăng ký mượn sách, hủy phiếu, theo dõi phiếu mượn và duyệt mượn sách (Module 3)

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
    private readonly ICurrentUserService _current;
    private readonly AppDbContext _db;

    public PhieuMuonController(IPhieuMuonService phieuMuonService, ICurrentUserService current, AppDbContext db)
    {
        _phieuMuonService = phieuMuonService;
        _current = current;
        _db = db;
    }
    // 7.3. ĐĂNG KÝ MƯỢN SÁCH (Dành cho bạn đọc)
    [AuthorizeRole(VaiTro.BanDoc)]
    [HttpGet]
    public async Task<IActionResult> Register(int id)
    {
        // Parameter id = maSach
        var maSach = id;
        var maBanDoc = await _current.GetMaBanDocAsync(HttpContext.RequestAborted);
        if (!maBanDoc.HasValue)
        {
            TempData["Loi"] = "Bạn chưa có hồ sơ bạn đọc hoặc thẻ thư viện. Vui lòng liên hệ thủ thư.";
            return RedirectToAction("Index", "Home");
        }

        var sach = await _db.Sachs
            .Include(s => s.TheLoai)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.MaSach == maSach, HttpContext.RequestAborted);

        if (sach is null)
            return NotFound("Không tìm thấy đầu sách.");

        var banDoc = await _db.BanDocs
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.MaBanDoc == maBanDoc.Value, HttpContext.RequestAborted);

        if (banDoc is null)
            return NotFound("Không tìm thấy hồ sơ bạn đọc.");

        var cauHinh = await _db.CauHinhThuViens.FirstOrDefaultAsync(HttpContext.RequestAborted) ?? new Models.CauHinhThuVien();

        var check = await _phieuMuonService.CheckCanBorrowAsync(maBanDoc.Value, maSach, HttpContext.RequestAborted);

        var soPhieuDangXuLy = await _db.PhieuMuons.CountAsync(
            p => p.MaBanDoc == maBanDoc.Value &&
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
        var maBanDoc = await _current.GetMaBanDocAsync(HttpContext.RequestAborted);
        if (!maBanDoc.HasValue)
            return Forbid();

        var result = await _phieuMuonService.RegisterLoanAsync(maBanDoc.Value, maSach, model.GhiChu, HttpContext.RequestAborted);

        if (!result.Succeeded)
        {
            // Nạp lại thông tin để hiển thị lỗi
            var sach = await _db.Sachs.Include(s => s.TheLoai).AsNoTracking().FirstOrDefaultAsync(s => s.MaSach == maSach, HttpContext.RequestAborted);
            var banDoc = await _db.BanDocs.AsNoTracking().FirstOrDefaultAsync(b => b.MaBanDoc == maBanDoc.Value, HttpContext.RequestAborted);
            var cauHinh = await _db.CauHinhThuViens.FirstOrDefaultAsync(HttpContext.RequestAborted) ?? new Models.CauHinhThuVien();
            var check = await _phieuMuonService.CheckCanBorrowAsync(maBanDoc.Value, maSach, HttpContext.RequestAborted);

            model.MaSach = sach?.MaSach ?? maSach;
            model.TenSach = sach?.TenSach ?? string.Empty;
            model.TacGia = sach?.TacGia ?? string.Empty;
            model.TheLoai = sach?.TheLoai?.TenTheLoai;
            model.NamXuatBan = sach?.NamXuatBan ?? 0;
            model.GiaBia = sach?.GiaBia ?? 0;
            model.ViTriKe = sach?.ViTriKe;
            model.SoLuongTong = sach?.SoLuongTong ?? 0;
            model.SoLuongConLai = check.SoLuongConLai;
            model.HoTenBanDoc = banDoc?.HoTen ?? string.Empty;
            model.MaSoThe = banDoc?.MaSoThe;
            model.LoaiBanDoc = banDoc?.LoaiBanDoc ?? LoaiBanDoc.SinhVien;
            model.NgayHetHanThe = banDoc?.NgayHetHanThe ?? DateOnly.MinValue;
            model.SoPhieuToiDa = cauHinh.SoPhieuToiDa;
            model.DuDieuKienMuon = false;
            model.LyDoKhongDuDieuKien = result.Errors.Values.SelectMany(x => x).ToList();

            result.AddTo(ModelState);
            return View(model);
        }

        TempData["ThongBao"] = "Đăng ký mượn sách thành công! Phiếu mượn đang ở trạng thái 'Chờ duyệt', vui lòng theo dõi thông báo từ thủ thư.";
        return RedirectToAction(nameof(MyLoans));
    }
    // 7.5. THEO DÕI PHIẾU MƯỢN CỦA BẠN ĐỌC
    [AuthorizeRole(VaiTro.BanDoc)]
    [HttpGet]
    public async Task<IActionResult> MyLoans(string? filter = "all")
    {
        var maBanDoc = await _current.GetMaBanDocAsync(HttpContext.RequestAborted);
        if (!maBanDoc.HasValue)
        {
            TempData["Loi"] = "Bạn chưa có hồ sơ bạn đọc.";
            return RedirectToAction("Index", "Home");
        }

        var loans = await _phieuMuonService.GetMyLoansAsync(maBanDoc.Value, filter, HttpContext.RequestAborted);

        var fines = await _phieuMuonService.GetMyFinesAsync(maBanDoc.Value, HttpContext.RequestAborted);

        var model = new MyLoansViewModel
        {
            Loans = loans,
            Filter = filter ?? "all",
            TongTienPhatChuaNop = fines.ChuaNop
        };

        return View(model);
    }

    // 7.5. CHI TIẾT PHIẾU MƯỢN
    [AuthorizeRole]
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        int? requiredMaBanDoc = null;
        if (_current.IsInRole(VaiTro.BanDoc))
        {
            requiredMaBanDoc = await _current.GetMaBanDocAsync(HttpContext.RequestAborted);
            if (!requiredMaBanDoc.HasValue)
                return NotFound();
        }

        // Với Bạn đọc: bắt buộc phiếu phải thuộc về requiredMaBanDoc
        // Với Thủ thư / Admin: requiredMaBanDoc = null -> xem được chi tiết phiếu
        var detail = await _phieuMuonService.GetLoanDetailAsync(id, requiredMaBanDoc, HttpContext.RequestAborted);
        if (detail is null)
            return NotFound("Không tìm thấy phiếu mượn hoặc bạn không có quyền xem phiếu mượn này.");

        return View(detail);
    }
    // 7.4. HỦY PHIẾU MƯỢN (POST only)
    [AuthorizeRole(VaiTro.BanDoc)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string? returnUrl = null)
    {
        var maBanDoc = await _current.GetMaBanDocAsync(HttpContext.RequestAborted);
        if (!maBanDoc.HasValue)
            return Forbid();

        var result = await _phieuMuonService.CancelLoanAsync(id, maBanDoc.Value, HttpContext.RequestAborted);

        if (result.Succeeded)
            TempData["ThongBao"] = "Đã hủy phiếu mượn thành công.";
        else
            TempData["Loi"] = result.FirstError;

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return RedirectToAction(nameof(MyLoans));
    }
    // 7.5. TIỀN PHẠT CỦA BẠN ĐỌC
    [AuthorizeRole(VaiTro.BanDoc)]
    [HttpGet]
    public async Task<IActionResult> MyFines()
    {
        var maBanDoc = await _current.GetMaBanDocAsync(HttpContext.RequestAborted);
        if (!maBanDoc.HasValue)
            return NotFound();

        var model = await _phieuMuonService.GetMyFinesAsync(maBanDoc.Value, HttpContext.RequestAborted);
        return View(model);
    }
    // DÀNH CHO THỦ THƯ & ADMIN: Xử lý & Xác nhận mượn sách
    [AuthorizeRole(VaiTro.Admin, VaiTro.ThuThu)]
    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        TrangThaiPhieuMuon? trangThai,
        int? maTheLoai,
        bool? overdueOnly,
        int page = 1,
        int pageSize = PagingDefaults.DefaultPageSize)
    {
        if (trangThai.HasValue && !Enum.IsDefined(trangThai.Value)) trangThai = null;
        search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

        var filter = new PhieuMuonManageFilter(search, trangThai, maTheLoai, overdueOnly, page, pageSize);
        var result = await _phieuMuonService.GetManageListAsync(filter, HttpContext.RequestAborted);

        var today = DateOnly.FromDateTime(DateTime.Today);
        var choDuyetCount = await _db.PhieuMuons.CountAsync(p => p.TrangThai == TrangThaiPhieuMuon.ChoDuyet, HttpContext.RequestAborted);
        var daDuyetCount = await _db.PhieuMuons.CountAsync(p => p.TrangThai == TrangThaiPhieuMuon.DaDuyet, HttpContext.RequestAborted);
        var dangMuonCount = await _db.PhieuMuons.CountAsync(p => p.TrangThai == TrangThaiPhieuMuon.DangMuon, HttpContext.RequestAborted);
        var overdueCount = await _db.PhieuMuons.CountAsync(p => p.TrangThai == TrangThaiPhieuMuon.DangMuon && p.HanTra.HasValue && p.HanTra.Value < today, HttpContext.RequestAborted);

        ViewBag.TheLoais = await _db.TheLoais.AsNoTracking().OrderBy(t => t.TenTheLoai).ToListAsync(HttpContext.RequestAborted);

        var routeValues = new Dictionary<string, string>();
        if (search is not null) routeValues["search"] = search;
        if (trangThai.HasValue) routeValues["trangThai"] = trangThai.Value.ToString();
        if (maTheLoai.HasValue) routeValues["maTheLoai"] = maTheLoai.Value.ToString();
        if (overdueOnly == true) routeValues["overdueOnly"] = "true";

        return View(new PhieuMuonManageListViewModel
        {
            Search = search,
            TrangThai = trangThai,
            MaTheLoai = maTheLoai,
            OverdueOnly = overdueOnly,
            PageSize = result.PageSize,
            Result = result,
            Pager = PagerViewModel.From(result, "PhieuMuon", nameof(Index), routeValues),
            ChoDuyetCount = choDuyetCount,
            DaDuyetCount = daDuyetCount,
            DangMuonCount = dangMuonCount,
            OverdueCount = overdueCount
        });
    }

    [AuthorizeRole(VaiTro.Admin, VaiTro.ThuThu)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Duyet(int id, string? nhanXet, string? returnUrl)
    {
        var actingId = _current.User!.MaTaiKhoan;
        var result = await _phieuMuonService.ApproveLoanAsync(id, actingId, nhanXet, HttpContext.RequestAborted);

        if (result.Succeeded)
            TempData["ThongBao"] = $"Đã duyệt phiếu mượn #{id}. Bạn đọc có thể đến quầy nhận sách.";
        else
            TempData["Loi"] = result.FirstError;

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return RedirectToAction(nameof(Index));
    }

    [AuthorizeRole(VaiTro.Admin, VaiTro.ThuThu)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TuChoi(int id, string? nhanXet, string? returnUrl)
    {
        var actingId = _current.User!.MaTaiKhoan;
        var result = await _phieuMuonService.RejectLoanAsync(id, actingId, nhanXet, HttpContext.RequestAborted);

        if (result.Succeeded)
            TempData["ThongBao"] = $"Đã từ chối phiếu mượn #{id}.";
        else
            TempData["Loi"] = result.FirstError;

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return RedirectToAction(nameof(Index));
    }

    [AuthorizeRole(VaiTro.Admin, VaiTro.ThuThu)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GiaoSach(int id, string? returnUrl)
    {
        var actingId = _current.User!.MaTaiKhoan;
        var result = await _phieuMuonService.DeliverBookAsync(id, actingId, HttpContext.RequestAborted);

        if (result.Succeeded)
            TempData["ThongBao"] = $"Đã giao sách cho phiếu mượn #{id}. Phiếu chuyển sang trạng thái 'Đang mượn'.";
        else
            TempData["Loi"] = result.FirstError;

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return RedirectToAction(nameof(Index));
    }
}
