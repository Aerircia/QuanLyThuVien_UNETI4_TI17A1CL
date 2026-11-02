// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Seed tài khoản Admin/Thủ thư bổ sung và 30 bạn đọc.

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien_UNETI4_TI17A1CL.Models;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Data;

public static partial class DbSeeder
{
    // Mật khẩu của mọi tài khoản mẫu bổ sung = tên đăng nhập viết hoa chữ cái đầu (vd: bandoc02 -> Bandoc02).
    // Ba tài khoản mặc định (admin, thuthu, bandoc) giữ nguyên mật khẩu cũ. Email: <tên đăng nhập>@library.local.
    private sealed record StaffSeed(string User, string Name, VaiTro Role);

    // Issued / Expires: số ngày so với hôm nay lúc seed (âm = quá khứ).
    private sealed record ReaderSeed(string User, string Name, GioiTinh Sex, LoaiBanDoc Type, DateOnly Birth,
        string Phone, string Address, int Issued, int Expires,
        TrangThaiBanDoc Status = TrangThaiBanDoc.HoatDong,
        TrangThaiTaiKhoan Account = TrangThaiTaiKhoan.HoatDong);

    private static readonly StaffSeed[] StaffSeeds =
    {
        new("quantri", "Nguyễn Quang Minh", VaiTro.Admin),
        new("thuthu2", "Trần Thị Lan", VaiTro.ThuThu),
        new("thuthu3", "Lê Văn Hùng", VaiTro.ThuThu),
    };

    // R02..R30. R01 là tài khoản mặc định "bandoc" do SeedBanDocAsync tạo.
    private static readonly ReaderSeed[] ReaderSeeds =
    {
        new("bandoc02", "Trần Thị Mai", GioiTinh.Nu, LoaiBanDoc.SinhVien, new DateOnly(2004, 11, 7), "0912340002", "8 Láng Hạ, Đống Đa, Hà Nội", -299, 66), // R02
        new("bandoc03", "Lê Minh Quân", GioiTinh.Nam, LoaiBanDoc.SinhVien, new DateOnly(2005, 4, 10), "0912340003", "101 Xuân Thủy, Cầu Giấy, Hà Nội", -286, 79), // R03
        new("bandoc04", "Phạm Thu Hà", GioiTinh.Nu, LoaiBanDoc.SinhVien, new DateOnly(2002, 9, 13), "0912340004", "27 Giải Phóng, Hai Bà Trưng, Hà Nội", -273, 92), // R04
        new("bandoc05", "Hoàng Đức Anh", GioiTinh.Nam, LoaiBanDoc.SinhVien, new DateOnly(2003, 2, 16), "0912340005", "63 Tây Sơn, Đống Đa, Hà Nội", -260, 105), // R05
        new("bandoc06", "Vũ Ngọc Lan", GioiTinh.Nu, LoaiBanDoc.SinhVien, new DateOnly(2004, 7, 19), "0912340006", "19 Kim Mã, Ba Đình, Hà Nội", -247, 118), // R06
        new("bandoc07", "Đặng Quốc Bảo", GioiTinh.Nam, LoaiBanDoc.SinhVien, new DateOnly(2005, 12, 22), "0912340007", "5 Lê Văn Lương, Thanh Xuân, Hà Nội", -234, 131), // R07
        new("bandoc08", "Bùi Khánh Linh", GioiTinh.Nu, LoaiBanDoc.SinhVien, new DateOnly(2002, 5, 25), "0912340008", "77 Nguyễn Chí Thanh, Đống Đa, Hà Nội", -366, -1), // R08 · thẻ hết hạn
        new("bandoc09", "Đỗ Văn Hùng", GioiTinh.Nam, LoaiBanDoc.SinhVien, new DateOnly(2003, 10, 28), "0912340009", "34 Phạm Văn Đồng, Bắc Từ Liêm, Hà Nội", -208, 157), // R09
        new("bandoc10", "Ngô Thị Hương", GioiTinh.Nu, LoaiBanDoc.SinhVien, new DateOnly(2004, 3, 3), "0912340010", "12 Nguyễn Trãi, Thanh Xuân, Hà Nội", -195, 170), // R10
        new("bandoc11", "Dương Tuấn Kiệt", GioiTinh.Nam, LoaiBanDoc.SinhVien, new DateOnly(2005, 8, 6), "0912340011", "45 Cầu Giấy, Cầu Giấy, Hà Nội", -182, 183), // R11
        new("bandoc12", "Lý Gia Hân", GioiTinh.Nu, LoaiBanDoc.SinhVien, new DateOnly(2002, 1, 9), "0912340012", "8 Láng Hạ, Đống Đa, Hà Nội", -169, 196, TrangThaiBanDoc.NgungHoatDong), // R12 · bị tạm dừng
        new("bandoc13", "Phan Công Danh", GioiTinh.Nam, LoaiBanDoc.SinhVien, new DateOnly(2003, 6, 12), "0912340013", "101 Xuân Thủy, Cầu Giấy, Hà Nội", -156, 209), // R13
        new("bandoc14", "Trịnh Bảo Châu", GioiTinh.Nu, LoaiBanDoc.SinhVien, new DateOnly(2004, 11, 15), "0912340014", "27 Giải Phóng, Hai Bà Trưng, Hà Nội", -143, 222), // R14
        new("bandoc15", "Mai Văn Toàn", GioiTinh.Nam, LoaiBanDoc.SinhVien, new DateOnly(2005, 4, 18), "0912340015", "63 Tây Sơn, Đống Đa, Hà Nội", -440, -75), // R15 · thẻ hết hạn
        new("bandoc16", "Đinh Thị Ngọc", GioiTinh.Nu, LoaiBanDoc.SinhVien, new DateOnly(2002, 9, 21), "0912340016", "19 Kim Mã, Ba Đình, Hà Nội", -117, 248), // R16
        new("bandoc17", "Lương Minh Triết", GioiTinh.Nam, LoaiBanDoc.SinhVien, new DateOnly(2003, 2, 24), "0912340017", "5 Lê Văn Lương, Thanh Xuân, Hà Nội", -324, 41), // R17
        new("bandoc18", "Hồ Thanh Hải", GioiTinh.Nam, LoaiBanDoc.SinhVien, new DateOnly(2004, 7, 27), "0912340018", "77 Nguyễn Chí Thanh, Đống Đa, Hà Nội", -311, 54, Account: TrangThaiTaiKhoan.BiKhoa), // R18 · tài khoản bị khóa
        new("bandoc19", "Cao Phương Thảo", GioiTinh.Nu, LoaiBanDoc.SinhVien, new DateOnly(2005, 12, 2), "0912340019", "34 Phạm Văn Đồng, Bắc Từ Liêm, Hà Nội", -298, 67), // R19
        new("bandoc20", "Tô Quang Vinh", GioiTinh.Nam, LoaiBanDoc.SinhVien, new DateOnly(2002, 5, 5), "0912340020", "12 Nguyễn Trãi, Thanh Xuân, Hà Nội", -285, 80), // R20
        new("bandoc21", "Nguyễn Thị Hạnh", GioiTinh.Nu, LoaiBanDoc.GiangVien, new DateOnly(1981, 10, 8), "0912340021", "45 Cầu Giấy, Cầu Giấy, Hà Nội", -637, 93), // R21
        new("bandoc22", "Trần Văn Phúc", GioiTinh.Nam, LoaiBanDoc.GiangVien, new DateOnly(1984, 3, 11), "0912340022", "8 Láng Hạ, Đống Đa, Hà Nội", -624, 106), // R22
        new("bandoc23", "Lê Thị Thanh Tâm", GioiTinh.Nu, LoaiBanDoc.GiangVien, new DateOnly(1987, 8, 14), "0912340023", "101 Xuân Thủy, Cầu Giấy, Hà Nội", -611, 119), // R23
        new("bandoc24", "Phạm Hoàng Nam", GioiTinh.Nam, LoaiBanDoc.GiangVien, new DateOnly(1972, 1, 17), "0912340024", "27 Giải Phóng, Hai Bà Trưng, Hà Nội", -870, -140), // R24 · thẻ hết hạn
        new("bandoc25", "Vương Thu Trang", GioiTinh.Nu, LoaiBanDoc.GiangVien, new DateOnly(1975, 6, 20), "0912340025", "63 Tây Sơn, Đống Đa, Hà Nội", -585, 145), // R25
        new("bandoc26", "Đào Quang Huy", GioiTinh.Nam, LoaiBanDoc.GiangVien, new DateOnly(1978, 11, 23), "0912340026", "19 Kim Mã, Ba Đình, Hà Nội", -572, 158), // R26
        new("bandoc27", "Châu Anh Tuấn", GioiTinh.Nam, LoaiBanDoc.Khac, new DateOnly(1989, 4, 26), "0912340027", "5 Lê Văn Lương, Thanh Xuân, Hà Nội", -194, 171), // R27
        new("bandoc28", "Kiều Thị Hoa", GioiTinh.Nu, LoaiBanDoc.Khac, new DateOnly(1996, 9, 1), "0912340028", "77 Nguyễn Chí Thanh, Đống Đa, Hà Nội", -181, 184, TrangThaiBanDoc.NgungHoatDong), // R28 · bị tạm dừng
        new("bandoc29", "Lâm Quốc Bình", GioiTinh.Nam, LoaiBanDoc.Khac, new DateOnly(1983, 2, 4), "0912340029", "34 Phạm Văn Đồng, Bắc Từ Liêm, Hà Nội", -374, -9), // R29 · thẻ hết hạn
        new("bandoc30", "Nghiêm Thị Yến", GioiTinh.Nu, LoaiBanDoc.Khac, new DateOnly(1990, 7, 7), "0912340030", "12 Nguyễn Trãi, Thanh Xuân, Hà Nội", -155, 210), // R30
    };

    // Số thứ tự bạn đọc (1..30) mà DbSeeder.Loans.cs tham chiếu.
    private static readonly string[] ReaderUsers =
        new[] { "bandoc" }.Concat(ReaderSeeds.Select(r => r.User)).ToArray();

    private static string PasswordOf(string user) => char.ToUpperInvariant(user[0]) + user[1..];

    private static async Task SeedPeopleAsync(AppDbContext db, ILogger log)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var hasher = new PasswordHasher<TaiKhoan>();
        var users = (await db.TaiKhoans.Select(t => t.TenDangNhap).ToListAsync())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var s in StaffSeeds)
        {
            if (users.Contains(s.User)) continue;
            var tk = new TaiKhoan
            {
                TenDangNhap = s.User,
                HoTen = s.Name,
                Email = $"{s.User}@library.local",
                VaiTro = s.Role,
                TrangThai = TrangThaiTaiKhoan.HoatDong
            };
            tk.MatKhau = hasher.HashPassword(tk, PasswordOf(s.User));
            db.TaiKhoans.Add(tk);
        }

        foreach (var r in ReaderSeeds)
        {
            if (users.Contains(r.User)) continue;
            var email = $"{r.User}@library.local";
            var tk = new TaiKhoan
            {
                TenDangNhap = r.User,
                HoTen = r.Name,
                Email = email,
                VaiTro = VaiTro.BanDoc,
                TrangThai = r.Account
            };
            tk.MatKhau = hasher.HashPassword(tk, PasswordOf(r.User));
            // MaSoThe để null: SEQUENCE trong CSDL tự cấp (TV-000002, ...).
            tk.BanDoc = new BanDoc
            {
                HoTen = r.Name,
                Email = email,
                NgaySinh = r.Birth,
                GioiTinh = r.Sex,
                SoDienThoai = r.Phone,
                DiaChi = r.Address,
                LoaiBanDoc = r.Type,
                NgayCapThe = today.AddDays(r.Issued),
                NgayHetHanThe = today.AddDays(r.Expires),
                TrangThai = r.Status
            };
            db.TaiKhoans.Add(tk);
        }

        // Bạn đọc mặc định "bandoc" có lịch sử mượn từ ~75 ngày trước: thẻ phải được cấp trước đó.
        var demo = await db.BanDocs.FirstOrDefaultAsync(b => b.TaiKhoan!.TenDangNhap == "bandoc");
        if (demo is not null && demo.NgayCapThe > today.AddDays(-200))
            demo.NgayCapThe = today.AddDays(-200);

        await db.SaveChangesAsync();
    }
}
