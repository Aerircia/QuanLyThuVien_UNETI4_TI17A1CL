// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Xây dựng DbSeeder.cs, seed dữ liệu ban đầu cho cơ sở dữ liệu, bao gồm cấu hình thư viện, tài khoản mẫu, và hồ sơ bạn đọc mẫu.

// Họ và tên: Vũ Tiến Thành
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Sửa đổi seed cho ba thể loại và 20 sách mẫu với trạng thái phục vụ, số lượng tồn và ISBN để phục vụ test module.

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien_UNETI4_TI17A1CL.Models;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Data;

// Baseline seed only: settings row, one account per role, one reader profile.
// Idempotent - safe to run on every startup. The volume required by the spec (section 16)
// is a later task, added per module as DbSeeder.<Module>.cs (make this class partial then).
public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Requires at least one migration (dotnet ef migrations add InitialCreate)
        await db.Database.MigrateAsync();

        await SeedSettingsAsync(db);
        await SeedAccountsAsync(db);
        await SeedBanDocAsync(db);
        await SeedCategoriesAndBooksAsync(db);
    }

    private static async Task SeedSettingsAsync(AppDbContext db)
    {
        if (await db.CauHinhThuViens.AnyAsync()) return;
        db.CauHinhThuViens.Add(new CauHinhThuVien()); // defaults live on the entity; table allows one row only
        await db.SaveChangesAsync();
    }

    private static async Task SeedAccountsAsync(AppDbContext db)
    {
        var hasher = new PasswordHasher<TaiKhoan>();

        async Task AddIfMissing(string user, string password, string hoTen, string email, VaiTro role)
        {
            if (await db.TaiKhoans.AnyAsync(t => t.TenDangNhap == user)) return;
            var tk = new TaiKhoan
            {
                TenDangNhap = user,
                HoTen = hoTen,
                Email = email,
                VaiTro = role,
                TrangThai = TrangThaiTaiKhoan.HoatDong
            };
            tk.MatKhau = hasher.HashPassword(tk, password);
            db.TaiKhoans.Add(tk);
        }

        await AddIfMissing("admin", "Admin@123", "Quản trị viên", "admin@library.local", VaiTro.Admin);
        await AddIfMissing("thuthu", "ThuThu@123", "Thủ thư mẫu", "thuthu@library.local", VaiTro.ThuThu);
        await AddIfMissing("bandoc", "BanDoc@123", "Nguyễn Văn A", "bandoc@library.local", VaiTro.BanDoc);
        await db.SaveChangesAsync();
    }

    private static async Task SeedBanDocAsync(AppDbContext db)
    {
        var account = await db.TaiKhoans.FirstOrDefaultAsync(t => t.TenDangNhap == "bandoc");
        if (account is null || await db.BanDocs.AnyAsync(b => b.MaTaiKhoan == account.MaTaiKhoan)) return;

        var today = DateOnly.FromDateTime(DateTime.Today);
        db.BanDocs.Add(new BanDoc
        {
            MaTaiKhoan = account.MaTaiKhoan,
            HoTen = account.HoTen,           // same as TaiKhoan (BanDoc is the source of truth)
            Email = account.Email,
            NgaySinh = new DateOnly(2003, 5, 1),
            GioiTinh = GioiTinh.Nam,
            SoDienThoai = "0900000001",
            DiaChi = "Hà Nội",
            LoaiBanDoc = LoaiBanDoc.SinhVien,
            NgayCapThe = today,
            NgayHetHanThe = today.AddYears(1),
            TrangThai = TrangThaiBanDoc.HoatDong
            // MaSoThe is left null on purpose: the database sequence fills it (TV-000001)
        });
        await db.SaveChangesAsync();
    }

    private static async Task SeedCategoriesAndBooksAsync(AppDbContext db)
    {
        var categorySeeds = new (string Name, string Description, TrangThaiTheLoai Status)[]
        {
            ("Văn học", "Tác phẩm văn học Việt Nam và thế giới", TrangThaiTheLoai.HoatDong),
            ("Khoa học", "Sách khoa học và công nghệ", TrangThaiTheLoai.HoatDong),
            ("Lịch sử", "Sách lịch sử và tư liệu", TrangThaiTheLoai.NgungHoatDong)
        };

        foreach (var seed in categorySeeds)
        {
            if (!await db.TheLoais.AnyAsync(c => c.TenTheLoai == seed.Name))
                db.TheLoais.Add(new TheLoai
                {
                    TenTheLoai = seed.Name,
                    MoTa = seed.Description,
                    TrangThai = seed.Status
                });
        }
        await db.SaveChangesAsync();

        if (await db.Sachs.AnyAsync())
            return;

        var categoryNames = categorySeeds.Select(s => s.Name).ToArray();
        var categoryIds = await db.TheLoais.AsNoTracking()
            .Where(c => categoryNames.Contains(c.TenTheLoai))
            .ToDictionaryAsync(c => c.TenTheLoai, c => c.MaTheLoai);

        var bookSeeds = new (string Title, string Author, string Publisher, int Year,
            string Category, int Total, TrangThaiSach Status)[]
        {
            ("Dế Mèn phiêu lưu ký", "Tô Hoài", "Kim Đồng", 1941, "Văn học", 8, TrangThaiSach.DangPhucVu),
            ("Truyện Kiều", "Nguyễn Du", "Văn Học", 1820, "Văn học", 0, TrangThaiSach.DangPhucVu),
            ("Số đỏ", "Vũ Trọng Phụng", "Văn Học", 1936, "Văn học", 5, TrangThaiSach.DangPhucVu),
            ("Lão Hạc", "Nam Cao", "Văn Học", 1943, "Văn học", 4, TrangThaiSach.DangPhucVu),
            ("Tắt đèn", "Ngô Tất Tố", "Văn Học", 1939, "Văn học", 0, TrangThaiSach.DangPhucVu),
            ("Nhật ký trong tù", "Hồ Chí Minh", "Chính trị Quốc gia", 1942, "Văn học", 3, TrangThaiSach.TamNgung),
            ("Đất rừng phương Nam", "Đoàn Giỏi", "Kim Đồng", 1957, "Văn học", 7, TrangThaiSach.DangPhucVu),
            ("Cho tôi xin một vé đi tuổi thơ", "Nguyễn Nhật Ánh", "Trẻ", 2008, "Văn học", 9, TrangThaiSach.DangPhucVu),
            ("Tôi thấy hoa vàng trên cỏ xanh", "Nguyễn Nhật Ánh", "Trẻ", 2010, "Văn học", 0, TrangThaiSach.TamNgung),
            ("Vừa nhắm mắt vừa mở cửa sổ", "Nguyễn Ngọc Thuần", "Trẻ", 2004, "Văn học", 3, TrangThaiSach.NgungPhucVu),
            ("Lược sử thời gian", "Stephen Hawking", "Trẻ", 1988, "Khoa học", 5, TrangThaiSach.DangPhucVu),
            ("Vũ trụ trong vỏ hạt dẻ", "Stephen Hawking", "Trẻ", 2001, "Khoa học", 0, TrangThaiSach.DangPhucVu),
            ("Nguồn gốc các loài", "Charles Darwin", "Tri Thức", 1859, "Khoa học", 3, TrangThaiSach.DangPhucVu),
            ("Cơ thể người", "Bill Bryson", "Thế Giới", 2019, "Khoa học", 6, TrangThaiSach.DangPhucVu),
            ("Lược sử vạn vật", "Bill Bryson", "Thế Giới", 2003, "Khoa học", 0, TrangThaiSach.TamNgung),
            ("Sapiens: Lược sử loài người", "Yuval Noah Harari", "Tri Thức", 2011, "Lịch sử", 7, TrangThaiSach.DangPhucVu),
            ("Đại Việt sử ký toàn thư", "Ngô Sĩ Liên", "Khoa học xã hội", 1479, "Lịch sử", 0, TrangThaiSach.DangPhucVu),
            ("Việt Nam sử lược", "Trần Trọng Kim", "Tân Việt", 1920, "Lịch sử", 4, TrangThaiSach.TamNgung),
            ("Lịch sử Việt Nam bằng tranh", "Nhiều tác giả", "Trẻ", 2010, "Lịch sử", 5, TrangThaiSach.DangPhucVu),
            ("Hà Nội nghìn năm văn hiến", "Nhiều tác giả", "Hà Nội", 2010, "Lịch sử", 0, TrangThaiSach.NgungPhucVu)
        };

        db.Sachs.AddRange(bookSeeds.Select((seed, index) => new Sach
        {
            TenSach = seed.Title,
            TacGia = seed.Author,
            ISBN = $"978604{index + 1:000000000}",
            NhaXuatBan = seed.Publisher,
            NamXuatBan = seed.Year,
            MaTheLoai = categoryIds[seed.Category],
            SoLuongTong = seed.Total,
            GiaBia = 50000 + index * 5000,
            ViTriKe = $"Kệ {index % 5 + 1}",
            MoTa = $"Sách {seed.Category.ToLower()} - {seed.Title}.",
            TrangThai = seed.Status
        }));
        await db.SaveChangesAsync();
    }
}
