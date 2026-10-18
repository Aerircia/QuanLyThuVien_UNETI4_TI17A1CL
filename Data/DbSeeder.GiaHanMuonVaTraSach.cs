// Họ và tên: Vũ Ngọc Hương Giang
// Mã sinh viên: 23103100280
// Nội dung thực hiện: Seeder gia hạn 13 yêu cầu và trả sách 16 bản ghi 

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien_UNETI4_TI17A1CL.Models;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;
using QuanLyThuVien_UNETI4_TI17A1CL.Services;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Data;
public static partial class DbSeeder
{
    // Seed chỉ để demo: nếu có lỗi thì in ra terminal và bỏ qua
    private static async Task SeedGiaHanMuonVaTraSachAsync(AppDbContext db)
    {
        try
        {
            await SeedGiaHanMuonVaTraSachCoreAsync(db);
        }
        catch (Exception ex)
        {
            db.ChangeTracker.Clear();
            Console.WriteLine($"[Seed Module 5] Lỗi khi tạo dữ liệu mẫu, bỏ qua: {ex.Message}");
        }
    }

    private static async Task SeedGiaHanMuonVaTraSachCoreAsync(AppDbContext db)
    {
        if (await db.GiaHanMuons.AnyAsync() || await db.TraSachs.AnyAsync())
        {
            Console.WriteLine("[Seed Module 5] Bỏ qua: database đã có dữ liệu gia hạn hoặc trả sách.");
            return;
        }

        // Ưu tiên Thủ thư làm người nhận trả sách; chỉ dùng Admin nếu chưa có Thủ thư nào.
        var staff = await db.TaiKhoans
            .Where(t => t.VaiTro == VaiTro.ThuThu || t.VaiTro == VaiTro.Admin)
            .OrderBy(t => t.VaiTro == VaiTro.ThuThu ? 0 : 1)
            .ThenBy(t => t.MaTaiKhoan)
            .FirstOrDefaultAsync();
        if (staff is null)
        {
            Console.WriteLine("[Seed Module 5] Bỏ qua: chưa có tài khoản Thủ thư/Admin.");
            return;
        }

        var today = DateOnly.FromDateTime(DateTime.Today);

        // Dữ liệu demo riêng của Module 5.
        // Note : có thể gỡ hoặc thay bằng dữ liệu thật.
        const string tenTheLoaiDemo = "Demo Module 5";

        var theLoai = await db.TheLoais.FirstOrDefaultAsync(t => t.TenTheLoai == tenTheLoaiDemo);
        if (theLoai is null)
        {
            theLoai = new TheLoai
            {
                TenTheLoai = tenTheLoaiDemo,
                MoTa = "Dữ liệu mẫu để thử Module 5",
                KhuVucLuuTru = "Khu demo"
            };
            db.TheLoais.Add(theLoai);
        }

        var sachs = await db.Sachs
            .Where(s => s.TheLoai!.TenTheLoai == tenTheLoaiDemo)
            .OrderBy(s => s.MaSach)
            .ToListAsync();
        for (var i = sachs.Count + 1; i <= 8; i++)
        {
            var sach = new Sach
            {
                TheLoai = theLoai,
                TenSach = $"[Demo M5] Sách {i}",
                TacGia = "Tác giả demo",
                NhaXuatBan = "NXB Demo",
                NamXuatBan = 2020,
                SoLuongTong = 30, 
                GiaBia = 50000m + i * 10000m,
                ViTriKe = "Kệ M5",
                TrangThai = TrangThaiSach.DangPhucVu
            };
            db.Sachs.Add(sach);
            sachs.Add(sach);
        }

        var banDocs = await db.BanDocs
            .Where(b => b.TaiKhoan!.TenDangNhap.StartsWith("demo_m5_"))
            .OrderBy(b => b.MaBanDoc)
            .ToListAsync();
        var hasher = new PasswordHasher<TaiKhoan>();
        for (var i = banDocs.Count + 1; i <= 5; i++)
        {
            var tk = new TaiKhoan
            {
                TenDangNhap = $"demo_m5_{i}",
                HoTen = $"Bạn đọc demo M5 {i}",
                Email = $"demo_m5_{i}@library.local",
                VaiTro = VaiTro.BanDoc,
                TrangThai = TrangThaiTaiKhoan.HoatDong
            };
            tk.MatKhau = hasher.HashPassword(tk, "BanDoc@123");

            var bd = new BanDoc
            {
                TaiKhoan = tk,
                HoTen = tk.HoTen,
                Email = tk.Email,
                NgaySinh = new DateOnly(2003, 1, i),
                GioiTinh = GioiTinh.Nam,
                SoDienThoai = "090000010" + i,
                DiaChi = "Hà Nội",
                LoaiBanDoc = LoaiBanDoc.SinhVien,
                NgayCapThe = today.AddMonths(-3),
                NgayHetHanThe = today.AddYears(1),
                TrangThai = TrangThaiBanDoc.HoatDong
            };
            db.BanDocs.Add(bd);
            banDocs.Add(bd);
        }

        // Lưu trước để các entity demo có mã (MaSach, MaBanDoc) dùng cho phần chọn cặp bên dưới
        await db.SaveChangesAsync();

        var cfg = await db.CauHinhThuViens.AsNoTracking().FirstOrDefaultAsync() ?? new CauHinhThuVien();
        var phatTre = new TienPhatTreHanService();
        var phatTinhTrang = new TienPhatTinhTrangSachService();

        // (bạn đọc, sách) đang xử lý "mỗi bạn đọc một phiếu đang xử lý cho một sách".
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
                // Sách Mất không quay lại kho 
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

            // gia hạn khi sách có người chờ mượn, Gia hạn phiếu đang mượn này sẽ bị chặn.
            var sachItBan = new Sach
            {
                TheLoai = theLoai,
                TenSach = "[Demo M5] Sách chỉ có 1 bản",
                TacGia = "Tác giả demo",
                NhaXuatBan = "NXB Demo",
                NamXuatBan = 2020,
                SoLuongTong = 1,
                GiaBia = 100000m,
                ViTriKe = "Kệ M5",
                TrangThai = TrangThaiSach.DangPhucVu
            };
            db.Sachs.Add(sachItBan);

            //banDocs[2] đang mượn bản duy nhất, chưa có yêu cầu gia hạn nào
            db.PhieuMuons.Add(new PhieuMuon
            {
                BanDoc = banDocs[2],
                Sach = sachItBan,
                NgayDangKy = today.AddDays(-7),
                NgayMuon = today.AddDays(-5),
                HanTra = today.AddDays(9),
                TrangThai = TrangThaiPhieuMuon.DangMuon,
                NgayXuLy = today.AddDays(-6),
                NhanXetThuThu = "Dữ liệu mẫu"
            });

            // banDocs[3] đang chờ duyệt mượn cùng cuốn sách đó
            db.PhieuMuons.Add(new PhieuMuon
            {
                BanDoc = banDocs[3],
                Sach = sachItBan,
                NgayDangKy = today,
                TrangThai = TrangThaiPhieuMuon.ChoDuyet
            });
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"[Seed Module 5] Bỏ qua: {ex.Message}");
            db.ChangeTracker.Clear();
            return;
        }

        await db.SaveChangesAsync();
        Console.WriteLine("[Seed Module 5] Đã tạo dữ liệu mẫu gia hạn và trả sách.");
    }
}