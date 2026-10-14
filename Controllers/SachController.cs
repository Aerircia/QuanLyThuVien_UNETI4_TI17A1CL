// Họ và tên: Vũ Tiến Thành
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Xử lý danh mục, tìm kiếm vị trí kệ, chi tiết và CRUD sách; tính tồn từ phiếu mượn, quản lý ảnh và chống xung đột cập nhật.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien_UNETI4_TI17A1CL.Data;
using QuanLyThuVien_UNETI4_TI17A1CL.Models;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Auth;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;
using QuanLyThuVien_UNETI4_TI17A1CL.ViewModels;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Controllers;

public class SachController(
    AppDbContext db,
    IWebHostEnvironment environment,
    ICurrentUserService currentUser) : Controller
{
    private const int PageSize = 10;
    private const long MaxCoverSize = 2 * 1024 * 1024;
    private static readonly HashSet<string> AllowedCoverExtensions =
        [".jpg", ".jpeg", ".png", ".webp"];

    private bool IsStaff =>
        currentUser.IsInRole(VaiTro.Admin, VaiTro.ThuThu);

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Index(SachIndexViewModel filter, CancellationToken cancellationToken)
    {
        var staff = IsStaff;
        var books = db.Sachs.AsNoTracking().Include(s => s.TheLoai).AsQueryable();

        if (!staff)
            books = books.Where(s => s.TrangThai != TrangThaiSach.NgungPhucVu);

        var activeLoans = db.PhieuMuons.AsNoTracking().Where(p =>
            p.TrangThai == TrangThaiPhieuMuon.ChoDuyet ||
            p.TrangThai == TrangThaiPhieuMuon.DaDuyet ||
            p.TrangThai == TrangThaiPhieuMuon.DangMuon);
        var booksWithAvailability = books
            .Select(s => new
            {
                Book = s,
                ActiveLoanCount = activeLoans.Count(p => p.MaSach == s.MaSach)
            })
            .Select(x => new
            {
                x.Book,
                SoLuongConLai = x.Book.SoLuongTong > x.ActiveLoanCount
                    ? x.Book.SoLuongTong - x.ActiveLoanCount
                    : 0
            });

        if (!string.IsNullOrWhiteSpace(filter.TuKhoa))
        {
            var keyword = filter.TuKhoa.Trim().ToLower();
            int? bookCode = int.TryParse(keyword, out var parsedCode) ? parsedCode : null;
            booksWithAvailability = booksWithAvailability.Where(x =>
                x.Book.TenSach.ToLower().Contains(keyword) ||
                x.Book.TacGia.ToLower().Contains(keyword) ||
                (x.Book.ISBN != null && x.Book.ISBN.ToLower().Contains(keyword)) ||
                (x.Book.NhaXuatBan != null && x.Book.NhaXuatBan.ToLower().Contains(keyword)) ||
                (x.Book.ViTriKe != null && x.Book.ViTriKe.ToLower().Contains(keyword)) ||
                x.Book.TheLoai!.TenTheLoai.ToLower().Contains(keyword) ||
                (bookCode.HasValue && x.Book.MaSach == bookCode.Value));
        }

        if (!string.IsNullOrWhiteSpace(filter.TuKhoaTheLoai))
        {
            var categoryKeyword = filter.TuKhoaTheLoai.Trim().ToLower();
            booksWithAvailability = booksWithAvailability
                .Where(x => x.Book.TheLoai!.TenTheLoai.ToLower().Contains(categoryKeyword));
        }

        if (filter.TheLoaiId is int categoryId)
            booksWithAvailability = booksWithAvailability.Where(x =>
                x.Book.MaTheLoai == categoryId &&
                (staff || x.Book.TheLoai!.TrangThai == TrangThaiTheLoai.HoatDong));

        if (filter.ConSach is true)
            booksWithAvailability = booksWithAvailability.Where(x => x.SoLuongConLai > 0);
        else if (filter.ConSach is false)
            booksWithAvailability = booksWithAvailability.Where(x => x.SoLuongConLai == 0);

        if (staff && filter.TrangThai is TrangThaiSach status)
            booksWithAvailability = booksWithAvailability.Where(x => x.Book.TrangThai == status);

        var sortBy = Enum.IsDefined(filter.SapXep) ? filter.SapXep : SapXepSach.TenSach;
        filter.SapXep = sortBy;
        booksWithAvailability = (sortBy, filter.GiamDan) switch
        {
            (SapXepSach.TacGia, false) => booksWithAvailability.OrderBy(x => x.Book.TacGia).ThenBy(x => x.Book.MaSach),
            (SapXepSach.TacGia, true) => booksWithAvailability.OrderByDescending(x => x.Book.TacGia).ThenBy(x => x.Book.MaSach),
            (SapXepSach.NamXuatBan, false) => booksWithAvailability.OrderBy(x => x.Book.NamXuatBan).ThenBy(x => x.Book.MaSach),
            (SapXepSach.NamXuatBan, true) => booksWithAvailability.OrderByDescending(x => x.Book.NamXuatBan).ThenBy(x => x.Book.MaSach),
            (SapXepSach.SoLuongConLai, false) => booksWithAvailability.OrderBy(x => x.SoLuongConLai).ThenBy(x => x.Book.MaSach),
            (SapXepSach.SoLuongConLai, true) => booksWithAvailability.OrderByDescending(x => x.SoLuongConLai).ThenBy(x => x.Book.MaSach),
            (_, false) => booksWithAvailability.OrderBy(x => x.Book.TenSach).ThenBy(x => x.Book.MaSach),
            _ => booksWithAvailability.OrderByDescending(x => x.Book.TenSach).ThenBy(x => x.Book.MaSach)
        };

        var projected = booksWithAvailability.Select(x => new SachIndexItemViewModel(
            x.Book.MaSach, x.Book.TenSach, x.Book.TacGia, x.Book.ISBN, x.Book.NhaXuatBan, x.Book.ViTriKe, x.Book.NamXuatBan,
            x.Book.SoLuongTong, x.SoLuongConLai, x.Book.TrangThai, x.Book.ImgPath,
            x.Book.MaTheLoai, x.Book.TheLoai!.TenTheLoai));
        var safePage = Math.Max(filter.Page, 1);
        filter.Sach = await PaginatedList<SachIndexItemViewModel>.CreateAsync(
            projected, safePage, PageSize, cancellationToken);

        var categories = db.TheLoais.AsNoTracking().AsQueryable();
        if (!staff)
            categories = categories.Where(c => c.TrangThai == TrangThaiTheLoai.HoatDong);

        var countQuery = db.Sachs.AsNoTracking().AsQueryable();
        if (!staff)
            countQuery = countQuery.Where(s => s.TrangThai != TrangThaiSach.NgungPhucVu);

        var counts = await countQuery.GroupBy(s => s.MaTheLoai)
            .Select(group => new { CategoryId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(x => x.CategoryId, x => x.Count, cancellationToken);
        var categoryRows = await categories.OrderBy(c => c.TenTheLoai)
            .Select(c => new { c.MaTheLoai, c.TenTheLoai })
            .ToListAsync(cancellationToken);
        filter.TheLoaiOptions = categoryRows
            .Select(c => new TheLoaiOptionViewModel(c.MaTheLoai, c.TenTheLoai,
                counts.GetValueOrDefault(c.MaTheLoai)))
            .ToList();

        return View(filter);
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var activeLoans = db.PhieuMuons.AsNoTracking().Where(p =>
            p.TrangThai == TrangThaiPhieuMuon.ChoDuyet ||
            p.TrangThai == TrangThaiPhieuMuon.DaDuyet ||
            p.TrangThai == TrangThaiPhieuMuon.DangMuon);
        var book = await db.Sachs.AsNoTracking()
            .Where(s => s.MaSach == id)
            .Select(s => new SachDetailsViewModel
            {
                MaSach = s.MaSach,
                TenSach = s.TenSach,
                TacGia = s.TacGia,
                ISBN = s.ISBN,
                NhaXuatBan = s.NhaXuatBan,
                ViTriKe = s.ViTriKe,
                NamXuatBan = s.NamXuatBan,
                TenTheLoai = s.TheLoai!.TenTheLoai,
                MaTheLoai = s.MaTheLoai,
                MoTa = s.MoTa,
                ImgPath = s.ImgPath,
                SoLuongTong = s.SoLuongTong,
                SoLuongConLai = s.SoLuongTong > activeLoans.Count(p => p.MaSach == s.MaSach)
                    ? s.SoLuongTong - activeLoans.Count(p => p.MaSach == s.MaSach)
                    : 0,
                TrangThai = s.TrangThai
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (book is null || (!IsStaff && book.TrangThai == TrangThaiSach.NgungPhucVu))
            return NotFound();

        return View(book);
    }

    [AuthorizeRole(VaiTro.Admin, VaiTro.ThuThu)]
    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var model = new SachFormViewModel();
        await PopulateActiveCategoriesAsync(model, cancellationToken);
        return View(model);
    }

    [AuthorizeRole(VaiTro.Admin, VaiTro.ThuThu)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SachFormViewModel model, CancellationToken cancellationToken)
    {
        ValidateYear(model);
        await ValidateCategoryAsync(model.MaTheLoai, cancellationToken);
        ValidateCover(model.AnhBia);
        if (!ModelState.IsValid)
        {
            await PopulateActiveCategoriesAsync(model, cancellationToken);
            return View(model);
        }

        var book = new Sach
        {
            TenSach = model.TenSach.Trim(),
            TacGia = model.TacGia.Trim(),
            ISBN = model.ISBN?.Trim(),
            NhaXuatBan = model.NhaXuatBan?.Trim(),
            NamXuatBan = model.NamXuatBan,
            SoLuongTong = model.SoLuongTong,
            GiaBia = model.GiaBia,
            ViTriKe = model.ViTriKe?.Trim(),
            MoTa = model.MoTa?.Trim(),
            MaTheLoai = model.MaTheLoai,
            TrangThai = TrangThaiSach.DangPhucVu
        };

        var savedCover = model.AnhBia is null ? null : await SaveCoverAsync(model.AnhBia, cancellationToken);
        book.ImgPath = savedCover;
        var saved = false;
        try
        {
            db.Sachs.Add(book);
            await db.SaveChangesAsync(cancellationToken);
            saved = true;
        }
        finally
        {
            if (!saved)
                DeleteCoverFile(savedCover);
        }

        TempData["ThongBao"] = "Đã thêm sách thành công.";
        return RedirectToAction(nameof(Index));
    }

    [AuthorizeRole(VaiTro.Admin, VaiTro.ThuThu)]
    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var book = await db.Sachs.AsNoTracking().FirstOrDefaultAsync(s => s.MaSach == id, cancellationToken);
        if (book is null)
            return NotFound();

        var model = new SachFormViewModel
        {
            TenSach = book.TenSach,
            TacGia = book.TacGia,
            ISBN = book.ISBN,
            NhaXuatBan = book.NhaXuatBan,
            NamXuatBan = book.NamXuatBan,
            SoLuongTong = book.SoLuongTong,
            GiaBia = book.GiaBia,
            ViTriKe = book.ViTriKe,
            MoTa = book.MoTa,
            MaTheLoai = book.MaTheLoai,
            AnhBiaHienTai = book.ImgPath,
            RowVersion = Convert.ToBase64String(book.RowVersion)
        };
        await PopulateActiveCategoriesAsync(model, cancellationToken);
        return View(model);
    }

    [AuthorizeRole(VaiTro.Admin, VaiTro.ThuThu)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, SachFormViewModel model, CancellationToken cancellationToken)
    {
        ValidateYear(model);
        await ValidateCategoryAsync(model.MaTheLoai, cancellationToken);
        ValidateCover(model.AnhBia);
        byte[]? submittedRowVersion = null;
        try
        {
            if (string.IsNullOrWhiteSpace(model.RowVersion))
                ModelState.AddModelError(nameof(model.RowVersion), "Thiếu thông tin phiên bản sách. Vui lòng tải lại trang.");
            else
                submittedRowVersion = Convert.FromBase64String(model.RowVersion);
        }
        catch (FormatException)
        {
            ModelState.AddModelError(nameof(model.RowVersion), "Thông tin phiên bản sách không hợp lệ. Vui lòng tải lại trang.");
        }

        var book = await db.Sachs.FirstOrDefaultAsync(s => s.MaSach == id, cancellationToken);
        if (book is null)
            return NotFound();

        if (!ModelState.IsValid)
        {
            model.AnhBiaHienTai = book.ImgPath;
            await PopulateActiveCategoriesAsync(model, cancellationToken);
            return View(model);
        }

        var oldCover = book.ImgPath;
        book.TenSach = model.TenSach.Trim();
        book.TacGia = model.TacGia.Trim();
        book.ISBN = model.ISBN?.Trim();
        book.NhaXuatBan = model.NhaXuatBan?.Trim();
        book.NamXuatBan = model.NamXuatBan;
        book.SoLuongTong = model.SoLuongTong;
        book.GiaBia = model.GiaBia;
        book.ViTriKe = model.ViTriKe?.Trim();
        book.MoTa = model.MoTa?.Trim();
        book.MaTheLoai = model.MaTheLoai;
        db.Entry(book).Property(s => s.RowVersion).OriginalValue = submittedRowVersion!;

        var newCover = model.AnhBia is null ? null : await SaveCoverAsync(model.AnhBia, cancellationToken);
        if (newCover is not null)
            book.ImgPath = newCover;

        var saved = false;
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            saved = true;
        }
        catch (DbUpdateConcurrencyException)
        {
            TempData["Loi"] = "Sách đã được người khác cập nhật. Vui lòng kiểm tra dữ liệu mới rồi thử lại.";
            return RedirectToAction(nameof(Edit), new { id });
        }
        finally
        {
            if (!saved)
                DeleteCoverFile(newCover);
        }

        if (newCover is not null)
            DeleteCoverFile(oldCover);
        TempData["ThongBao"] = "Đã cập nhật thông tin sách.";
        return RedirectToAction(nameof(Index));
    }

    [AuthorizeRole(VaiTro.Admin, VaiTro.ThuThu)]
    [HttpGet]
    public async Task<IActionResult> ChangeStatus(int id, CancellationToken cancellationToken)
    {
        var book = await db.Sachs.AsNoTracking().FirstOrDefaultAsync(s => s.MaSach == id, cancellationToken);
        return book is null ? NotFound() : View(new SachChangeStatusViewModel
        {
            MaSach = book.MaSach,
            TenSach = book.TenSach,
            TrangThai = book.TrangThai
        });
    }

    [AuthorizeRole(VaiTro.Admin, VaiTro.ThuThu)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(
        int id, SachChangeStatusViewModel model, CancellationToken cancellationToken)
    {
        var book = await db.Sachs.FirstOrDefaultAsync(s => s.MaSach == id, cancellationToken);
        if (book is null)
            return NotFound();
        model = new SachChangeStatusViewModel
        {
            MaSach = book.MaSach,
            TenSach = book.TenSach,
            TrangThai = model.TrangThai
        };
        if (!Enum.IsDefined(model.TrangThai))
            ModelState.AddModelError(nameof(model.TrangThai), "Trạng thái sách không hợp lệ.");
        if (!ModelState.IsValid)
            return View(model);

        book.TrangThai = model.TrangThai;
        await db.SaveChangesAsync(cancellationToken);
        TempData["ThongBao"] = "Đã cập nhật trạng thái sách.";
        return RedirectToAction(nameof(Index));
    }

    [AuthorizeRole(VaiTro.Admin, VaiTro.ThuThu)]
    [HttpGet]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var book = await db.Sachs.AsNoTracking().Include(s => s.TheLoai)
            .FirstOrDefaultAsync(s => s.MaSach == id, cancellationToken);
        if (book is null)
            return NotFound();

        return View(ToDeleteViewModel(book,
            await HasActiveLoansAsync(id, cancellationToken),
            await db.PhieuMuons.AnyAsync(p => p.MaSach == id, cancellationToken)));
    }

    [AuthorizeRole(VaiTro.Admin, VaiTro.ThuThu)]
    [HttpPost]
    [ActionName(nameof(Delete))]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        var book = await db.Sachs.FirstOrDefaultAsync(s => s.MaSach == id, cancellationToken);
        if (book is null)
            return NotFound();

        if (await HasActiveLoansAsync(id, cancellationToken))
        {
            TempData["Loi"] = "Không thể xóa sách đang có phiếu mượn chờ duyệt, đã duyệt hoặc đang mượn.";
            return RedirectToAction(nameof(Delete), new { id });
        }
        if (await db.PhieuMuons.AnyAsync(p => p.MaSach == id, cancellationToken))
        {
            TempData["Loi"] = "Sách đã có lịch sử mượn, không thể xóa. Vui lòng chuyển trạng thái sang Ngừng phục vụ.";
            return RedirectToAction(nameof(Delete), new { id });
        }

        var cover = book.ImgPath;
        db.Sachs.Remove(book);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            TempData["Loi"] = "Không thể xóa sách vì dữ liệu liên quan vừa phát sinh. Vui lòng chuyển trạng thái sang Ngừng phục vụ.";
            return RedirectToAction(nameof(Delete), new { id });
        }

        DeleteCoverFile(cover);
        TempData["ThongBao"] = "Đã xóa sách thành công.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> HasActiveLoansAsync(int bookId, CancellationToken cancellationToken) =>
        await db.PhieuMuons.AsNoTracking().AnyAsync(p =>
            p.MaSach == bookId &&
            (p.TrangThai == TrangThaiPhieuMuon.ChoDuyet ||
             p.TrangThai == TrangThaiPhieuMuon.DaDuyet ||
             p.TrangThai == TrangThaiPhieuMuon.DangMuon), cancellationToken);

    private static SachDeleteViewModel ToDeleteViewModel(Sach book, bool hasActiveLoans, bool hasHistory) => new()
    {
        MaSach = book.MaSach,
        TenSach = book.TenSach,
        TacGia = book.TacGia,
        NhaXuatBan = book.NhaXuatBan,
        TenTheLoai = book.TheLoai?.TenTheLoai ?? string.Empty,
        SoLuongTong = book.SoLuongTong,
        TrangThai = book.TrangThai,
        ImgPath = book.ImgPath,
        CoPhieuDangXuLy = hasActiveLoans,
        CoLichSuMuon = hasHistory
    };

    private async Task PopulateActiveCategoriesAsync(SachFormViewModel model, CancellationToken cancellationToken)
    {
        model.TheLoaiOptions = await db.TheLoais.AsNoTracking()
            .Where(c => c.TrangThai == TrangThaiTheLoai.HoatDong)
            .OrderBy(c => c.TenTheLoai)
            .Select(c => new TheLoaiOptionViewModel(c.MaTheLoai, c.TenTheLoai, 0))
            .ToListAsync(cancellationToken);
    }

    private async Task ValidateCategoryAsync(int categoryId, CancellationToken cancellationToken)
    {
        if (!await db.TheLoais.AsNoTracking().AnyAsync(c =>
                c.MaTheLoai == categoryId && c.TrangThai == TrangThaiTheLoai.HoatDong, cancellationToken))
            ModelState.AddModelError(nameof(SachFormViewModel.MaTheLoai), "Vui lòng chọn thể loại đang hoạt động.");
    }

    private void ValidateYear(SachFormViewModel model)
    {
        if (model.NamXuatBan > DateTime.Now.Year + 1)
            ModelState.AddModelError(nameof(model.NamXuatBan), "Năm xuất bản không được vượt quá năm sau.");
    }

    private void ValidateCover(IFormFile? cover)
    {
        if (cover is null)
            return;
        if (cover.Length == 0)
            ModelState.AddModelError(nameof(SachFormViewModel.AnhBia), "Tệp ảnh không có dữ liệu.");
        else if (cover.Length > MaxCoverSize)
            ModelState.AddModelError(nameof(SachFormViewModel.AnhBia), "Ảnh bìa không được vượt quá 2 MB.");
        else if (!AllowedCoverExtensions.Contains(Path.GetExtension(cover.FileName).ToLowerInvariant()))
            ModelState.AddModelError(nameof(SachFormViewModel.AnhBia), "Chỉ chấp nhận ảnh .jpg, .jpeg, .png hoặc .webp.");
    }

    private async Task<string> SaveCoverAsync(IFormFile cover, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(cover.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var directory = Path.Combine(environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot"),
            "images", "books");
        Directory.CreateDirectory(directory);
        var fullPath = Path.Combine(directory, fileName);
        await using var stream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await cover.CopyToAsync(stream, cancellationToken);
        return $"/images/books/{fileName}";
    }

    private void DeleteCoverFile(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
            return;
        var fileName = Path.GetFileName(imagePath);
        if (string.IsNullOrWhiteSpace(fileName))
            return;
        var fullPath = Path.Combine(environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot"),
            "images", "books", fileName);
        if (System.IO.File.Exists(fullPath))
            System.IO.File.Delete(fullPath);
    }
}
