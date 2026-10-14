// Họ và tên: Vũ Tiến Thành
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Quản lý danh sách, chi tiết và CRUD thể loại dành riêng cho Admin; ngăn xóa thể loại đang được sách sử dụng.

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien_UNETI4_TI17A1CL.Data;
using QuanLyThuVien_UNETI4_TI17A1CL.Models;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Auth;
using QuanLyThuVien_UNETI4_TI17A1CL.ViewModels;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Controllers;

[AuthorizeRole(VaiTro.Admin)]
public sealed class TheLoaiController(AppDbContext db) : Controller
{
    private const int PageSize = 10;

    [HttpGet]
    public async Task<IActionResult> Index(string? tuKhoa, int page = 1, CancellationToken cancellationToken = default)
    {
        tuKhoa = string.IsNullOrWhiteSpace(tuKhoa) ? null : tuKhoa.Trim();
        var query = db.TheLoais.AsNoTracking().AsQueryable();
        if (tuKhoa is not null)
        {
            var keyword = tuKhoa.ToLower();
            query = query.Where(category =>
                category.TenTheLoai.ToLower().Contains(keyword) ||
                (category.MoTa != null && category.MoTa.ToLower().Contains(keyword)));
        }

        var items = query.OrderBy(category => category.TenTheLoai)
            .Select(category => new TheLoaiListItemViewModel(
                category.MaTheLoai,
                category.TenTheLoai,
                category.MoTa,
                category.KhuVucLuuTru,
                category.TrangThai,
                category.Sachs.Count));
        var result = await PaginatedList<TheLoaiListItemViewModel>.CreateAsync(
            items, Math.Max(page, 1), PageSize, cancellationToken);
        return View(new TheLoaiIndexViewModel { TuKhoa = tuKhoa, TheLoais = result });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var category = await db.TheLoais.AsNoTracking()
            .Where(item => item.MaTheLoai == id)
            .Select(item => new TheLoaiDetailsViewModel
            {
                MaTheLoai = item.MaTheLoai,
                TenTheLoai = item.TenTheLoai,
                MoTa = item.MoTa,
                KhuVucLuuTru = item.KhuVucLuuTru,
                TrangThai = item.TrangThai,
                SoLuongSach = item.Sachs.Count
            })
            .FirstOrDefaultAsync(cancellationToken);
        return category is null ? NotFound() : View(category);
    }

    [HttpGet]
    public IActionResult Create() => View(new TheLoaiFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TheLoaiFormViewModel model, CancellationToken cancellationToken)
    {
        Normalize(model);
        ValidateStatus(model);
        await ValidateNameAsync(model.TenTheLoai, null, cancellationToken);
        if (!ModelState.IsValid)
            return View(model);

        db.TheLoais.Add(new TheLoai
        {
            TenTheLoai = model.TenTheLoai,
            MoTa = model.MoTa,
            KhuVucLuuTru = model.KhuVucLuuTru,
            TrangThai = model.TrangThai
        });
        await db.SaveChangesAsync(cancellationToken);
        TempData["ThongBao"] = "Đã thêm thể loại.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var category = await db.TheLoais.AsNoTracking()
            .FirstOrDefaultAsync(item => item.MaTheLoai == id, cancellationToken);
        if (category is null)
            return NotFound();

        return View(new TheLoaiFormViewModel
        {
            MaTheLoai = category.MaTheLoai,
            TenTheLoai = category.TenTheLoai,
            MoTa = category.MoTa,
            KhuVucLuuTru = category.KhuVucLuuTru,
            TrangThai = category.TrangThai
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(TheLoaiFormViewModel model, CancellationToken cancellationToken)
    {
        Normalize(model);
        var category = await db.TheLoais
            .FirstOrDefaultAsync(item => item.MaTheLoai == model.MaTheLoai, cancellationToken);
        if (category is null)
            return NotFound();

        ValidateStatus(model);
        await ValidateNameAsync(model.TenTheLoai, model.MaTheLoai, cancellationToken);
        if (!ModelState.IsValid)
            return View(model);

        category.TenTheLoai = model.TenTheLoai;
        category.MoTa = model.MoTa;
        category.KhuVucLuuTru = model.KhuVucLuuTru;
        category.TrangThai = model.TrangThai;
        await db.SaveChangesAsync(cancellationToken);
        TempData["ThongBao"] = "Đã cập nhật thể loại.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var category = await db.TheLoais.AsNoTracking()
            .Where(item => item.MaTheLoai == id)
            .Select(item => new TheLoaiDetailsViewModel
            {
                MaTheLoai = item.MaTheLoai,
                TenTheLoai = item.TenTheLoai,
                MoTa = item.MoTa,
                KhuVucLuuTru = item.KhuVucLuuTru,
                TrangThai = item.TrangThai,
                SoLuongSach = item.Sachs.Count
            })
            .FirstOrDefaultAsync(cancellationToken);
        return category is null ? NotFound() : View(category);
    }

    [HttpPost, ActionName(nameof(Delete))]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        var category = await db.TheLoais
            .FirstOrDefaultAsync(item => item.MaTheLoai == id, cancellationToken);
        if (category is null)
            return NotFound();

        if (await db.Sachs.AnyAsync(book => book.MaTheLoai == id, cancellationToken))
        {
            TempData["Loi"] = "Không thể xóa thể loại đang được sách sử dụng. Hãy chuyển thể loại sang Ngừng hoạt động.";
            return RedirectToAction(nameof(Delete), new { id });
        }

        db.TheLoais.Remove(category);
        await db.SaveChangesAsync(cancellationToken);
        TempData["ThongBao"] = "Đã xóa thể loại.";
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateNameAsync(string name, int? currentId, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(name) &&
            await db.TheLoais.AsNoTracking()
                .AnyAsync(item => item.TenTheLoai == name && item.MaTheLoai != currentId, cancellationToken))
        {
            ModelState.AddModelError(nameof(TheLoaiFormViewModel.TenTheLoai), "Tên thể loại đã tồn tại.");
        }
    }

    private static void Normalize(TheLoaiFormViewModel model)
    {
        model.TenTheLoai = model.TenTheLoai?.Trim() ?? string.Empty;
        model.MoTa = string.IsNullOrWhiteSpace(model.MoTa) ? null : model.MoTa.Trim();
        model.KhuVucLuuTru = string.IsNullOrWhiteSpace(model.KhuVucLuuTru) ? null : model.KhuVucLuuTru.Trim();
    }

    private void ValidateStatus(TheLoaiFormViewModel model)
    {
        if (!Enum.IsDefined(model.TrangThai))
            ModelState.AddModelError(nameof(model.TrangThai), "Trạng thái thể loại không hợp lệ.");
    }
}
