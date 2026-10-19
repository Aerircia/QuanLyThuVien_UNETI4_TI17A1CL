// Họ và tên: Nguyễn Hữu Tuấn
// Mã sinh viên: 23103100136
// Nội dung thực hiện: HomeController hiển thị trang chủ và danh mục tra cứu sách mượn

using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien_UNETI4_TI17A1CL.Data;
using QuanLyThuVien_UNETI4_TI17A1CL.Models;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;
using QuanLyThuVien_UNETI4_TI17A1CL.ViewModels;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _db;

    public HomeController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(string? search, int? maTheLoai, bool? conSach)
    {
        var theLoais = await _db.TheLoais
            .Where(t => t.TrangThai == TrangThaiTheLoai.HoatDong)
            .OrderBy(t => t.TenTheLoai)
            .ToListAsync(HttpContext.RequestAborted);

        var query = _db.Sachs
            .Include(s => s.TheLoai)
            .Include(s => s.PhieuMuons)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(b => b.TenSach.Contains(s) || b.TacGia.Contains(s));
        }

        if (maTheLoai.HasValue)
        {
            query = query.Where(b => b.MaTheLoai == maTheLoai.Value);
        }

        var booksRaw = await query
            .OrderByDescending(b => b.MaSach)
            .ToListAsync(HttpContext.RequestAborted);

        // Đếm số lượng đã mượn / mất qua LINQ
        var bookIds = booksRaw.Select(b => b.MaSach).ToList();
        var heldCounts = await _db.PhieuMuons
            .Where(p => bookIds.Contains(p.MaSach) &&
                       (p.TrangThai == TrangThaiPhieuMuon.DaDuyet || p.TrangThai == TrangThaiPhieuMuon.DangMuon))
            .GroupBy(p => p.MaSach)
            .Select(g => new { MaSach = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.MaSach, x => x.Count, HttpContext.RequestAborted);

        var lostCounts = await _db.TraSachs
            .Where(t => bookIds.Contains(t.PhieuMuon!.MaSach) && t.TinhTrangSach == TinhTrangSach.Mat)
            .GroupBy(t => t.PhieuMuon!.MaSach)
            .Select(g => new { MaSach = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.MaSach, x => x.Count, HttpContext.RequestAborted);

        var bookItems = booksRaw.Select(b =>
        {
            var held = heldCounts.GetValueOrDefault(b.MaSach, 0);
            var lost = lostCounts.GetValueOrDefault(b.MaSach, 0);
            var left = Math.Max(0, b.SoLuongTong - held - lost);

            return new BookItemViewModel
            {
                MaSach = b.MaSach,
                TenSach = b.TenSach,
                TacGia = b.TacGia,
                MaTheLoai = b.MaTheLoai,
                TenTheLoai = b.TheLoai?.TenTheLoai,
                NhaXuatBan = b.NhaXuatBan,
                NamXuatBan = b.NamXuatBan,
                SoLuongTong = b.SoLuongTong,
                SoLuongConLai = left,
                GiaBia = b.GiaBia,
                ViTriKe = b.ViTriKe,
                MoTa = b.MoTa,
                TrangThai = b.TrangThai
            };
        }).ToList();

        if (conSach == true)
        {
            bookItems = bookItems.Where(b => b.ConSach).ToList();
        }

        var model = new BookCatalogViewModel
        {
            Search = search,
            MaTheLoai = maTheLoai,
            ConSach = conSach,
            TheLoais = theLoais,
            Books = bookItems,
            TongSoDauSach = await _db.Sachs.CountAsync(HttpContext.RequestAborted),
            TongSoBanSach = await _db.Sachs.SumAsync(s => (int?)s.SoLuongTong ?? 0, HttpContext.RequestAborted),
            SoDauSachSanSang = bookItems.Count(b => b.ConSach)
        };

        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
