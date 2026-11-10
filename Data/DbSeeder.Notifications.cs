// Họ và tên: Phùng Ngọc Bảo
// Mã sinh viên: 23103100278
// Nội dung thực hiện: Seeder cấu hình thông báo mặc định cho các tài khoản chưa có cấu hình

using Microsoft.EntityFrameworkCore;
using QuanLyThuVien_UNETI4_TI17A1CL.Models;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Data;

public static partial class DbSeeder
{
    private static async Task SeedNotificationSettingsAsync(AppDbContext db)
    {
        // Lấy mã các tài khoản đã có cấu hình thông báo
        var configuredAccountIds = await db.CaiDatThongBaos
            .Select(x => x.MaTaiKhoan)
            .ToHashSetAsync();

        // Chỉ tạo cấu hình cho tài khoản chưa có bản ghi tương ứng
        var settings = await db.TaiKhoans
            .Where(account => !configuredAccountIds.Contains(account.MaTaiKhoan))
            .Select(account => new CaiDatThongBao
            {
                MaTaiKhoan = account.MaTaiKhoan,
                BatThongBaoWeb = true,
                BatThongBaoEmail = true,
                NhomMuonTra = true,
                NhomGiaHan = true,
                NhomTienPhat = true,
                NhomHeThong = true,
                NgayCapNhat = DateTime.UtcNow
            })
            .ToListAsync();

        if (settings.Count == 0)
        {
            return;
        }

        await db.CaiDatThongBaos.AddRangeAsync(settings);
        await db.SaveChangesAsync();
    }
}