// Họ và tên: Vũ Ngọc Hương Giang
// Mã sinh viên: 23103100280
// Nội dung thực hiện: Seeder gia hạn 13 yêu cầu và trả sách 16 bản ghi 

// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Sửa dữ liệu seed, mở rộng số lượng, đổi seed nghiệp vụ.

//NOTE: File này từng tên là DbSeeder.GiaHanMuonVaTraSach.cs

using Microsoft.EntityFrameworkCore;
using QuanLyThuVien_UNETI4_TI17A1CL.Models;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;
using QuanLyThuVien_UNETI4_TI17A1CL.Services;
using BookCond = QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums.TinhTrangSach;
using ExtState = QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums.TrangThaiGiaHan;
using LoanState = QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums.TrangThaiPhieuMuon;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Data;

// Kịch bản phiếu mượn được viết sẵn (không ngẫu nhiên) để mọi máy có cùng dữ liệu và đủ tình huống kiểm thử.
// Mọi con số ngày là SỐ NGÀY SO VỚI HÔM NAY lúc seed (âm = quá khứ, dương = tương lai).
public static partial class DbSeeder
{
    // Reader/Book: số thứ tự (ReaderUsers 1..30, SampleBooks 1..50). Due = hạn trả hiện tại (đã tính các lần gia hạn được duyệt).
    private sealed record LoanSeed(int Reader, int Book, LoanState Status, int Reg, int? Proc = null,
        int? Out = null, int? Due = null, int? End = null, string? Note = null, string? Remark = null);

    // Loan = số thứ tự phiếu trong SampleLoans (1..48).
    private sealed record ExtSeed(int Loan, ExtState Status, int OldDue, int NewDue, int Requested,
        int? Processed, string Reason, string? Remark = null);

    private sealed record ReturnSeed(int Loan, int Date, BookCond Cond, bool Paid, string? Note = null);

    private static async Task SeedLoansAsync(AppDbContext db, ILogger log)
    {
        // Phiếu mượn cùng ràng buộc số lượng/trùng phiếu nên chỉ seed khi chưa có phiếu nào.
        if (await db.PhieuMuons.AnyAsync()) return;

        var today = DateOnly.FromDateTime(DateTime.Today);
        DateOnly? DayN(int? offset) => offset is int o ? today.AddDays(o) : null;
        DateTime At(int offset, int hour, int salt) => today.AddDays(offset).ToDateTime(new TimeOnly(hour, salt * 7 % 60));

        var sachs = (await db.Sachs.AsNoTracking().ToListAsync())
            .GroupBy(s => s.TenSach).ToDictionary(g => g.Key, g => g.First());
        var readerIds = await db.BanDocs.AsNoTracking()
            .Select(b => new { b.MaBanDoc, User = b.TaiKhoan!.TenDangNhap })
            .ToDictionaryAsync(x => x.User, x => x.MaBanDoc);
        var accountIds = await db.TaiKhoans.AsNoTracking()
            .ToDictionaryAsync(t => t.TenDangNhap, t => t.MaTaiKhoan);

        var staffUsers = new[] { "thuthu", "thuthu2", "thuthu3" };
        var missing = ReaderUsers.Where(u => !readerIds.ContainsKey(u))
            .Concat(staffUsers.Where(u => !accountIds.ContainsKey(u)))
            .Concat(SampleBooks.Select(b => b.Title).Where(t => !sachs.ContainsKey(t)))
            .ToList();
        if (missing.Count > 0)
        {
            log.LogWarning("Bỏ qua seed phiếu mượn vì thiếu dữ liệu nền: {Missing}", string.Join(", ", missing));
            return;
        }
        //xóa hàm ktra ẩn khiến seeder không chạy

        // Kiểm tra số cuốn đang giữ + đã mất không vượt SoLuongTong hiện có trong CSDL.
        var load = new int[SampleBooks.Length + 1];
        foreach (var l in SampleLoans)
            if (l.Status is LoanState.DaDuyet or LoanState.DangMuon) load[l.Book]++;
        foreach (var r in SampleReturns)
            if (r.Cond == BookCond.Mat) load[SampleLoans[r.Loan - 1].Book]++;
        for (var b = 1; b <= SampleBooks.Length; b++)
        {
            if (load[b] <= sachs[SampleBooks[b - 1].Title].SoLuongTong) continue;
            log.LogWarning("Bỏ qua seed phiếu mượn: sách '{Title}' có SoLuongTong nhỏ hơn số cuốn kịch bản cần.", SampleBooks[b - 1].Title);
            return;
        }

        var cfg = await db.CauHinhThuViens.AsNoTracking().FirstOrDefaultAsync() ?? new CauHinhThuVien();
        var phatTre = new TienPhatTreHanService();
        var phatTinhTrang = new TienPhatTinhTrangSachService();
        int Staff(int index) => accountIds[staffUsers[index % staffUsers.Length]];

        //Thêm phiếu mượn + lưu lịch sử
        //xóa nextpair, cặp bạn đọc không cần thiết
        var loans = new List<PhieuMuon>();
        for (var i = 0; i < SampleLoans.Length; i++)
        {
            var s = SampleLoans[i];
            var user = ReaderUsers[s.Reader - 1];
            var reader = accountIds[user];
            var staff = Staff(i);

            var p = new PhieuMuon
            {
                MaBanDoc = readerIds[user],
                MaSach = sachs[SampleBooks[s.Book - 1].Title].MaSach,
                NgayDangKy = today.AddDays(s.Reg),
                NgayMuon = DayN(s.Out),
                HanTra = DayN(s.Due),
                GhiChu = s.Note,
                TrangThai = s.Status,
                NgayXuLy = DayN(s.Proc),
                NhanXetThuThu = s.Remark
            };

            void AddHistory(LoanState? from, LoanState to, int offset, int hour, int actor, string note) =>
                p.LichSus.Add(new PhieuMuonLichSu
                {
                    TrangThaiCu = from,
                    TrangThaiMoi = to,
                    NgayThucHien = At(offset, hour, i),
                    MaNguoiThucHien = actor,
                    GhiChu = note
                });

            AddHistory(null, LoanState.ChoDuyet, s.Reg, 9, reader, "Bạn đọc đăng ký mượn sách");
            if (s.Status is LoanState.DaDuyet or LoanState.DangMuon or LoanState.DaTra)
                AddHistory(LoanState.ChoDuyet, LoanState.DaDuyet, s.Proc!.Value, 10, staff, "Thủ thư duyệt phiếu");
            if (s.Status is LoanState.DangMuon or LoanState.DaTra)
                AddHistory(LoanState.DaDuyet, LoanState.DangMuon, s.Out!.Value, 14, staff, "Giao sách cho bạn đọc");
            if (s.Status == LoanState.DaTra)
            {
                var ret = SampleReturns.First(r => r.Loan == i + 1);
                AddHistory(LoanState.DangMuon, LoanState.DaTra, ret.Date, 15, staff, "Nhận trả sách");
            }
            if (s.Status == LoanState.TuChoi)
                AddHistory(LoanState.ChoDuyet, LoanState.TuChoi, s.Proc!.Value, 10, staff, "Thủ thư từ chối phiếu");
            if (s.Status == LoanState.DaHuy)
            {
                if (s.Proc is int approved)
                    AddHistory(LoanState.ChoDuyet, LoanState.DaDuyet, approved, 10, staff, "Thủ thư duyệt phiếu");
                AddHistory(s.Proc is null ? LoanState.ChoDuyet : LoanState.DaDuyet, LoanState.DaHuy,
                    s.End!.Value, 11, reader, "Bạn đọc hủy phiếu");
            }

            loans.Add(p);
        }

        // Thêm yêu cầu gia hạn theo thứ tự thời gian; duyệt xong thì hạn trả của phiếu = hạn trả mới.
        foreach (var e in SampleExtensions)
        {
            loans[e.Loan - 1].GiaHanMuons.Add(new GiaHanMuon
            {
                HanTraCu = today.AddDays(e.OldDue),
                HanTraMoi = today.AddDays(e.NewDue),
                LyDo = e.Reason,
                NgayYeuCau = today.AddDays(e.Requested),
                TrangThai = e.Status,
                NgayXuLy = DayN(e.Processed),
                NhanXet = e.Remark
            });
        }

        // Tiền phạt tính bằng đúng hai service nghiệp vụ, mức phạt lấy từ CauHinhThuVien.
        foreach (var r in SampleReturns)
        {
            var p = loans[r.Loan - 1];
            var giaBia = sachs[SampleBooks[SampleLoans[r.Loan - 1].Book - 1].Title].GiaBia;
            var ngayTra = today.AddDays(r.Date);
            var tre = phatTre.Tinh(cfg, p.HanTra, ngayTra);
            var tienPhat = tre.TienPhat + phatTinhTrang.TinhTienPhat(cfg, r.Cond, giaBia);

            p.TraSach = new TraSach
            {
                NgayTra = ngayTra,
                TinhTrangSach = r.Cond,
                SoNgayTre = tre.SoNgayTre,
                TienPhat = tienPhat,
                TrangThaiPhat = tienPhat <= 0 ? TrangThaiPhat.KhongPhat
                    : r.Paid ? TrangThaiPhat.DaNop : TrangThaiPhat.ChuaNop,
                GhiChu = r.Note,
                MaNguoiNhan = Staff(r.Loan - 1)
            };
        }

        db.PhieuMuons.AddRange(loans);
        await db.SaveChangesAsync();
        log.LogInformation("Đã seed {Loans} phiếu mượn, {Ext} yêu cầu gia hạn, {Ret} bản ghi trả sách.",
            SampleLoans.Length, SampleExtensions.Length, SampleReturns.Length);
    }

    // Sắp theo ngày đăng ký tăng dần. Chú thích cuối dòng: #số phiếu, số thứ tự bạn đọc, tên sách.
    private static readonly LoanSeed[] SampleLoans =
    {
        new(26, 38, LoanState.DaTra, -102, -101, -100, -70, Remark: "Đồng ý cho mượn"), // #01 R26 · SPQR
        new(15, 3, LoanState.DaTra, -92, -91, -90, -76, Remark: "Đồng ý cho mượn"), // #02 R15 · Pro ASP.NET Core
        new(20, 18, LoanState.DaTra, -82, -81, -80, -66, Remark: "Đồng ý cho mượn"), // #03 R20 · Code Complete
        new(1, 27, LoanState.DaTra, -75, -74, -73, -59, Remark: "Đồng ý cho mượn"), // #04 R01 · Tắt Đèn
        new(10, 9, LoanState.DaTra, -72, -71, -70, -56, Remark: "Đồng ý cho mượn"), // #05 R10 · Việt Nam Sử Lược
        new(13, 16, LoanState.DaTra, -72, -71, -70, -56, Remark: "Đồng ý cho mượn"), // #06 R13 · Introduction to Algorithms
        new(5, 15, LoanState.DaTra, -66, -65, -64, -50, Remark: "Đồng ý cho mượn"), // #07 R05 · Head First C#
        new(5, 43, LoanState.DaTra, -62, -61, -60, -39, Remark: "Đồng ý cho mượn"), // #08 R05 · Zero to One
        new(17, 22, LoanState.DaTra, -60, -59, -58, -44, Remark: "Đồng ý cho mượn"), // #09 R17 · The Gene: An Intimate History
        new(12, 11, LoanState.DaTra, -57, -56, -55, -41, Remark: "Đồng ý cho mượn"), // #10 R12 · Kinh Tế Học Hài Hước
        new(1, 12, LoanState.DaTra, -50, -49, -48, -34, Remark: "Đồng ý cho mượn"), // #11 R01 · Hoàng Tử Bé
        new(9, 8, LoanState.DaTra, -47, -46, -45, -26, Remark: "Đồng ý cho mượn"), // #12 R09 · Sapiens: Lược Sử Loài Người
        new(18, 50, LoanState.DaTra, -47, -46, -45, -31, Remark: "Đồng ý cho mượn"), // #13 R18 · Doraemon - Tập 1
        new(21, 34, LoanState.DangMuon, -42, -41, -40, -3, Remark: "Đồng ý cho mượn"), // #14 R21 · Homo Deus
        new(19, 29, LoanState.DaTra, -37, -36, -35, -21, Remark: "Đồng ý cho mượn"), // #15 R19 · Số Đỏ
        new(27, 40, LoanState.DangMuon, -32, -31, -30, -16, Remark: "Đồng ý cho mượn"), // #16 R27 · Rich Dad Poor Dad
        new(14, 1, LoanState.DaTra, -32, -31, -30, -16, Remark: "Đồng ý cho mượn"), // #17 R14 · C# in Depth
        new(1, 36, LoanState.DaHuy, -30, null, null, null, -29), // #18 R01 · The Silk Roads
        new(2, 6, LoanState.DangMuon, -24, -23, -22, -8, Note: "Mượn đọc trong kỳ nghỉ", Remark: "Đồng ý cho mượn"), // #19 R02 · Dế Mèn Phiêu Lưu Ký
        new(13, 18, LoanState.DangMuon, -20, -19, -19, -5, Remark: "Đã kiểm tra thẻ và tình trạng sách"), // #20 R13 · Code Complete
        new(1, 25, LoanState.TuChoi, -20, -19, Remark: "Sách đã ngừng phục vụ"), // #21 R01 · Silent Spring
        new(3, 5, LoanState.DaTra, -17, -16, -16, -2, Remark: "Đồng ý cho mượn"), // #22 R03 · A Brief History of Time
        new(9, 21, LoanState.TuChoi, -15, -14, Remark: "Thông tin liên hệ chưa đầy đủ, vui lòng cập nhật hồ sơ"), // #23 R09 · The Selfish Gene
        new(11, 21, LoanState.DangMuon, -12, -11, -10, 4, Remark: "Đồng ý cho mượn"), // #24 R11 · The Selfish Gene
        new(17, 48, LoanState.DaDuyet, -12, -10, Remark: "Đến quầy nhận sách trong 3 ngày"), // #25 R17 · Harry Potter and the Philosopher's Stone
        new(4, 14, LoanState.DangMuon, -11, -10, -9, 8, Remark: "Đồng ý cho mượn"), // #26 R04 · Design Patterns
        new(22, 4, LoanState.DangMuon, -11, -10, -9, 35, Remark: "Đồng ý cho mượn"), // #27 R22 · Cosmos
        new(12, 23, LoanState.TuChoi, -10, -9, Remark: "Thẻ bạn đọc đang bị tạm dừng"), // #28 R12 · Surely You're Joking, Mr. Feynman!
        new(6, 35, LoanState.DangMuon, -9, -8, -7, 7, Remark: "Đồng ý cho mượn"), // #29 R06 · Guns, Germs, and Steel
        new(1, 1, LoanState.DangMuon, -8, -7, -6, 15, Note: "Tham khảo cho đồ án môn học", Remark: "Đồng ý cho mượn"), // #30 R01 · C# in Depth
        new(14, 47, LoanState.DaHuy, -8, -7, null, null, -5, Remark: "Đồng ý cho mượn"), // #31 R14 · Matilda
        new(25, 13, LoanState.DangMuon, -7, -6, -5, 25, Remark: "Đồng ý cho mượn"), // #32 R25 · The Pragmatic Programmer
        new(10, 21, LoanState.DangMuon, -6, -5, -4, 10, Remark: "Đồng ý cho mượn"), // #33 R10 · The Selfish Gene
        new(4, 2, LoanState.DangMuon, -5, -4, -3, 11, Remark: "Đồng ý cho mượn"), // #34 R04 · Clean Code
        new(14, 21, LoanState.DaDuyet, -5, -4, Remark: "Đến quầy nhận sách trong 3 ngày"), // #35 R14 · The Selfish Gene
        new(4, 20, LoanState.DangMuon, -4, -3, -2, 12, Remark: "Đồng ý cho mượn"), // #36 R04 · Astrophysics for People in a Hurry
        new(7, 35, LoanState.DaDuyet, -4, -3, Remark: "Đến quầy nhận sách trong 3 ngày"), // #37 R07 · Guns, Germs, and Steel
        new(16, 3, LoanState.DangMuon, -3, -3, -2, 12, Remark: "Đồng ý cho mượn"), // #38 R16 · Pro ASP.NET Core
        new(1, 28, LoanState.DaDuyet, -3, -2, Remark: "Đến quầy nhận sách trong 3 ngày"), // #39 R01 · Chí Phèo
        new(4, 31, LoanState.DaDuyet, -3, -2, Remark: "Đến quầy nhận sách trong 3 ngày"), // #40 R04 · Cho Tôi Xin Một Vé Đi Tuổi Thơ
        new(3, 17, LoanState.ChoDuyet, -3, Note: "Tham khảo cho dự án nhóm"), // #41 R03 · Refactoring
        new(9, 35, LoanState.ChoDuyet, -2), // #42 R09 · Guns, Germs, and Steel
        new(8, 41, LoanState.ChoDuyet, -2), // #43 R08 · Principles of Economics
        new(23, 20, LoanState.ChoDuyet, -2, Note: "Phục vụ bài giảng"), // #44 R23 · Astrophysics for People in a Hurry
        new(1, 10, LoanState.ChoDuyet, -1, Note: "Mượn để ôn thi giữa kỳ"), // #45 R01 · Tư Duy Nhanh Và Chậm
        new(19, 45, LoanState.ChoDuyet, -1), // #46 R19 · Good to Great
        new(4, 7, LoanState.ChoDuyet, 0), // #47 R04 · Truyện Kiều
        new(30, 49, LoanState.ChoDuyet, 0), // #48 R30 · Nhóc Nicolas
    };

    private static readonly ExtSeed[] SampleExtensions =
    {
        new(2, ExtState.TuChoi, -76, -69, -80, -79, "Muốn mượn thêm", "Từ chối: sách có người đăng ký chờ"), // loan #02
        new(8, ExtState.DaDuyet, -46, -39, -52, -51, "Cần thêm thời gian đọc", "Đồng ý"), // loan #08
        new(12, ExtState.DaDuyet, -31, -26, -38, -37, "Cần thêm thời gian viết tiểu luận", "Đồng ý"), // loan #12
        new(14, ExtState.DaDuyet, -10, -3, -15, -14, "Cần thêm thời gian để hoàn thành giáo trình", "Đồng ý"), // loan #14
        new(24, ExtState.TuChoi, 4, 11, -4, -3, "Muốn đọc thêm một tuần", "Từ chối: vui lòng trả sách đúng hạn để phục vụ bạn đọc khác"), // loan #24
        new(26, ExtState.DaDuyet, 5, 8, -2, -1, "Cần thêm ít ngày", "Đồng ý"), // loan #26
        new(27, ExtState.DaDuyet, 21, 28, -3, -2, "Chuẩn bị bài giảng", "Đồng ý"), // loan #27
        new(27, ExtState.DaDuyet, 28, 35, -1, 0, "Cần thêm thời gian soạn giáo án", "Đồng ý (đã đủ 2 lần)"), // loan #27
        new(29, ExtState.ChoDuyet, 7, 12, -1, null, "Cần thêm thời gian ôn tập"), // loan #29
        new(30, ExtState.DaDuyet, 8, 15, -3, -2, "Chưa kịp hoàn thành đồ án", "Đồng ý"), // loan #30
        new(30, ExtState.ChoDuyet, 15, 20, -1, null, "Cần thêm vài ngày để viết báo cáo"), // loan #30
        new(34, ExtState.ChoDuyet, 11, 18, -1, null, "Cần thêm thời gian thực hành"), // loan #34
        new(38, ExtState.ChoDuyet, 12, 16, -1, null, "Chuẩn bị bảo vệ đồ án"), // loan #38
    };

    private static readonly ReturnSeed[] SampleReturns =
    {
        new(1, -66, BookCond.BinhThuong, false), // loan #01 · trễ 4 ngày · phạt 8,000
        new(2, -73, BookCond.BinhThuong, true), // loan #02 · trễ 3 ngày · phạt 6,000
        new(3, -65, BookCond.Mat, false), // loan #03 · trễ 1 ngày · phạt 542,000
        new(4, -59, BookCond.BinhThuong, true), // loan #04 · trễ 0 ngày · phạt 0
        new(5, -52, BookCond.BinhThuong, true), // loan #05 · trễ 4 ngày · phạt 8,000
        new(6, -52, BookCond.Mat, true), // loan #06 · trễ 4 ngày · phạt 728,000
        new(7, -50, BookCond.HuHong, false), // loan #07 · trễ 0 ngày · phạt 230,000
        new(8, -47, BookCond.BinhThuong, true), // loan #08 · trễ 0 ngày · phạt 0
        new(9, -41, BookCond.HuHong, true), // loan #09 · trễ 3 ngày · phạt 151,000
        new(10, -35, BookCond.BinhThuong, true), // loan #10 · trễ 6 ngày · phạt 12,000
        new(11, -36, BookCond.BinhThuong, true), // loan #11 · trễ 0 ngày · phạt 0
        new(12, -33, BookCond.BinhThuong, true), // loan #12 · trễ 0 ngày · phạt 0
        new(13, -29, BookCond.Mat, true), // loan #13 · trễ 2 ngày · phạt 29,000
        new(15, -22, BookCond.HuHong, true), // loan #15 · trễ 0 ngày · phạt 45,000
        new(17, -17, BookCond.BinhThuong, true), // loan #17 · trễ 0 ngày · phạt 0
        new(22, 0, BookCond.BinhThuong, false), // loan #22 · trễ 2 ngày · phạt 4,000
    };
}