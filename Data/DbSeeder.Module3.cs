// Họ và tên: Nguyễn Hữu Tuấn
// Mã sinh viên: 23103100136
// Nội dung thực hiện: Seed dữ liệu thể loại, sách mẫu và dữ liệu phiếu mượn ban đầu phục vụ Module 3

using Microsoft.EntityFrameworkCore;
using QuanLyThuVien_UNETI4_TI17A1CL.Models;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Data;

public static partial class DbSeeder
{
    private static async Task SeedModule3Async(AppDbContext db)
    {
        await SeedTheLoaiAsync(db);
        await SeedSachAsync(db);
    }

    private static async Task SeedTheLoaiAsync(AppDbContext db)
    {
        if (await db.TheLoais.AnyAsync()) return;

        var theLoais = new List<TheLoai>
        {
            new() { TenTheLoai = "Lập trình", KhuVucLuuTru = "Khu A - Tầng 2", MoTa = "Sách kỹ thuật, công nghệ thông tin và khoa học máy tính", TrangThai = TrangThaiTheLoai.HoatDong },
            new() { TenTheLoai = "Khoa học", KhuVucLuuTru = "Khu B - Tầng 2", MoTa = "Vũ trụ, vật lý, sinh học và khoa học tự nhiên", TrangThai = TrangThaiTheLoai.HoatDong },
            new() { TenTheLoai = "Văn học", KhuVucLuuTru = "Khu C - Tầng 1", MoTa = "Tiểu thuyết, truyện ngắn, tác phẩm kinh điển trong và ngoài nước", TrangThai = TrangThaiTheLoai.HoatDong },
            new() { TenTheLoai = "Lịch sử", KhuVucLuuTru = "Khu D - Tầng 1", MoTa = "Lịch sử Việt Nam và thế giới, văn hóa nhân loại", TrangThai = TrangThaiTheLoai.HoatDong },
            new() { TenTheLoai = "Kinh tế", KhuVucLuuTru = "Khu E - Tầng 2", MoTa = "Kinh tế học, tài chính, quản trị kinh doanh", TrangThai = TrangThaiTheLoai.HoatDong },
            new() { TenTheLoai = "Thiếu nhi", KhuVucLuuTru = "Khu F - Tầng 1", MoTa = "Truyện thiếu nhi, văn học tuổi thơ", TrangThai = TrangThaiTheLoai.HoatDong }
        };

        db.TheLoais.AddRange(theLoais);
        await db.SaveChangesAsync();
    }

    private static async Task SeedSachAsync(AppDbContext db)
    {
        if (await db.Sachs.AnyAsync()) return;

        var cats = await db.TheLoais.ToDictionaryAsync(c => c.TenTheLoai, c => c.MaTheLoai);
        if (!cats.Any()) return;

        var books = new List<Sach>
        {
            new()
            {
                MaTheLoai = cats["Lập trình"],
                TenSach = "C# in Depth",
                TacGia = "Jon Skeet",
                NhaXuatBan = "NXB Thông tin và Truyền thông",
                NamXuatBan = 2019,
                SoLuongTong = 5,
                GiaBia = 450000m,
                ViTriKe = "K1",
                MoTa = "Tài liệu chuyên sâu về ngôn ngữ C# từ cơ bản đến nâng cao, generics, async/await và LINQ.",
                TrangThai = TrangThaiSach.DangPhucVu
            },
            new()
            {
                MaTheLoai = cats["Lập trình"],
                TenSach = "Clean Code",
                TacGia = "Robert C. Martin",
                NhaXuatBan = "NXB Khoa học và Kỹ thuật",
                NamXuatBan = 2008,
                SoLuongTong = 4,
                GiaBia = 380000m,
                ViTriKe = "K2",
                MoTa = "Cẩm nang quy tắc viết mã sạch, dễ bảo trì và mở rộng dành cho kỹ sư phần mềm.",
                TrangThai = TrangThaiSach.DangPhucVu
            },
            new()
            {
                MaTheLoai = cats["Lập trình"],
                TenSach = "Pro ASP.NET Core",
                TacGia = "Adam Freeman",
                NhaXuatBan = "NXB Bách Khoa",
                NamXuatBan = 2022,
                SoLuongTong = 4,
                GiaBia = 520000m,
                ViTriKe = "K3",
                MoTa = "Hướng dẫn toàn diện xây dựng ứng dụng web hiện đại với ASP.NET Core MVC và Entity Framework Core.",
                TrangThai = TrangThaiSach.DangPhucVu
            },
            new()
            {
                MaTheLoai = cats["Khoa học"],
                TenSach = "Cosmos",
                TacGia = "Carl Sagan",
                NhaXuatBan = "NXB Thế Giới",
                NamXuatBan = 1980,
                SoLuongTong = 4,
                GiaBia = 210000m,
                ViTriKe = "K4",
                MoTa = "Hành trình khám phá vũ trụ kỳ thú qua lời kể đầy cảm hứng của nhà thiên văn học Carl Sagan.",
                TrangThai = TrangThaiSach.DangPhucVu
            },
            new()
            {
                MaTheLoai = cats["Khoa học"],
                TenSach = "A Brief History of Time",
                TacGia = "Stephen Hawking",
                NhaXuatBan = "NXB Trẻ",
                NamXuatBan = 1988,
                SoLuongTong = 3,
                GiaBia = 190000m,
                ViTriKe = "K5",
                MoTa = "Lược sử thời gian: Từ vụ nổ lớn Big Bang đến lỗ đen vũ trụ.",
                TrangThai = TrangThaiSach.DangPhucVu
            },
            new()
            {
                MaTheLoai = cats["Văn học"],
                TenSach = "Dế Mèn Phiêu Lưu Ký",
                TacGia = "Tô Hoài",
                NhaXuatBan = "NXB Kim Đồng",
                NamXuatBan = 1941,
                SoLuongTong = 6,
                GiaBia = 80000m,
                ViTriKe = "K6",
                MoTa = "Tác phẩm văn học kinh điển của nhà văn Tô Hoài về chuyến phiêu lưu bổ ích của chú Dế Mèn.",
                TrangThai = TrangThaiSach.DangPhucVu
            },
            new()
            {
                MaTheLoai = cats["Văn học"],
                TenSach = "Truyện Kiều",
                TacGia = "Nguyễn Du",
                NhaXuatBan = "NXB Văn học",
                NamXuatBan = 1920,
                SoLuongTong = 5,
                GiaBia = 95000m,
                ViTriKe = "K7",
                MoTa = "Kiệt tác văn học thơ Nôm của đại thi hào Nguyễn Du.",
                TrangThai = TrangThaiSach.DangPhucVu
            },
            new()
            {
                MaTheLoai = cats["Lịch sử"],
                TenSach = "Sapiens: Lược Sử Loài Người",
                TacGia = "Yuval Noah Harari",
                NhaXuatBan = "NXB Tri Thức",
                NamXuatBan = 2011,
                SoLuongTong = 4,
                GiaBia = 260000m,
                ViTriKe = "K8",
                MoTa = "Lịch sử tiến hóa và phát triển của loài người từ thời kỳ đồ đá đến hiện đại.",
                TrangThai = TrangThaiSach.DangPhucVu
            },
            new()
            {
                MaTheLoai = cats["Lịch sử"],
                TenSach = "Việt Nam Sử Lược",
                TacGia = "Trần Trọng Kim",
                NhaXuatBan = "NXB Văn học",
                NamXuatBan = 1920,
                SoLuongTong = 3,
                GiaBia = 150000m,
                ViTriKe = "K9",
                MoTa = "Bộ thông sử viết bằng chữ quốc ngữ đầu tiên của Việt Nam.",
                TrangThai = TrangThaiSach.DangPhucVu
            },
            new()
            {
                MaTheLoai = cats["Kinh tế"],
                TenSach = "Tư Duy Nhanh Và Chậm",
                TacGia = "Daniel Kahneman",
                NhaXuatBan = "NXB Thế Giới",
                NamXuatBan = 2011,
                SoLuongTong = 3,
                GiaBia = 310000m,
                ViTriKe = "K1",
                MoTa = "Nghiên cứu kinh tế học hành vi và tâm lý ra quyết định của con người.",
                TrangThai = TrangThaiSach.DangPhucVu
            },
            new()
            {
                MaTheLoai = cats["Kinh tế"],
                TenSach = "Kinh Tế Học Hài Hước",
                TacGia = "Steven Levitt & Stephen Dubner",
                NhaXuatBan = "NXB Trẻ",
                NamXuatBan = 2005,
                SoLuongTong = 4,
                GiaBia = 200000m,
                ViTriKe = "K2",
                MoTa = "Góc nhìn kinh tế học dí dỏm giải thích các hiện tượng xã hội hàng ngày.",
                TrangThai = TrangThaiSach.DangPhucVu
            },
            new()
            {
                MaTheLoai = cats["Thiếu nhi"],
                TenSach = "Hoàng Tử Bé",
                TacGia = "Antoine de Saint-Exupéry",
                NhaXuatBan = "NXB Kim Đồng",
                NamXuatBan = 1943,
                SoLuongTong = 5,
                GiaBia = 120000m,
                ViTriKe = "K3",
                MoTa = "Câu chuyện ngụ ngôn triết lý nhân văn về tình yêu và trách nhiệm.",
                TrangThai = TrangThaiSach.DangPhucVu
            }
        };

        db.Sachs.AddRange(books);
        await db.SaveChangesAsync();
    }
}
