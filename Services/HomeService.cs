// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Service trang chủ và tra cứu kho sách (LINQ chạy trong SQL: đếm, nhóm, lọc, sắp xếp, phân trang)

using Microsoft.EntityFrameworkCore;
using QuanLyThuVien_UNETI4_TI17A1CL.Data;
using QuanLyThuVien_UNETI4_TI17A1CL.Models;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Common;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;
using QuanLyThuVien_UNETI4_TI17A1CL.ViewModels;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services;

public sealed class HomeService : IHomeService
{
    private readonly AppDbContext _db;
    private readonly TimeProvider _time;

    public HomeService(AppDbContext db, TimeProvider time)
    {
        _db = db;
        _time = time;
    }

    /// <summary>
    /// Truy vấn gốc cho mọi danh sách sách: còn lại = SoLuongTong - đang giữ (Đã duyệt + Đang mượn) - đã mất.
    /// Biểu thức này được dùng lại để lọc "còn sách" và sắp xếp theo số lượng còn lại ngay trong SQL.
    /// </summary>
    private IQueryable<BookCardItem> Cards() =>
        _db.Sachs.AsNoTracking().Select(s => new BookCardItem
        {
            MaSach = s.MaSach,
            TenSach = s.TenSach,
            TacGia = s.TacGia,
            NamXuatBan = s.NamXuatBan,
            MaTheLoai = s.MaTheLoai,
            TenTheLoai = s.TheLoai!.TenTheLoai,
            SoLuongTong = s.SoLuongTong,
            GiaBia = s.GiaBia,
            ViTriKe = s.ViTriKe,
            TrangThai = s.TrangThai,
            ImgPath = s.ImgPath,
            DangGiu = s.PhieuMuons.Count(p =>
                p.TrangThai == TrangThaiPhieuMuon.DaDuyet || p.TrangThai == TrangThaiPhieuMuon.DangMuon),
            DaMat = s.PhieuMuons.Count(p =>
                p.TraSach != null && p.TraSach.TinhTrangSach == TinhTrangSach.Mat)
        });

    // ------------------------------------------------------------------ Trang chủ

    public async Task<LandingViewModel> GetLandingAsync(CancellationToken ct = default)
    {
        var vm = new LandingViewModel
        {
            CauHinh = await _db.CauHinhThuViens.AsNoTracking().FirstOrDefaultAsync(ct) ?? new CauHinhThuVien(),
            TongDauSach = await _db.Sachs.CountAsync(ct),
            TongBanSach = await _db.Sachs.SumAsync(s => (int?)s.SoLuongTong, ct) ?? 0,
            TongBanDoc = await _db.BanDocs.CountAsync(
                b => b.TrangThai == TrangThaiBanDoc.HoatDong || b.TrangThai == TrangThaiBanDoc.NgungHoatDong, ct),
            // Cùng định nghĩa với thống kê: một lượt mượn là một phiếu đã được giao (NgayMuon != null).
            TongLuotMuon = await _db.PhieuMuons.CountAsync(p => p.NgayMuon != null, ct)
        };

        vm.TheLoais = await _db.TheLoais.AsNoTracking()
            .Where(t => t.TrangThai == TrangThaiTheLoai.HoatDong)
            .OrderBy(t => t.TenTheLoai)
            .Select(t => new CategoryItem { MaTheLoai = t.MaTheLoai, TenTheLoai = t.TenTheLoai, SoDauSach = t.Sachs.Count() })
            .ToListAsync(ct);

        // Được mượn nhiều nhất: top 5 theo số lượt mượn.
        var top = await _db.PhieuMuons.AsNoTracking()
            .Where(p => p.NgayMuon != null)
            .GroupBy(p => p.MaSach)
            .Select(g => new { MaSach = g.Key, SoLuot = g.Count() })
            .OrderByDescending(x => x.SoLuot)
            .ThenBy(x => x.MaSach)
            .Take(5)
            .ToListAsync(ct);
        var topIds = top.Select(x => x.MaSach).ToList();
        var topBooks = await Cards().Where(b => topIds.Contains(b.MaSach)).ToListAsync(ct);
        vm.MuonNhieu = top
            .Select(x => new { x.SoLuot, Book = topBooks.FirstOrDefault(b => b.MaSach == x.MaSach) })
            .Where(x => x.Book is not null)
            .Select(x => new RankedBookItem { Book = x.Book!, SoLuotMuon = x.SoLuot })
            .ToList();
        vm.GoiY = vm.MuonNhieu.Take(4).Select(x => x.Book.TenSach).ToList();

        // Sách mới về: Sach không có ngày tạo nên dùng MaSach giảm dần.
        vm.SachMoi = await Cards().OrderByDescending(b => b.MaSach).Take(8).ToListAsync(ct);

        // Ba bìa xếp quạt ở hero: sách được mượn nhiều trước, thiếu thì lấy sách mới.
        vm.HeroBooks = vm.MuonNhieu.Select(x => x.Book).Concat(vm.SachMoi)
            .DistinctBy(b => b.MaSach).Take(3).ToList();

        // Tác giả nổi bật: nhiều lượt mượn nhất, rồi nhiều đầu sách nhất.
        var bookCounts = await _db.Sachs.AsNoTracking()
            .GroupBy(s => s.TacGia)
            .Select(g => new { Ten = g.Key, SoDauSach = g.Count() })
            .ToListAsync(ct);
        var loanCounts = await _db.PhieuMuons.AsNoTracking()
            .Where(p => p.NgayMuon != null)
            .GroupBy(p => p.Sach!.TacGia)
            .Select(g => new { Ten = g.Key, SoLuot = g.Count() })
            .ToListAsync(ct);
        var loanMap = loanCounts.ToDictionary(x => x.Ten, x => x.SoLuot, StringComparer.OrdinalIgnoreCase);
        vm.TacGiaNoiBat = bookCounts
            .Select(x => new AuthorItem
            {
                TenTacGia = x.Ten,
                SoDauSach = x.SoDauSach,
                SoLuotMuon = loanMap.GetValueOrDefault(x.Ten, 0)
            })
            .OrderByDescending(a => a.SoLuotMuon)
            .ThenByDescending(a => a.SoDauSach)
            .ThenBy(a => a.TenTacGia)
            .Take(8)
            .ToList();

        // Mỗi tác giả nổi bật kèm tối đa 3 đầu sách (2 truy vấn: lấy mã sách, rồi lấy thẻ sách).
        if (vm.TacGiaNoiBat.Count > 0)
        {
            var names = vm.TacGiaNoiBat.Select(a => a.TenTacGia).ToList();
            var idRows = await _db.Sachs.AsNoTracking()
                .Where(s => names.Contains(s.TacGia))
                .OrderByDescending(s => s.MaSach)
                .Select(s => new { s.MaSach, s.TacGia })
                .ToListAsync(ct);
            var pickedIds = idRows
                .GroupBy(x => x.TacGia, StringComparer.OrdinalIgnoreCase)
                .SelectMany(g => g.Take(3))
                .Select(x => x.MaSach)
                .ToList();
            var picked = await Cards().Where(b => pickedIds.Contains(b.MaSach)).ToListAsync(ct);
            foreach (var a in vm.TacGiaNoiBat)
                a.Books = picked
                    .Where(b => string.Equals(b.TacGia, a.TenTacGia, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(b => b.MaSach).Take(3).ToList();
        }

        // Sách của ngày: chọn cố định theo ngày trong số sách đang có thể mượn.
        var borrowable = Cards().Where(b =>
            b.TrangThai == TrangThaiSach.DangPhucVu && b.SoLuongTong - b.DangGiu - b.DaMat > 0);
        var count = await borrowable.CountAsync(ct);
        vm.SoDauSachSanSang = count;
        if (count > 0)
        {
            var today = DateOnly.FromDateTime(_time.GetLocalNow().DateTime);
            vm.SachCuaNgay = await borrowable.OrderBy(b => b.MaSach)
                .Skip(today.DayNumber % count).Take(1).FirstOrDefaultAsync(ct);
        }

        return vm;
    }

    // ------------------------------------------------------------------ Tra cứu

    public async Task<CatalogViewModel> GetCatalogAsync(CatalogFilter filter, int page, int pageSize, bool includeSidebar = true, CancellationToken ct = default)
    {
        var q = Cards();

        if (filter.Search is { } s)
            q = q.Where(b => b.TenSach.Contains(s) || b.TacGia.Contains(s) || b.TenTheLoai.Contains(s));
        if (filter.MaTheLoai is { } maTheLoai)
            q = q.Where(b => b.MaTheLoai == maTheLoai);
        if (filter.TacGia is { } tacGia)
            q = q.Where(b => b.TacGia == tacGia);
        if (filter.NamTu is { } namTu)
            q = q.Where(b => b.NamXuatBan >= namTu);
        if (filter.NamDen is { } namDen)
            q = q.Where(b => b.NamXuatBan <= namDen);
        if (filter.GiaTu is { } giaTu)
            q = q.Where(b => b.GiaBia >= giaTu);
        if (filter.GiaDen is { } giaDen)
            q = q.Where(b => b.GiaBia <= giaDen);
        if (filter.TrangThai is { } trangThai)
            q = q.Where(b => b.TrangThai == trangThai);

        if (filter.TonKho == TonKhoFilter.ConSach)
            q = q.Where(b => b.TrangThai == TrangThaiSach.DangPhucVu && b.SoLuongTong - b.DangGiu - b.DaMat > 0);
        else if (filter.TonKho == TonKhoFilter.HetSach)
            q = q.Where(b => b.SoLuongTong - b.DangGiu - b.DaMat <= 0);

        var ordered = filter.Sort switch
        {
            "za" => q.OrderByDescending(b => b.TenSach),
            "newest" => q.OrderByDescending(b => b.NamXuatBan),
            "oldest" => q.OrderBy(b => b.NamXuatBan),
            "stockdesc" => q.OrderByDescending(b => b.SoLuongTong - b.DangGiu - b.DaMat),
            "stockasc" => q.OrderBy(b => b.SoLuongTong - b.DangGiu - b.DaMat),
            "priceasc" => q.OrderBy(b => b.GiaBia),
            "pricedesc" => q.OrderByDescending(b => b.GiaBia),
            _ => q.OrderBy(b => b.TenSach)
        };
        ordered = ordered.ThenBy(b => b.TenSach).ThenBy(b => b.MaSach);

        var result = await PagedResult<BookCardItem>.CreateAsync(ordered, page, pageSize, ct);

        var vm = new CatalogViewModel
        {
            Filter = filter,
            Result = result,
            Pager = PagerViewModel.From(result, "Home", "Catalog", filter.ToRouteValues())
        };

        // Chip thể loại cần tên: một truy vấn nhỏ, luôn có.
        if (filter.MaTheLoai is { } maChon)
            vm.TenTheLoaiChon = await _db.TheLoais.AsNoTracking()
                .Where(t => t.MaTheLoai == maChon).Select(t => t.TenTheLoai).FirstOrDefaultAsync(ct);

        // Thanh bên (danh sách thể loại, tác giả) chỉ cần khi tải cả trang; yêu cầu AJAX bỏ qua.
        if (includeSidebar)
        {
            vm.TongDauSach = await _db.Sachs.CountAsync(ct);
            vm.TheLoais = await _db.TheLoais.AsNoTracking()
                .Where(t => t.TrangThai == TrangThaiTheLoai.HoatDong)
                .OrderBy(t => t.TenTheLoai)
                .Select(t => new CategoryItem { MaTheLoai = t.MaTheLoai, TenTheLoai = t.TenTheLoai, SoDauSach = t.Sachs.Count() })
                .ToListAsync(ct);
            vm.TacGias = await _db.Sachs.AsNoTracking()
                .Select(b => b.TacGia).Distinct().OrderBy(a => a).ToListAsync(ct);
        }

        return vm;
    }

    // ------------------------------------------------------------------ Chi tiết

    public async Task<BookDetailViewModel?> GetBookAsync(int maSach, CancellationToken ct = default)
    {
        var book = await Cards().Where(b => b.MaSach == maSach).FirstOrDefaultAsync(ct);
        if (book is null) return null;

        var extra = await _db.Sachs.AsNoTracking()
            .Where(s => s.MaSach == maSach)
            .Select(s => new { s.NhaXuatBan, s.MoTa, KhuVuc = s.TheLoai!.KhuVucLuuTru })
            .FirstAsync(ct);

        var related = await Cards()
            .Where(b => b.MaTheLoai == book.MaTheLoai && b.MaSach != maSach)
            .OrderByDescending(b => b.MaSach)
            .Take(6)
            .ToListAsync(ct);

        return new BookDetailViewModel
        {
            Book = book,
            NhaXuatBan = extra.NhaXuatBan,
            MoTa = extra.MoTa,
            KhuVucLuuTru = extra.KhuVuc,
            CungTheLoai = related
        };
    }
}
