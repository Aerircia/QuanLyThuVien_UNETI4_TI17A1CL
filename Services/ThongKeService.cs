// Họ và tên: Phùng Ngọc Bảo
// Mã sinh viên: 23103100278
// Nội dung thực hiện: Service truy vấn LINQ cho 9 chỉ số Dashboard và 11 nhóm thống kê

using Microsoft.EntityFrameworkCore;
using QuanLyThuVien_UNETI4_TI17A1CL.Data;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services;

public sealed class ThongKeService : IThongKeService
{
    private readonly AppDbContext _db;
    private readonly TimeProvider _time;

    public ThongKeService(AppDbContext db, TimeProvider time)
    {
        _db = db;
        _time = time;
    }

    private DateOnly Today => DateOnly.FromDateTime(_time.GetLocalNow().DateTime);

    public async Task<DashboardSummary> GetDashboardAsync(CancellationToken ct = default)
    {
        var today = Today;
        var firstDay = new DateOnly(today.Year, today.Month, 1);
        var nextMonth = firstDay.AddMonths(1);

        var tongTheLoai = await _db.TheLoais.CountAsync(ct);
        var tongDauSach = await _db.Sachs.CountAsync(ct);
        var tongCuonSach = await _db.Sachs.SumAsync(s => (int?)s.SoLuongTong, ct) ?? 0;
        var tongBanDoc = await _db.BanDocs.CountAsync(ct);
        var tongPhieuMuon = await _db.PhieuMuons.CountAsync(ct);
        var choDuyet = await _db.PhieuMuons.CountAsync(
            p => p.TrangThai == TrangThaiPhieuMuon.ChoDuyet, ct);
        var dangMuon = await _db.PhieuMuons.CountAsync(
            p => p.TrangThai == TrangThaiPhieuMuon.DangMuon, ct);
        var quaHan = await _db.PhieuMuons.CountAsync(p =>
            p.TrangThai == TrangThaiPhieuMuon.DangMuon &&
            p.HanTra.HasValue && p.HanTra.Value < today, ct);
        var tienPhatThangNay = await _db.TraSachs
            .Where(t => t.NgayTra >= firstDay && t.NgayTra < nextMonth)
            .SumAsync(t => (decimal?)t.TienPhat, ct) ?? 0m;

        return new DashboardSummary(
            tongTheLoai,
            tongDauSach,
            tongCuonSach,
            tongBanDoc,
            tongPhieuMuon,
            choDuyet,
            dangMuon,
            quaHan,
            tienPhatThangNay);
    }

    public async Task<ThongKeReport> GetReportAsync(CancellationToken ct = default)
    {
        var today = Today;
        var firstMonth = new DateOnly(today.Year, today.Month, 1).AddMonths(-11);
        var nextMonth = firstMonth.AddMonths(12);

        // 1. Số đầu sách theo thể loại, kể cả thể loại chưa có đầu sách
        var byCategory = await _db.TheLoais
            .AsNoTracking()
            .OrderBy(t => t.TenTheLoai)
            .Select(t => new ThongKeTheLoaiItem(
                t.MaTheLoai, t.TenTheLoai, t.Sachs.Count()))
            .ToListAsync(ct);

        // Định nghĩa thống nhất: một lượt mượn là một phiếu đã được giao (NgayMuon != null)
        var borrowed = _db.PhieuMuons.AsNoTracking().Where(p => p.NgayMuon != null);

        // 2. Số lượt mượn theo từng đầu sách.
        var byBook = await borrowed
            .GroupBy(p => new { p.MaSach, TenSach = p.Sach!.TenSach })
            .Select(g => new ThongKeSachItem(g.Key.MaSach, g.Key.TenSach, g.Count()))
            .OrderByDescending(x => x.SoLuot)
            .ThenBy(x => x.TenSach)
            .ToListAsync(ct);

        // 3. Số phiếu theo trạng thái; trạng thái không có dữ liệu vẫn hiện 0
        var statusRaw = await _db.PhieuMuons
            .AsNoTracking()
            .GroupBy(p => p.TrangThai)
            .Select(g => new { TrangThai = g.Key, SoPhieu = g.Count() })
            .ToListAsync(ct);
        var statusMap = statusRaw.ToDictionary(x => x.TrangThai, x => x.SoPhieu);
        var byStatus = Enum.GetValues<TrangThaiPhieuMuon>()
            .Select(s => new ThongKeTrangThaiItem(
                s.GetDisplayName(), statusMap.GetValueOrDefault(s, 0)))
            .ToList();

        // 4. Top 10 đầu sách được mượn nhiều nhất
        var top10 = byBook.Take(10).ToList();

        // 5. Bạn đọc mượn nhiều nhất (hiển thị top 10 để dễ so sánh)
        var byReader = await borrowed
            .GroupBy(p => new { p.MaBanDoc, HoTen = p.BanDoc!.HoTen })
            .Select(g => new ThongKeBanDocItem(g.Key.MaBanDoc, g.Key.HoTen, g.Count()))
            .OrderByDescending(x => x.SoLuot)
            .ThenBy(x => x.HoTen)
            .Take(10)
            .ToListAsync(ct);

        // 6. Lượt mượn theo tháng trong 12 tháng gần nhất
        var borrowByMonthRaw = await borrowed
            .Where(p => p.NgayMuon!.Value >= firstMonth && p.NgayMuon.Value < nextMonth)
            .GroupBy(p => new { Nam = p.NgayMuon!.Value.Year, Thang = p.NgayMuon.Value.Month })
            .Select(g => new { g.Key.Nam, g.Key.Thang, SoLuong = g.Count() })
            .ToListAsync(ct);
        var borrowMonthMap = borrowByMonthRaw.ToDictionary(
            x => (x.Nam, x.Thang), x => x.SoLuong);

        // 7 và 8. Số lượt trả và tiền phạt theo tháng
        var returnByMonthRaw = await _db.TraSachs
            .AsNoTracking()
            .Where(t => t.NgayTra >= firstMonth && t.NgayTra < nextMonth)
            .GroupBy(t => new { Nam = t.NgayTra.Year, Thang = t.NgayTra.Month })
            .Select(g => new
            {
                g.Key.Nam,
                g.Key.Thang,
                SoLuotTra = g.Count(),
                TienPhat = g.Sum(t => t.TienPhat)
            })
            .ToListAsync(ct);
        var returnMonthMap = returnByMonthRaw.ToDictionary(
            x => (x.Nam, x.Thang), x => x.SoLuotTra);
        var fineMonthMap = returnByMonthRaw.ToDictionary(
            x => (x.Nam, x.Thang), x => x.TienPhat);

        // Điền cả những tháng chưa có dữ liệu bằng 0
        var borrowByMonth = new List<ThongKeTheoThangItem>();
        var returnByMonth = new List<ThongKeTheoThangItem>();
        var fineByMonth = new List<ThongKeTienPhatTheoThangItem>();
        for (var i = 0; i < 12; i++)
        {
            var month = firstMonth.AddMonths(i);
            var key = (month.Year, month.Month);
            var label = month.ToString("yyyy-MM");
            borrowByMonth.Add(new ThongKeTheoThangItem(
                month.Year, month.Month, label, borrowMonthMap.GetValueOrDefault(key, 0)));
            returnByMonth.Add(new ThongKeTheoThangItem(
                month.Year, month.Month, label, returnMonthMap.GetValueOrDefault(key, 0)));
            fineByMonth.Add(new ThongKeTienPhatTheoThangItem(
                month.Year, month.Month, label, fineMonthMap.GetValueOrDefault(key, 0m)));
        }

        // 9. Số yêu cầu gia hạn đã được duyệt theo đầu sách
        var byExtension = await _db.GiaHanMuons
            .AsNoTracking()
            .Where(g => g.TrangThai == TrangThaiGiaHan.DaDuyet)
            .GroupBy(g => new
            {
                MaSach = g.PhieuMuon!.MaSach,
                TenSach = g.PhieuMuon!.Sach!.TenSach
            })
            .Select(g => new ThongKeSachItem(g.Key.MaSach, g.Key.TenSach, g.Count()))
            .OrderByDescending(x => x.SoLuot)
            .ThenBy(x => x.TenSach)
            .ToListAsync(ct);

        // 10. Tỷ lệ phiếu trả trễ trên tổng số bản ghi trả
        var tongTra = await _db.TraSachs.CountAsync(ct);
        var traTre = await _db.TraSachs.CountAsync(t => t.SoNgayTre > 0, ct);
        var tyLeTraTre = tongTra == 0
            ? 0m
            : Math.Round(traTre * 100m / tongTra, 1);

        // 11. Tỷ lệ số cuốn đang mượn trên tổng số cuốn theo thể loại
        var stockRaw = await _db.Sachs
            .AsNoTracking()
            .GroupBy(s => new { s.MaTheLoai, TenTheLoai = s.TheLoai!.TenTheLoai })
            .Select(g => new
            {
                g.Key.MaTheLoai,
                g.Key.TenTheLoai,
                TongSoCuon = g.Sum(s => s.SoLuongTong)
            })
            .ToListAsync(ct);
        var borrowedStockRaw = await _db.PhieuMuons
            .AsNoTracking()
            .Where(p => p.TrangThai == TrangThaiPhieuMuon.DangMuon)
            .GroupBy(p => new { p.Sach!.MaTheLoai })
            .Select(g => new { g.Key.MaTheLoai, SoCuonDangMuon = g.Count() })
            .ToListAsync(ct);
        var stockMap = stockRaw.ToDictionary(x => x.MaTheLoai, x => x.TongSoCuon);
        var borrowedStockMap = borrowedStockRaw.ToDictionary(x => x.MaTheLoai, x => x.SoCuonDangMuon);
        var loanRatioByCategory = byCategory.Select(category =>
        {
            var total = stockMap.GetValueOrDefault(category.MaTheLoai, 0);
            var currentlyBorrowed = borrowedStockMap.GetValueOrDefault(category.MaTheLoai, 0);
            var ratio = total == 0 ? 0m : Math.Round(currentlyBorrowed * 100m / total, 1);
            return new ThongKeTyLeMuonTheoTheLoaiItem(
                category.MaTheLoai, category.TenTheLoai, currentlyBorrowed, total, ratio);
        }).ToList();

        return new ThongKeReport(
            byCategory,
            byBook,
            byStatus,
            top10,
            byReader,
            borrowByMonth,
            returnByMonth,
            fineByMonth,
            byExtension,
            tyLeTraTre,
            loanRatioByCategory);
    }
}
