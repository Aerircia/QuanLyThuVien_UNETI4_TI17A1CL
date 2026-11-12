// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Xây dựng Controller ThanhToanController để quản lý thanh toán trực tuyến và lịch sử thanh toán của bạn đọc
using Microsoft.AspNetCore.Mvc;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Auth;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Common;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;
using QuanLyThuVien_UNETI4_TI17A1CL.ViewModels;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Controllers;

public class ThanhToanController : Controller
{
    private readonly IThanhToanService _service;
    private readonly ICurrentUserService _current;

    public ThanhToanController(IThanhToanService service, ICurrentUserService current)
    {
        _service = service;
        _current = current;
    }

    // Bạn đọc chọn các khoản phạt ở MyFines; chỉ nhận danh sách MaTra, số tiền do server tính.
    [HttpPost("/ThanhToan/Pay")]
    [AuthorizeRole(VaiTro.BanDoc)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Pay(int[]? maTras)
    {
        if (!await _current.HasActiveBanDocAsync(HttpContext.RequestAborted))
            return RedirectToAction("MyProfile", "BanDoc");
        var maBanDoc = await _current.GetMaBanDocAsync(HttpContext.RequestAborted);
        if (!maBanDoc.HasValue)
            return RedirectToAction("MyProfile", "BanDoc");

        var result = await _service.CreateOnlineAsync(maBanDoc.Value, maTras ?? Array.Empty<int>(), HttpContext.RequestAborted);
        if (!result.Succeeded)
        {
            TempData["Loi"] = result.FirstError;
            return RedirectToAction("MyFines", "PhieuMuon");
        }
        return Redirect(result.Value!);   // chuyển sang trang thanh toán payOS
    }

    // payOS đưa trình duyệt về đây. Query string KHÔNG được tin: chỉ dùng orderCode để hỏi lại payOS.
    // Cố ý không có [AuthorizeRole]: kết quả không phụ thuộc session; trang Result mới kiểm tra chủ sở hữu.
    [HttpGet("/ThanhToan/Return")]
    public async Task<IActionResult> Return(long orderCode)
    {
        await _service.SettleAsync(orderCode, HttpContext.RequestAborted);
        return RedirectToAction(nameof(Result), new { orderCode });
    }

    // Khách bấm "Hủy" trên trang payOS. Không đổi trạng thái bằng GET: chỉ đối soát rồi hiện kết quả.
    [HttpGet("/ThanhToan/Cancel")]
    public async Task<IActionResult> Cancel(long orderCode)
    {
        await _service.SettleAsync(orderCode, HttpContext.RequestAborted);
        return RedirectToAction(nameof(Result), new { orderCode });
    }
    [HttpGet("/BanDoc/KetQuaThanhToan")]
    [AuthorizeRole(VaiTro.BanDoc)]
    public async Task<IActionResult> Result(long orderCode)
    {
        var maBanDoc = await _current.GetMaBanDocAsync(HttpContext.RequestAborted);
        if (!maBanDoc.HasValue) return NotFound();
        var detail = await _service.GetByOrderCodeAsync(orderCode, maBanDoc.Value, HttpContext.RequestAborted);
        return detail is null ? NotFound() : View(detail);
    }

    // Trang chờ gọi định kỳ (dùng khi trình duyệt không quay về được, vd chạy localhost).
    [HttpGet("/ThanhToan/Status")]
    [AuthorizeRole(VaiTro.BanDoc)]
    public async Task<IActionResult> Status(long orderCode)
    {
        var maBanDoc = await _current.GetMaBanDocAsync(HttpContext.RequestAborted);
        if (!maBanDoc.HasValue) return NotFound();
        if (await _service.GetByOrderCodeAsync(orderCode, maBanDoc.Value, HttpContext.RequestAborted) is null)
            return NotFound();

        var r = await _service.SettleAsync(orderCode, HttpContext.RequestAborted);
        if (!r.Succeeded)
            return Json(new { status = "Error", label = r.FirstError, final = false });
        return Json(new
        {
            status = r.Value.ToString(),
            label = r.Value.GetDisplayName(),
            final = r.Value != TrangThaiGiaoDich.ChoThanhToan
        });
    }
    [HttpPost("/BanDoc/HuyGiaoDich")]
    [AuthorizeRole(VaiTro.BanDoc)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HuyGiaoDich(long orderCode)
    {
        var maBanDoc = await _current.GetMaBanDocAsync(HttpContext.RequestAborted);
        if (!maBanDoc.HasValue) return Forbid();

        var result = await _service.CancelOnlineAsync(orderCode, maBanDoc.Value, HttpContext.RequestAborted);
        if (result.Succeeded) TempData["ThongBao"] = "Đã hủy giao dịch thanh toán.";
        else TempData["Loi"] = result.FirstError;
        return RedirectToAction(nameof(Result), new { orderCode });
    }

    // Webhook payOS (tùy chọn, cần URL công khai). Không antiforgery, không session; xác thực bằng chữ ký.
    [HttpPost("/ThanhToan/Webhook")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Webhook()
    {
        using var reader = new StreamReader(Request.Body);
        var raw = await reader.ReadToEndAsync(HttpContext.RequestAborted);
        return await _service.HandleWebhookAsync(raw, HttpContext.RequestAborted) ? Ok() : BadRequest();
    }

    // Lịch sử thanh toán của bạn đọc (chỉ giao dịch của chính mình).
    [HttpGet("/BanDoc/LichSuThanhToan")]
    [AuthorizeRole(VaiTro.BanDoc)]
    public async Task<IActionResult> LichSu(
        string? search, PhuongThucThanhToan? phuongThuc, TrangThaiGiaoDich? trangThai,
        DateOnly? tu, DateOnly? den, int page = 1, int pageSize = PagingDefaults.DefaultPageSize)
    {
        var maBanDoc = await _current.GetMaBanDocAsync(HttpContext.RequestAborted);
        if (!maBanDoc.HasValue) return RedirectToAction("MyProfile", "BanDoc");
        if (phuongThuc.HasValue && !Enum.IsDefined(phuongThuc.Value)) phuongThuc = null;
        if (trangThai.HasValue && !Enum.IsDefined(trangThai.Value)) trangThai = null;
        search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

        await _service.GetPendingFineIdsAsync(maBanDoc.Value, HttpContext.RequestAborted);   // cập nhật giao dịch đang chờ
        var result = await _service.GetListAsync(
            new ThanhToanFilter(search, phuongThuc, trangThai, tu, den, page, pageSize),
            maBanDoc.Value, HttpContext.RequestAborted);

        var route = new Dictionary<string, string>();
        if (search is not null) route["search"] = search;
        if (phuongThuc.HasValue) route["phuongThuc"] = phuongThuc.Value.ToString();
        if (trangThai.HasValue) route["trangThai"] = trangThai.Value.ToString();
        if (tu.HasValue) route["tu"] = tu.Value.ToString("yyyy-MM-dd");
        if (den.HasValue) route["den"] = den.Value.ToString("yyyy-MM-dd");

        return View(new ThanhToanListViewModel
        {
            Search = search, PhuongThuc = phuongThuc, TrangThai = trangThai, Tu = tu, Den = den,
            Result = result,
            Pager = PagerViewModel.From(result, "ThanhToan", nameof(LichSu), route)
        });
    }
    [HttpGet("/BanDoc/LichSuThanhToan/Details/{id:int}")]
    [AuthorizeRole(VaiTro.BanDoc)]
    public async Task<IActionResult> ChiTiet(int id)
    {
        var maBanDoc = await _current.GetMaBanDocAsync(HttpContext.RequestAborted);
        if (!maBanDoc.HasValue) return NotFound();
        var detail = await _service.GetAsync(id, maBanDoc.Value, HttpContext.RequestAborted);
        return detail is null ? NotFound() : View(detail);
    }
}
