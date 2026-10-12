// Họ và tên: Vũ Ngọc Hương Giang
// Mã sinh viên: 23103100280
// Nội dung thực hiện: Seeder gia hạn 13 yêu cầu và trả sách 16 bản ghi 

using Microsoft.EntityFrameworkCore;
using QuanLyThuVien_UNETI4_TI17A1CL.Models;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;
using QuanLyThuVien_UNETI4_TI17A1CL.Services;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Data;
public static partial class DbSeeder
{
    private static async Task SeedGiaHanMuonVaTraSachAsync(AppDbContext db)
    {
        if (await db.GiaHanMuons.AnyAsync() || await db.TraSachs.AnyAsync())
            return;

        var staff = await db.TaiKhoans
            .Where(t => t.VaiTro == VaiTro.ThuThu || t.VaiTro == VaiTro.Admin)
            .OrderBy(t => t.MaTaiKhoan)
            .FirstOrDefaultAsync();
        if (staff is null)
            return;

        var today = DateOnly.FromDateTime(DateTime.Today);

        var sachs = await db.Sachs
            .Where(s => s.TrangThai == TrangThaiSach.DangPhucVu)
            .OrderBy(s => s.MaSach)
            .ToListAsync();
        var banDocs = await db.BanDocs
            .Where(b => b.TrangThai == TrangThaiBanDoc.HoatDong && b.NgayHetHanThe > today)
            .OrderBy(b => b.MaBanDoc)
            .ToListAsync();
        if (sachs.Count < 8 || banDocs.Count < 5)
            return;  

        var cfg = await db.CauHinhThuViens.AsNoTracking().FirstOrDefaultAsync() ?? new CauHinhThuVien();
        var phatTre = new TienPhatTreHanService();
        var phatTinhTrang = new TienPhatTinhTrangSachService();

        // Cặp (bạn đọc, sách) đang xử lý "mỗi bạn đọc một phiếu đang xử lý cho một sách".
        var dangXuLy = new[] { TrangThaiPhieuMuon.ChoDuyet, TrangThaiPhieuMuon.DaDuyet, TrangThaiPhieuMuon.DangMuon };
        var used = (await db.PhieuMuons
                .Where(p => dangXuLy.Contains(p.TrangThai))
                .Select(p => new { p.MaBanDoc, p.MaSach })
                .ToListAsync())
            .Select(x => (x.MaBanDoc, x.MaSach))
            .ToHashSet();

        // Số cuốn còn lại của từng sách
        var dangGiu = await db.PhieuMuons
            .Where(p => p.TrangThai == TrangThaiPhieuMuon.DaDuyet || p.TrangThai == TrangThaiPhieuMuon.DangMuon)
            .GroupBy(p => p.MaSach)
            .Select(g => new { MaSach = g.Key, SoLuong = g.Count() })
            .ToDictionaryAsync(x => x.MaSach, x => x.SoLuong);
        var conLai = sachs.ToDictionary(s => s.MaSach, s => s.SoLuongTong - dangGiu.GetValueOrDefault(s.MaSach));

        var idx = 0;

        // active: phiếu đang xử lý ; giuSach: phiếu chiếm một cuốn : đang mượn hoặc sách mất.
        (BanDoc BanDoc, Sach Sach) NextPair(bool active, bool giuSach)
        {
            for (var n = 0; n < banDocs.Count * sachs.Count; n++, idx++)
            {
                var bd = banDocs[idx % banDocs.Count];
                var s = sachs[(idx / banDocs.Count) % sachs.Count];
                if (active && used.Contains((bd.MaBanDoc, s.MaSach))) continue;
                if (giuSach && conLai[s.MaSach] <= 0) continue;

                if (active) used.Add((bd.MaBanDoc, s.MaSach));
                if (giuSach) conLai[s.MaSach]--;
                idx++;
                return (bd, s);
            }
            throw new InvalidOperationException("Không đủ cặp bạn đọc/sách còn trống để seed Module 5.");
        }

        PhieuMuon NewPhieu(DateOnly ngayMuon, DateOnly hanTra, TrangThaiPhieuMuon trangThai, bool giuSach)
        {
            var (bd, s) = NextPair(active: trangThai == TrangThaiPhieuMuon.DangMuon, giuSach: giuSach);
            var p = new PhieuMuon
            {
                BanDoc = bd,
                Sach = s,
                NgayDangKy = ngayMuon.AddDays(-2),
                NgayMuon = ngayMuon,
                HanTra = hanTra,
                TrangThai = trangThai,
                NgayXuLy = ngayMuon.AddDays(-1),
                NhanXetThuThu = "Dữ liệu mẫu"
            };
            db.PhieuMuons.Add(p);
            return p;
        }

        // Thêm yêu cầu gia hạn theo thứ tự thời gian; duyệt xong thì hạn trả của phiếu = hạn trả mới.
        void GiaHan(PhieuMuon p, int soNgay, TrangThaiGiaHan trangThai, string lyDo, int ngayYeuCauTruoc, string? nhanXet = null)
        {
            var hanCu = p.HanTra!.Value;
            var hanMoi = hanCu.AddDays(soNgay);
            var ngayYeuCau = today.AddDays(-ngayYeuCauTruoc);
            db.GiaHanMuons.Add(new GiaHanMuon
            {
                PhieuMuon = p,
                HanTraCu = hanCu,
                HanTraMoi = hanMoi,
                LyDo = lyDo,
                NgayYeuCau = ngayYeuCau,
                TrangThai = trangThai,
                NgayXuLy = trangThai == TrangThaiGiaHan.ChoDuyet ? null : ngayYeuCau,
                NhanXet = nhanXet
            });
            if (trangThai == TrangThaiGiaHan.DaDuyet)
                p.HanTra = hanMoi;
        }

        try
        {
            // 10 phiếu đang mượn còn hạn, có 13 yêu cầu gia hạn
            PhieuMuon PhieuA() => NewPhieu(today.AddDays(-5), today.AddDays(9), TrangThaiPhieuMuon.DangMuon, giuSach: true);

            GiaHan(PhieuA(), 7, TrangThaiGiaHan.ChoDuyet, "Cần thêm thời gian làm đồ án", 1);
            GiaHan(PhieuA(), 3, TrangThaiGiaHan.ChoDuyet, "Chưa đọc xong", 0);
            GiaHan(PhieuA(), 5, TrangThaiGiaHan.ChoDuyet, "Ôn thi cuối kỳ", 2);
            GiaHan(PhieuA(), 7, TrangThaiGiaHan.DaDuyet, "Viết báo cáo", 3, "Đồng ý");

            var a5 = PhieuA();
            GiaHan(a5, 7, TrangThaiGiaHan.DaDuyet, "Cần thêm thời gian", 4, "Đồng ý");
            GiaHan(a5, 7, TrangThaiGiaHan.DaDuyet, "Vẫn chưa xong", 1, "Đồng ý (đã đủ 2 lần)");

            GiaHan(PhieuA(), 7, TrangThaiGiaHan.TuChoi, "Muốn mượn thêm", 2, "Sách có người chờ mượn");

            var a7 = PhieuA();
            GiaHan(a7, 5, TrangThaiGiaHan.DaDuyet, "Đi công tác", 3, "Đồng ý");
            GiaHan(a7, 7, TrangThaiGiaHan.ChoDuyet, "Xin thêm một tuần", 0);

            PhieuA();  

            var a9 = PhieuA();
            GiaHan(a9, 7, TrangThaiGiaHan.TuChoi, "Gia hạn dài", 4, "Vui lòng gia hạn ngắn hơn");
            GiaHan(a9, 7, TrangThaiGiaHan.DaDuyet, "Gia hạn lại", 3, "Đồng ý");
            GiaHan(a9, 3, TrangThaiGiaHan.DaDuyet, "Cần thêm ít ngày", 1, "Đồng ý (đã đủ 2 lần)");

            GiaHan(PhieuA(), 4, TrangThaiGiaHan.ChoDuyet, "Chuẩn bị bảo vệ", 0);

            // 2 phiếu đang mượn đã quá hạn trễ 1 và 6 ngày
            NewPhieu(today.AddDays(-15), today.AddDays(-1), TrangThaiPhieuMuon.DangMuon, giuSach: true);
            NewPhieu(today.AddDays(-20), today.AddDays(-6), TrangThaiPhieuMuon.DangMuon, giuSach: true);

            // 3 phiếu đang mượn thử nhận trả sách (hạn: hôm nay, +3, +10 ngày)
            NewPhieu(today.AddDays(-14), today, TrangThaiPhieuMuon.DangMuon, giuSach: true);
            NewPhieu(today.AddDays(-11), today.AddDays(3), TrangThaiPhieuMuon.DangMuon, giuSach: true);
            NewPhieu(today.AddDays(-4), today.AddDays(10), TrangThaiPhieuMuon.DangMuon, giuSach: true);

            // 16 phiếu Đã trả 
            // hanTraTruoc: hạn trả cách hôm nay bao nhiêu ngày; 
            // ngayTraLech: ngày trả lệch so với hạn, âm = trả sớm.
            void Tra(int hanTraTruoc, int ngayTraLech, TinhTrangSach tinhTrang, bool daNop = false)
            {
                var hanTra = today.AddDays(-hanTraTruoc);
                // Sách Mất không quay lại kho tính vào số cuốn bị chiếm.
                var p = NewPhieu(hanTra.AddDays(-14), hanTra, TrangThaiPhieuMuon.DaTra, giuSach: tinhTrang == TinhTrangSach.Mat);
                var ngayTra = hanTra.AddDays(ngayTraLech);
                var tre = phatTre.Tinh(cfg, hanTra, ngayTra);
                var tienPhat = tre.TienPhat + phatTinhTrang.TinhTienPhat(cfg, tinhTrang, p.Sach!.GiaBia);

                db.TraSachs.Add(new TraSach
                {
                    PhieuMuon = p,
                    NgayTra = ngayTra,
                    TinhTrangSach = tinhTrang,
                    SoNgayTre = tre.SoNgayTre,
                    TienPhat = tienPhat,
                    TrangThaiPhat = tienPhat <= 0
                        ? TrangThaiPhat.KhongPhat
                        : daNop ? TrangThaiPhat.DaNop : TrangThaiPhat.ChuaNop,
                    GhiChu = "Dữ liệu mẫu",
                    NguoiNhan = staff
                });
            }

            // đúng hạn hoặc trả sớm
            Tra(30, -3, TinhTrangSach.BinhThuong);
            Tra(26, 0, TinhTrangSach.BinhThuong);
            Tra(22, -1, TinhTrangSach.BinhThuong);
            Tra(18, 0, TinhTrangSach.BinhThuong);
            Tra(14, -2, TinhTrangSach.BinhThuong);
            // trả trễ
            Tra(40, 2, TinhTrangSach.BinhThuong);
            Tra(35, 5, TinhTrangSach.BinhThuong);
            Tra(12, 3, TinhTrangSach.BinhThuong);
            Tra(50, 4, TinhTrangSach.BinhThuong, daNop: true);
            Tra(45, 7, TinhTrangSach.BinhThuong, daNop: true);
            // hư hỏng
            Tra(33, 0, TinhTrangSach.HuHong);
            Tra(28, 3, TinhTrangSach.HuHong);
            Tra(24, 0, TinhTrangSach.HuHong, daNop: true);
            // mất
            Tra(60, 0, TinhTrangSach.Mat);
            Tra(55, 6, TinhTrangSach.Mat, daNop: true);
            Tra(10, 1, TinhTrangSach.Mat);
        }
        catch (InvalidOperationException)
        {
            db.ChangeTracker.Clear();
            return;
        }

        await db.SaveChangesAsync();
    }
}