// Họ và tên: Phùng Ngọc Bảo
// Mã sinh viên: 23103100278
// Nội dung thực hiện: Dữ liệu mẫu phiếu mượn và lịch sử trạng thái Module 4

using Microsoft.EntityFrameworkCore;
using QuanLyThuVien_UNETI4_TI17A1CL.Models;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Data;

public static partial class DbSeeder
{
    private const int SoPhieuMauMucTieu = 47;
    private const string GhiChuSeedModule4 = "Dữ liệu mẫu Module 4";

    private static async Task SeedQuanLyPhieuMuonAsync(AppDbContext db)
    {
        var soGiaHan = await db.GiaHanMuons.CountAsync();
        var soTraSach = await db.TraSachs.CountAsync();
        var existingLoans = await db.PhieuMuons
            .Include(p => p.TraSach)
            .Include(p => p.LichSus)
            .OrderBy(p => p.MaPhieuMuon)
            .ToListAsync();

        if (soGiaHan < 10 || soTraSach < 15 || existingLoans.Count < 30)
        {
            Console.WriteLine(
                "[DbSeeder] Bỏ qua seed Module 4: cần seed Module 5 trước (>=10 gia hạn, >=15 trả sách, >=30 phiếu). " +
                "Kiểm tra seed đầu sách/bạn đọc và DbSeeder.SeedAsync.");
            return;
        }

        var staff = await db.TaiKhoans
            .Where(t => t.VaiTro == VaiTro.ThuThu || t.VaiTro == VaiTro.Admin)
            .OrderBy(t => t.MaTaiKhoan)
            .FirstOrDefaultAsync();

        if (staff is null)
        {
            Console.WriteLine("[DbSeeder] Bỏ qua seed Module 4: chưa có tài khoản Admin/Thủ thư.");
            return;
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        var readers = await db.BanDocs
            .Where(b => b.TrangThai == TrangThaiBanDoc.HoatDong && b.NgayHetHanThe >= today)
            .OrderBy(b => b.MaBanDoc)
            .ToListAsync();
        var books = await db.Sachs
            .Where(s => s.TrangThai == TrangThaiSach.DangPhucVu)
            .OrderBy(s => s.MaSach)
            .ToListAsync();

        if (readers.Count < 5 || books.Count < 8)
        {
            Console.WriteLine("[DbSeeder] Bỏ qua seed Module 4: cần tối thiểu 5 bạn đọc hợp lệ và 8 sách Đang phục vụ.");
            return;
        }

        // Bổ sung lịch sử cho phiếu mẫu từ seeder Module 5, nhưng không tự tạo lịch sử
        // giả cho phiếu nghiệp vụ thật (chỉ xử lý những dòng đã được đánh dấu mẫu)
        foreach (var p in existingLoans.Where(p =>
                     p.LichSus.Count == 0 && p.NhanXetThuThu == "Dữ liệu mẫu"))
        {
            ThemLichSuChoPhieuMauCu(db, p, staff.MaTaiKhoan);
        }

        // Chạy lặp an toàn: nếu đã có bản ghi seed Module 4 thì chỉ lưu lịch sử còn thiếu
        if (existingLoans.Any(p => p.GhiChu == GhiChuSeedModule4))
        {
            await db.SaveChangesAsync();
            return;
        }

        var soPhieuCanThem = Math.Max(0, SoPhieuMauMucTieu - existingLoans.Count);
        if (soPhieuCanThem == 0)
        {
            await db.SaveChangesAsync();
            Console.WriteLine($"[DbSeeder] Đã có {existingLoans.Count} phiếu, không cần bổ sung mẫu Module 4.");
            return;
        }

        var activeStatuses = new[]
        {
            TrangThaiPhieuMuon.ChoDuyet,
            TrangThaiPhieuMuon.DaDuyet,
            TrangThaiPhieuMuon.DangMuon
        };
        var activePairs = existingLoans
            .Where(p => activeStatuses.Contains(p.TrangThai))
            .Select(p => (p.MaBanDoc, p.MaSach))
            .ToHashSet();
        var heldByBook = existingLoans
            .Where(p => p.TrangThai == TrangThaiPhieuMuon.DaDuyet || p.TrangThai == TrangThaiPhieuMuon.DangMuon)
            .GroupBy(p => p.MaSach)
            .ToDictionary(g => g.Key, g => g.Count());
        var lostByBook = existingLoans
            .Where(p => p.TrangThai == TrangThaiPhieuMuon.DaTra && p.TraSach?.TinhTrangSach == TinhTrangSach.Mat)
            .GroupBy(p => p.MaSach)
            .ToDictionary(g => g.Key, g => g.Count());
        var remainingByBook = books.ToDictionary(
            s => s.MaSach,
            s => s.SoLuongTong - heldByBook.GetValueOrDefault(s.MaSach) - lostByBook.GetValueOrDefault(s.MaSach));

        // Seeder Module 5 đã tạo mẫu DangMuon/DaTra. Mẫu Module 4 bổ sung đều các trạng thái
        // Chờ duyệt, Đã duyệt, Từ chối và Đã hủy để danh sách/chi tiết có đủ ca kiểm thử
        var plannedStatuses = new[]
        {
            TrangThaiPhieuMuon.ChoDuyet,
            TrangThaiPhieuMuon.DaDuyet,
            TrangThaiPhieuMuon.TuChoi,
            TrangThaiPhieuMuon.DaHuy
        };

        var newLoans = new List<PhieuMuon>();
        var newHistories = new List<PhieuMuonLichSu>();
        var usedTerminalPairs = new HashSet<(int MaBanDoc, int MaSach)>();

        for (var i = 0; i < soPhieuCanThem; i++)
        {
            var status = plannedStatuses[i % plannedStatuses.Length];
            var needsActivePair = activeStatuses.Contains(status);
            (BanDoc Reader, Sach Book)? selected = null;

            // Ưu tiên sách còn bản; trạng thái Đã duyệt giữ chỗ một cuốn sách
            foreach (var reader in readers)
            {
                foreach (var book in books.OrderByDescending(b => remainingByBook[b.MaSach]))
                {
                    var pair = (reader.MaBanDoc, book.MaSach);
                    if (needsActivePair && activePairs.Contains(pair))
                        continue;
                    if (!needsActivePair && (activePairs.Contains(pair) || usedTerminalPairs.Contains(pair)))
                        continue;
                    if (status == TrangThaiPhieuMuon.DaDuyet && remainingByBook[book.MaSach] <= 0)
                        continue;

                    selected = (reader, book);
                    if (needsActivePair)
                        activePairs.Add(pair);
                    else
                        usedTerminalPairs.Add(pair);
                    if (status == TrangThaiPhieuMuon.DaDuyet)
                        remainingByBook[book.MaSach]--;
                    break;
                }
                if (selected.HasValue) break;
            }

            if (selected is null)
            {
                Console.WriteLine("[DbSeeder] Không đủ cặp bạn đọc/sách để seed mẫu Module 4; không tạo phiếu bổ sung.");
                // Lịch sử cho phiếu Module 5 vẫn có thể lưu, nhưng không seed một phần batch.
                await db.SaveChangesAsync();
                return;
            }

            var readerSelected = selected.Value.Reader;
            var bookSelected = selected.Value.Book;
            var registered = today.AddDays(-(2 + (i % 18)));
            var processed = registered.AddDays(1);
            var note = status switch
            {
                TrangThaiPhieuMuon.DaDuyet => "Đã duyệt - dữ liệu mẫu Module 4.",
                TrangThaiPhieuMuon.TuChoi => "Từ chối mẫu để kiểm thử luồng trạng thái.",
                TrangThaiPhieuMuon.DaHuy => "Hủy mẫu để kiểm thử luồng trạng thái.",
                _ => null
            };

            var loan = new PhieuMuon
            {
                MaBanDoc = readerSelected.MaBanDoc,
                MaSach = bookSelected.MaSach,
                NgayDangKy = registered,
                NgayMuon = null,
                HanTra = null,
                GhiChu = GhiChuSeedModule4,
                TrangThai = status,
                NgayXuLy = status == TrangThaiPhieuMuon.ChoDuyet ? null : processed,
                NhanXetThuThu = note
            };
            newLoans.Add(loan);

            newHistories.Add(new PhieuMuonLichSu
            {
                PhieuMuon = loan,
                TrangThaiCu = null,
                TrangThaiMoi = TrangThaiPhieuMuon.ChoDuyet,
                NgayThucHien = At(registered, 8),
                MaNguoiThucHien = staff.MaTaiKhoan,
                GhiChu = "Đăng ký mượn (dữ liệu mẫu)."
            });

            if (status != TrangThaiPhieuMuon.ChoDuyet)
            {
                newHistories.Add(new PhieuMuonLichSu
                {
                    PhieuMuon = loan,
                    TrangThaiCu = TrangThaiPhieuMuon.ChoDuyet,
                    TrangThaiMoi = status,
                    NgayThucHien = At(processed, 9),
                    MaNguoiThucHien = staff.MaTaiKhoan,
                    GhiChu = note
                });
            }
        }

        db.PhieuMuons.AddRange(newLoans);
        db.PhieuMuonLichSus.AddRange(newHistories);
        await db.SaveChangesAsync();
        Console.WriteLine($"[DbSeeder] Đã seed {newLoans.Count} phiếu Module 4; tổng hiện có {existingLoans.Count + newLoans.Count} phiếu.");
    }

    private static void ThemLichSuChoPhieuMauCu(AppDbContext db, PhieuMuon p, int maNguoiThucHien)
    {
        var ngayDangKy = At(p.NgayDangKy, 8);
        AddHistory(db, p, null, TrangThaiPhieuMuon.ChoDuyet, ngayDangKy, maNguoiThucHien,
            "Đăng ký mượn (dữ liệu mẫu Module 5).");

        var ngayXuLy = At(p.NgayXuLy ?? p.NgayDangKy.AddDays(1), 9);
        switch (p.TrangThai)
        {
            case TrangThaiPhieuMuon.ChoDuyet:
                break;
            case TrangThaiPhieuMuon.DaDuyet:
                AddHistory(db, p, TrangThaiPhieuMuon.ChoDuyet, TrangThaiPhieuMuon.DaDuyet,
                    ngayXuLy, maNguoiThucHien, "Duyệt phiếu (dữ liệu mẫu Module 5).");
                break;
            case TrangThaiPhieuMuon.DangMuon:
                AddHistory(db, p, TrangThaiPhieuMuon.ChoDuyet, TrangThaiPhieuMuon.DaDuyet,
                    ngayXuLy, maNguoiThucHien, "Duyệt phiếu (dữ liệu mẫu Module 5).");
                if (p.NgayMuon is { } ngayMuon)
                    AddHistory(db, p, TrangThaiPhieuMuon.DaDuyet, TrangThaiPhieuMuon.DangMuon,
                        At(ngayMuon, 10), maNguoiThucHien, "Giao sách (dữ liệu mẫu Module 5).");
                break;
            case TrangThaiPhieuMuon.DaTra:
                AddHistory(db, p, TrangThaiPhieuMuon.ChoDuyet, TrangThaiPhieuMuon.DaDuyet,
                    ngayXuLy, maNguoiThucHien, "Duyệt phiếu (dữ liệu mẫu Module 5).");
                if (p.NgayMuon is { } ngayMuonDaTra)
                    AddHistory(db, p, TrangThaiPhieuMuon.DaDuyet, TrangThaiPhieuMuon.DangMuon,
                        At(ngayMuonDaTra, 10), maNguoiThucHien, "Giao sách (dữ liệu mẫu Module 5).");
                var ngayTra = p.TraSach?.NgayTra ?? p.HanTra ?? p.NgayDangKy;
                AddHistory(db, p, TrangThaiPhieuMuon.DangMuon, TrangThaiPhieuMuon.DaTra,
                    At(ngayTra, 15), maNguoiThucHien, "Nhận trả sách (dữ liệu mẫu Module 5).");
                break;
            case TrangThaiPhieuMuon.TuChoi:
                AddHistory(db, p, TrangThaiPhieuMuon.ChoDuyet, TrangThaiPhieuMuon.TuChoi,
                    ngayXuLy, maNguoiThucHien, p.NhanXetThuThu);
                break;
            case TrangThaiPhieuMuon.DaHuy:
                AddHistory(db, p, TrangThaiPhieuMuon.ChoDuyet, TrangThaiPhieuMuon.DaHuy,
                    ngayXuLy, maNguoiThucHien, "Hủy phiếu (dữ liệu mẫu Module 5).");
                break;
        }
    }

    private static void AddHistory(
        AppDbContext db,
        PhieuMuon p,
        TrangThaiPhieuMuon? from,
        TrangThaiPhieuMuon to,
        DateTime at,
        int maNguoiThucHien,
        string? ghiChu)
    {
        db.PhieuMuonLichSus.Add(new PhieuMuonLichSu
        {
            PhieuMuon = p,
            TrangThaiCu = from,
            TrangThaiMoi = to,
            NgayThucHien = at,
            MaNguoiThucHien = maNguoiThucHien,
            GhiChu = ghiChu
        });
    }

    private static DateTime At(DateOnly date, int hour) => date.ToDateTime(new TimeOnly(hour, 0));
}
