// Họ và tên: Nguyễn Hữu Tuấn
// Mã sinh viên: 23103100136
// Nội dung thực hiện: Controller phía bạn đọc: đăng ký mượn sách, hủy phiếu, theo dõi phiếu mượn và tiền phạt (Module 3). Phần quản lý của thủ thư nằm ở QuanLyPhieuMuonController.

// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: QuanLyPhieuMuonContoller tách từ PhieuMuonController phía bạn đọc

using Microsoft.AspNetCore.Mvc;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Auth;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Common;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;
using QuanLyThuVien_UNETI4_TI17A1CL.ViewModels;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Controllers;

[AuthorizeRole(VaiTro.Admin, VaiTro.ThuThu)]
public class QuanLyPhieuMuonController : Controller
{
    private readonly IPhieuMuonService _phieuMuonService;
    private readonly ITheLoaiService _theLoaiService;
    private readonly ICurrentUserService _current;

    public QuanLyPhieuMuonController(
        IPhieuMuonService phieuMuonService,
        ITheLoaiService theLoaiService,
        ICurrentUserService current)
    {
        _phieuMuonService = phieuMuonService;
        _theLoaiService = theLoaiService;
        _current = current;
    }

    private int ActingId => _current.User!.MaTaiKhoan;

    // Gộp mọi lý do không đạt thành một thông báo để thủ thư thấy đủ.
    private static string AllErrors(ServiceResult r) =>
        string.Join(" ", r.Errors.Values.SelectMany(m => m));

    private IActionResult Back(string? returnUrl) =>
        !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction(nameof(Index));

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        TrangThaiPhieuMuon? trangThai,
        int? maTheLoai,
        bool? overdueOnly,
        int page = 1,
        int pageSize = PagingDefaults.DefaultPageSize)
    {
        var ct = HttpContext.RequestAborted;
        if (trangThai.HasValue && !Enum.IsDefined(trangThai.Value)) trangThai = null;
        search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

        var result = await _phieuMuonService.GetManageListAsync(
            new PhieuMuonManageFilter(search, trangThai, maTheLoai, overdueOnly, page, pageSize), ct);
        var summary = await _phieuMuonService.GetManageSummaryAsync(ct);

        ViewBag.TheLoais = await _theLoaiService.GetActiveOptionsAsync(ct);
        ViewBag.ChuaNhanSach = summary.ChuaNhanSach;

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
            Pager = PagerViewModel.From(result, "QuanLyPhieuMuon", nameof(Index), routeValues),
            ChoDuyetCount = summary.ChoDuyet,
            DaDuyetCount = summary.DaDuyet,
            DangMuonCount = summary.DangMuon,
            OverdueCount = summary.QuaHan
        });
    }

    // Chi tiết dùng chung view với bạn đọc; view tự ẩn các nút chỉ dành cho bạn đọc.
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var detail = await _phieuMuonService.GetLoanDetailAsync(id, null, HttpContext.RequestAborted);
        return detail is null ? NotFound() : View("~/Views/PhieuMuon/Details.cshtml", detail);
    }

    // Phiếu Đã duyệt quá LoanRules.UncollectedDays ngày mà bạn đọc chưa đến nhận (vẫn giữ một bản sách).
    [HttpGet]
    public async Task<IActionResult> ChuaNhanSach()
    {
        var items = await _phieuMuonService.GetUncollectedAsync(HttpContext.RequestAborted);
        return View(items);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Duyet(int id, string? nhanXet, string? returnUrl)
    {
        var result = await _phieuMuonService.ApproveLoanAsync(id, ActingId, nhanXet, HttpContext.RequestAborted);

        if (result.Succeeded)
            TempData["ThongBao"] = $"Đã duyệt phiếu mượn #{id}. Bạn đọc có thể đến quầy nhận sách.";
        else
            TempData["Loi"] = $"Không thể duyệt phiếu #{id}: {AllErrors(result)}";

        return Back(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TuChoi(int id, string? nhanXet, string? returnUrl)
    {
        var result = await _phieuMuonService.RejectLoanAsync(id, ActingId, nhanXet, HttpContext.RequestAborted);

        if (result.Succeeded)
            TempData["ThongBao"] = $"Đã từ chối phiếu mượn #{id}.";
        else
            TempData["Loi"] = AllErrors(result);

        return Back(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GiaoSach(int id, string? returnUrl)
    {
        var result = await _phieuMuonService.DeliverBookAsync(id, ActingId, HttpContext.RequestAborted);

        if (result.Succeeded)
            TempData["ThongBao"] = $"Đã giao sách cho phiếu mượn #{id}. Phiếu chuyển sang trạng thái 'Đang mượn'.";
        else
            TempData["Loi"] = AllErrors(result);

        return Back(returnUrl);
    }
}
