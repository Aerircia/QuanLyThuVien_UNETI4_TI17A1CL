// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Xây dựng DbSeeder.cs, seed dữ liệu ban đầu cho cơ sở dữ liệu, bao gồm cấu hình thư viện, tài khoản mẫu, và hồ sơ bạn đọc mẫu.

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien_UNETI4_TI17A1CL.Models;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Data;

// Baseline seed only: settings row, one account per role, one reader profile.
// Idempotent - safe to run on every startup. The volume required by the spec (section 16)
// is a later task, added per module as DbSeeder.<Module>.cs (make this class partial then).
public static partial class DbSeeder
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
        await SeedGiaHanMuonVaTraSachAsync(db);
    }
}
