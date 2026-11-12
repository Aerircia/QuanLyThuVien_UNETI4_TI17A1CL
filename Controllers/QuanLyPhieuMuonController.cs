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
    [HttpGet("/ThuThu/QuanLyPhieuMuon")]
    public async Task<IActionResult> Index(
        string? search,
        string? groupFilter,
        TrangThaiPhieuMuon? trangThai,
        int? maTheLoai,
        bool? overdueOnly,
        int page = 1,
        int pageSize = PagingDefaults.DefaultPageSize)
    {
        var ct = HttpContext.RequestAborted;
        await _phieuMuonService.AutoCancelOverdueApprovedLoansAsync(ct);

        if (trangThai.HasValue && !Enum.IsDefined(trangThai.Value)) trangThai = null;
        search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        groupFilter = string.IsNullOrWhiteSpace(groupFilter) ? null : groupFilter.Trim().ToLowerInvariant();

        var result = await _phieuMuonService.GetManageListAsync(
            new PhieuMuonManageFilter(search, trangThai, maTheLoai, overdueOnly, page, pageSize, groupFilter), ct);
        var summary = await _phieuMuonService.GetManageSummaryAsync(ct);

        ViewBag.TheLoais = await _theLoaiService.GetActiveOptionsAsync(ct);
        ViewBag.ChuaNhanSach = summary.ChuaNhanSach;

        var routeValues = new Dictionary<string, string>();
        if (search is not null) routeValues["search"] = search;
        if (groupFilter is not null) routeValues["groupFilter"] = groupFilter;
        if (trangThai.HasValue) routeValues["trangThai"] = trangThai.Value.ToString();
        if (maTheLoai.HasValue) routeValues["maTheLoai"] = maTheLoai.Value.ToString();
        if (overdueOnly == true) routeValues["overdueOnly"] = "true";

        var model = new PhieuMuonManageListViewModel
        {
            Search = search,
            GroupFilter = groupFilter,
            TrangThai = trangThai,
            MaTheLoai = maTheLoai,
            OverdueOnly = overdueOnly,
            PageSize = result.PageSize,
            Result = result,
            Pager = PagerViewModel.From(result, "QuanLyPhieuMuon", nameof(Index), routeValues),
            ChoDuyetGiaoCount = summary.ChoDuyet + summary.DaDuyet,
            DangMuonCount = summary.DangMuon,
            HoanThanhCount = summary.HoanThanh,
            ChoDuyetCount = summary.ChoDuyet,
            DaDuyetCount = summary.DaDuyet,
            OverdueCount = summary.QuaHan
        };

        var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Query.ContainsKey("ajax");
        if (isAjax)
            return PartialView("_ManageLoansContentPartial", model);

        return View(model);
    }

    // Chi tiết dùng chung view với bạn đọc; view tự ẩn các nút chỉ dành cho bạn đọc.
    [HttpGet("/ThuThu/QuanLyPhieuMuon/Details/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var detail = await _phieuMuonService.GetLoanDetailAsync(id, null, HttpContext.RequestAborted);
        return detail is null ? NotFound() : View("~/Views/PhieuMuon/Details.cshtml", detail);
    }

    // Phiếu Đã duyệt quá LoanRules.UncollectedDays ngày mà bạn đọc chưa đến nhận (vẫn giữ một bản sách).
    [HttpGet("/ThuThu/PhieuMuonChuaNhanSach")]
    public async Task<IActionResult> ChuaNhanSach()
    {
        var items = await _phieuMuonService.GetUncollectedAsync(HttpContext.RequestAborted);
        return View(items);
    }
    [HttpPost("/ThuThu/QuanLyPhieuMuon/TuDongHuyQuaHan")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TuDongHuyQuaHan(string? returnUrl)
    {
        var count = await _phieuMuonService.AutoCancelOverdueApprovedLoansAsync(HttpContext.RequestAborted);
        var msg = count > 0
            ? $"Đã tự động hủy thành công {count} phiếu mượn đã duyệt nhưng quá 7 ngày chưa nhận sách."
            : "Không có phiếu mượn nào quá hạn nhận sách cần hủy.";

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return Json(new { success = true, count, message = msg });

        TempData["ThongBao"] = msg;
        return Back(returnUrl);
    }
    [HttpPost("/ThuThu/QuanLyPhieuMuon/Duyet/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Duyet(int id, string? nhanXet, string? returnUrl)
    {
        var result = await _phieuMuonService.ApproveLoanAsync(id, ActingId, nhanXet, HttpContext.RequestAborted);
        var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        if (result.Succeeded)
        {
            var msg = $"Đã duyệt phiếu mượn #{id}. Bạn đọc có thể đến quầy nhận sách.";
            if (isAjax) return Json(new { success = true, message = msg });
            TempData["ThongBao"] = msg;
        }
        else
        {
            var msg = $"Không thể duyệt phiếu #{id}: {AllErrors(result)}";
            if (isAjax) return Json(new { success = false, message = msg });
            TempData["Loi"] = msg;
        }

        return Back(returnUrl);
    }
    [HttpPost("/ThuThu/QuanLyPhieuMuon/TuChoi/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TuChoi(int id, string? nhanXet, string? returnUrl)
    {
        var result = await _phieuMuonService.RejectLoanAsync(id, ActingId, nhanXet, HttpContext.RequestAborted);
        var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        if (result.Succeeded)
        {
            var msg = $"Đã từ chối phiếu mượn #{id}.";
            if (isAjax) return Json(new { success = true, message = msg });
            TempData["ThongBao"] = msg;
        }
        else
        {
            var msg = AllErrors(result);
            if (isAjax) return Json(new { success = false, message = msg });
            TempData["Loi"] = msg;
        }

        return Back(returnUrl);
    }
    [HttpPost("/ThuThu/QuanLyPhieuMuon/GiaoSach/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GiaoSach(int id, string? returnUrl)
    {
        var result = await _phieuMuonService.DeliverBookAsync(id, ActingId, HttpContext.RequestAborted);
        var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        if (result.Succeeded)
        {
            var msg = $"Đã giao sách cho phiếu mượn #{id}. Phiếu chuyển sang trạng thái 'Đang mượn'.";
            if (isAjax) return Json(new { success = true, message = msg });
            TempData["ThongBao"] = msg;
        }
        else
        {
            var msg = AllErrors(result);
            if (isAjax) return Json(new { success = false, message = msg });
            TempData["Loi"] = msg;
        }

        return Back(returnUrl);
    }
    [HttpPost("/ThuThu/QuanLyPhieuMuon/HuyPhieu/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HuyPhieu(int id, string? nhanXet, string? returnUrl)
    {
        var result = await _phieuMuonService.CancelLoanByStaffAsync(id, ActingId, nhanXet, HttpContext.RequestAborted);
        var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        if (result.Succeeded)
        {
            var msg = $"Đã hủy phiếu mượn #{id}.";
            if (isAjax) return Json(new { success = true, message = msg });
            TempData["ThongBao"] = msg;
        }
        else
        {
            var msg = $"Không thể hủy phiếu #{id}: {AllErrors(result)}";
            if (isAjax) return Json(new { success = false, message = msg });
            TempData["Loi"] = msg;
        }

        return Back(returnUrl);
    }
}
