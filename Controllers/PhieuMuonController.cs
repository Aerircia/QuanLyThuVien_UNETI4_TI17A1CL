using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien_UNETI4_TI17A1CL.Data;
using QuanLyThuVien_UNETI4_TI17A1CL.Models;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Auth;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Controllers;

public class PhieuMuonController : Controller
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _current;

    public PhieuMuonController(AppDbContext db, ICurrentUserService current)
    {
        _db = db;
        _current = current;
    }

    [HttpGet]
    [AuthorizeRole(VaiTro.Admin, VaiTro.ThuThu)]
    public async Task<IActionResult> Index(TrangThaiPhieuMuon? trangThai)
    {
        var query = _db.PhieuMuons.AsNoTracking()
            .Include(p => p.BanDoc)
            .Include(p => p.Sach)
            .AsQueryable();
        if (trangThai.HasValue)
            query = query.Where(p => p.TrangThai == trangThai.Value);

        ViewBag.TrangThai = trangThai;
        return View(await query.OrderByDescending(p => p.NgayDangKy)
            .ThenByDescending(p => p.MaPhieuMuon).ToListAsync(HttpContext.RequestAborted));
    }

    [HttpPost]
    [AuthorizeRole(VaiTro.BanDoc)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(int maSach, string? ghiChu)
    {
        var maBanDoc = await _current.GetMaBanDocAsync(HttpContext.RequestAborted);
        if (maBanDoc is null)
            return RedirectToAction("AccessDenied", "TaiKhoan");

        var book = await _db.Sachs.FirstOrDefaultAsync(s =>
            s.MaSach == maSach && s.TrangThai == TrangThaiSach.DangPhucVu,
            HttpContext.RequestAborted);
        if (book is null)
        {
            TempData["Loi"] = "Sách không tồn tại hoặc hiện không thể mượn.";
            return RedirectToAction("Details", "Sach", new { id = maSach });
        }

        if (await _db.PhieuMuons.AnyAsync(p =>
            p.MaBanDoc == maBanDoc && p.MaSach == maSach &&
            (p.TrangThai == TrangThaiPhieuMuon.ChoDuyet ||
             p.TrangThai == TrangThaiPhieuMuon.DaDuyet ||
             p.TrangThai == TrangThaiPhieuMuon.DangMuon),
            HttpContext.RequestAborted))
        {
            TempData["Loi"] = "Bạn đã có phiếu đăng ký hoặc đang mượn cuốn sách này.";
            return RedirectToAction("Details", "Sach", new { id = maSach });
        }

        var reader = await _db.BanDocs.AsNoTracking().FirstOrDefaultAsync(
            b => b.MaBanDoc == maBanDoc.Value, HttpContext.RequestAborted);
        var today = DateOnly.FromDateTime(DateTime.Today);
        if (reader is null || reader.TrangThai != TrangThaiBanDoc.HoatDong || reader.NgayHetHanThe < today)
        {
            TempData["Loi"] = "Thẻ bạn đọc chưa hoạt động hoặc đã hết hạn.";
            return RedirectToAction("Details", "Sach", new { id = maSach });
        }

        var loan = new PhieuMuon
        {
            MaBanDoc = maBanDoc.Value,
            MaSach = maSach,
            NgayDangKy = today,
            GhiChu = string.IsNullOrWhiteSpace(ghiChu) ? null : ghiChu.Trim(),
            TrangThai = TrangThaiPhieuMuon.ChoDuyet
        };
        _db.PhieuMuons.Add(loan);
        await _db.SaveChangesAsync(HttpContext.RequestAborted);

        _db.PhieuMuonLichSus.Add(new PhieuMuonLichSu
        {
            MaPhieuMuon = loan.MaPhieuMuon,
            TrangThaiMoi = loan.TrangThai,
            NgayThucHien = DateTime.Now,
            MaNguoiThucHien = _current.User!.MaTaiKhoan,
            GhiChu = "Bạn đọc đăng ký mượn sách"
        });
        await _db.SaveChangesAsync(HttpContext.RequestAborted);
        TempData["ThongBao"] = "Đã gửi đăng ký mượn. Phiếu đang chờ thủ thư xác nhận.";
        return RedirectToAction("Details", "Sach", new { id = maSach });
    }

    [HttpPost]
    [AuthorizeRole(VaiTro.Admin, VaiTro.ThuThu)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, string? nhanXet, bool accept = true)
    {
        var loan = await _db.PhieuMuons.FirstOrDefaultAsync(p => p.MaPhieuMuon == id, HttpContext.RequestAborted);
        if (loan is null || loan.TrangThai != TrangThaiPhieuMuon.ChoDuyet)
        {
            TempData["Loi"] = "Phiếu không còn ở trạng thái chờ duyệt.";
            return RedirectToAction(nameof(Index));
        }
        var old = loan.TrangThai;
        loan.TrangThai = accept ? TrangThaiPhieuMuon.DaDuyet : TrangThaiPhieuMuon.TuChoi;
        loan.NgayXuLy = DateOnly.FromDateTime(DateTime.Today);
        loan.NhanXetThuThu = string.IsNullOrWhiteSpace(nhanXet) ? null : nhanXet.Trim();
        AddHistory(loan, old, nhanXet ?? (accept ? "Thủ thư xác nhận yêu cầu" : "Thủ thư từ chối yêu cầu"));
        await _db.SaveChangesAsync(HttpContext.RequestAborted);
        TempData["ThongBao"] = accept ? "Đã xác nhận phiếu mượn." : "Đã từ chối phiếu mượn.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [AuthorizeRole(VaiTro.Admin, VaiTro.ThuThu)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id, string? nhanXet)
    {
        var loan = await _db.PhieuMuons.FirstOrDefaultAsync(p => p.MaPhieuMuon == id, HttpContext.RequestAborted);
        if (loan is null || loan.TrangThai != TrangThaiPhieuMuon.DaDuyet)
        {
            TempData["Loi"] = "Chỉ hoàn tất phiếu đã được xác nhận.";
            return RedirectToAction(nameof(Index));
        }
        var old = loan.TrangThai;
        var today = DateOnly.FromDateTime(DateTime.Today);
        loan.TrangThai = TrangThaiPhieuMuon.DangMuon;
        loan.NgayMuon = today;
        loan.HanTra = today.AddDays(14);
        AddHistory(loan, old, nhanXet ?? "Thủ thư giao sách cho bạn đọc");
        await _db.SaveChangesAsync(HttpContext.RequestAborted);
        TempData["ThongBao"] = "Đã giao sách và hoàn tất phiếu mượn.";
        return RedirectToAction(nameof(Index));
    }

    private void AddHistory(PhieuMuon loan, TrangThaiPhieuMuon oldStatus, string note)
    {
        _db.PhieuMuonLichSus.Add(new PhieuMuonLichSu
        {
            MaPhieuMuon = loan.MaPhieuMuon,
            TrangThaiCu = oldStatus,
            TrangThaiMoi = loan.TrangThai,
            NgayThucHien = DateTime.Now,
            MaNguoiThucHien = _current.User!.MaTaiKhoan,
            GhiChu = note.Trim()
        });
    }
}
