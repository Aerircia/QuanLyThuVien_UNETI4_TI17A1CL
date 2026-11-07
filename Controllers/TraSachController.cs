// Họ và tên: Vũ Ngọc Hương Giang
// Mã sinh viên: 23103100280
// Nội dung thực hiện: Controller trả sách: lịch sử trả, nhận trả sách, chi tiết và đánh dấu đã nộp phạt.

using Microsoft.AspNetCore.Mvc;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Auth;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Common;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;
using QuanLyThuVien_UNETI4_TI17A1CL.ViewModels;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Controllers;
[AuthorizeRole(VaiTro.Admin, VaiTro.ThuThu)]
public class TraSachController : Controller
{
    private readonly ITraSachService _service;
    private readonly IThanhToanService _thanhToanService;
    private readonly ICurrentUserService _current;

    public TraSachController(ITraSachService service, IThanhToanService thanhToanService, ICurrentUserService current)
    {
        _service = service;
        _thanhToanService = thanhToanService;
        _current = current;
    }
    private bool IsAjax => Request.Headers["X-Requested-With"] == "XMLHttpRequest";
    private IActionResult AjaxLoi(string macDinh = "Không thể ghi nhận trả sách.")
    {
        var loi = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
            .Where(m => !string.IsNullOrWhiteSpace(m)).ToList();
        return Json(new { success = false, message = loi.Count > 0 ? string.Join(" ", loi) : macDinh });
    }

// Lịch sử trả sách và tiền phạt 
    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        TinhTrangSach? tinhTrang,
        TrangThaiPhat? trangThaiPhat,
        int page = 1,
        int pageSize = PagingDefaults.DefaultPageSize)
    {
        if (tinhTrang.HasValue && !Enum.IsDefined(tinhTrang.Value)) tinhTrang = null;
        if (trangThaiPhat.HasValue && !Enum.IsDefined(trangThaiPhat.Value)) trangThaiPhat = null;
        search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

        var result = await _service.GetListAsync(
            new TraSachFilter(search, tinhTrang, trangThaiPhat, page, pageSize), HttpContext.RequestAborted);

        var routeValues = new Dictionary<string, string>();
        if (search is not null) routeValues["search"] = search;
        if (tinhTrang.HasValue) routeValues["tinhTrang"] = tinhTrang.Value.ToString();
        if (trangThaiPhat.HasValue) routeValues["trangThaiPhat"] = trangThaiPhat.Value.ToString();
        var vm = new TraSachListViewModel
        {
            Search = search,
            TinhTrang = tinhTrang,
            TrangThaiPhat = trangThaiPhat,
            PageSize = result.PageSize,
            Result = result,
            Pager = PagerViewModel.From(result, "TraSach", nameof(Index), routeValues)
        };

        // AJAX (tìm/lọc/phân trang): chỉ trả partial bảng + phân trang
        if (IsAjax)
            return PartialView("_BangTraSach", vm);

        return View(vm);
    }
// Phiếu đang mượn chờ nhận trả
    [HttpGet]
    public async Task<IActionResult> ChoTra(
        string? search,
        bool chiQuaHan = false,
        int page = 1,
        int pageSize = PagingDefaults.DefaultPageSize)
    {
        search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

        var result = await _service.GetPhieuChoTraAsync(
            new PhieuChoTraFilter(search, chiQuaHan, page, pageSize), HttpContext.RequestAborted);

        var routeValues = new Dictionary<string, string>();
        if (search is not null) routeValues["search"] = search;
        if (chiQuaHan) routeValues["chiQuaHan"] = "true";

        var vm = new PhieuChoTraListViewModel
        {
            Search = search,
            ChiQuaHan = chiQuaHan,
            PageSize = result.PageSize,
            Result = result,
            Pager = PagerViewModel.From(result, "TraSach", nameof(ChoTra), routeValues)
        };

        if (IsAjax)
            return PartialView("_BangChoTra", vm);

        return View(vm);
    }

//Nhận trả sách 
    [HttpGet("TraSach/Create/{maPhieuMuon:int}")]
    public async Task<IActionResult> Create(int maPhieuMuon)
    {
        var preview = await _service.GetPreviewAsync(maPhieuMuon, HttpContext.RequestAborted);
        if (preview is null)
            return NotFound();

        return View(new TraSachCreateViewModel { Preview = preview });
    }

    [HttpPost("TraSach/Create/{maPhieuMuon:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int maPhieuMuon, TraSachCreateViewModel model)
    {
        var preview = await _service.GetPreviewAsync(maPhieuMuon, HttpContext.RequestAborted);
        if (preview is null)
            return NotFound();
        model.Preview = preview;

        if (!ModelState.IsValid)
            return IsAjax ? AjaxLoi("Dữ liệu không hợp lệ.") : View(model);

        var result = await _service.CreateAsync(
            new TraSachInput(maPhieuMuon, model.TinhTrang, model.GhiChu),
            _current.User!.MaTaiKhoan,       
            HttpContext.RequestAborted);

        if (!result.Succeeded)
        {
            result.AddTo(ModelState);
            return IsAjax ? AjaxLoi() : View(model);
        }

        TempData["ThongBao"] = "Đã ghi nhận trả sách.";
        if (IsAjax)
            return Json(new
            {
                success = true,
                message = "Đã ghi nhận trả sách.",
                redirectUrl = Url.Action(nameof(Details), new { id = result.Value })
            });

        return RedirectToAction(nameof(Details), new { id = result.Value });
    }

//Chi tiết
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var detail = await _service.GetAsync(id, HttpContext.RequestAborted);
        if (detail is null)
            return NotFound();

        return View(detail);
    }

// Đánh dấu đã nộp phạt 
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkPaid(int id)
    {
        var result = await _thanhToanService.RecordCounterAsync(new[] { id }, _current.User!.MaTaiKhoan, HttpContext.RequestAborted);
        if (IsAjax)
            return Json(new
            {
                success = result.Succeeded,
                message = result.Succeeded ? "Đã ghi nhận nộp phạt." : result.FirstError
            });
        if (result.Succeeded)
            TempData["ThongBao"] = "Đã ghi nhận nộp phạt.";
        else
            TempData["Loi"] = result.FirstError;

        return RedirectToAction(nameof(Details), new { id });
    }
}