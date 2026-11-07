// Họ và tên: Nguyễn Hữu Tuấn
// Mã sinh viên: 23103100136
// Nội dung thực hiện: Seed dữ liệu thể loại, sách mẫu và dữ liệu phiếu mượn ban đầu phục vụ Module 3

// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Mở rộng seed

//NOTE: File này từng tên là DbSeeder.Module3.cs

using Microsoft.EntityFrameworkCore;
using QuanLyThuVien_UNETI4_TI17A1CL.Models;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Data;

public static partial class DbSeeder
{
    // Nạp 1 lần khi seed để kiểm tra file ảnh bìa có tồn tại trên đĩa hay không.
    private static string? _webRootPath;

    // Thứ tự mảng = số thứ tự đầu sách (1..50) mà DbSeeder.Loans.cs tham chiếu.
    private sealed record BookSeed(string Title, string Author, string Publisher, int Year, int Total,
        decimal Price, string Category, TrangThaiSach Status, string Description, string ImageFile);

    private static async Task SeedCatalogAsync(AppDbContext db, ILogger log, string? webRootPath)
    {
        _webRootPath = webRootPath;
        await SeedTheLoaiAsync(db);
        await SeedSachAsync(db, log);
    }

    private static async Task SeedTheLoaiAsync(AppDbContext db)
    {
        if (await db.TheLoais.AnyAsync()) return;

        db.TheLoais.AddRange(
            new TheLoai { TenTheLoai = "Lập trình", KhuVucLuuTru = "Khu A - Tầng 2", MoTa = "Sách kỹ thuật, công nghệ thông tin và khoa học máy tính", TrangThai = TrangThaiTheLoai.HoatDong },
            new TheLoai { TenTheLoai = "Khoa học", KhuVucLuuTru = "Khu B - Tầng 2", MoTa = "Vũ trụ, vật lý, sinh học và khoa học tự nhiên", TrangThai = TrangThaiTheLoai.HoatDong },
            new TheLoai { TenTheLoai = "Văn học", KhuVucLuuTru = "Khu C - Tầng 1", MoTa = "Tiểu thuyết, truyện ngắn, tác phẩm kinh điển trong và ngoài nước", TrangThai = TrangThaiTheLoai.HoatDong },
            new TheLoai { TenTheLoai = "Lịch sử", KhuVucLuuTru = "Khu D - Tầng 1", MoTa = "Lịch sử Việt Nam và thế giới, văn hóa nhân loại", TrangThai = TrangThaiTheLoai.HoatDong },
            new TheLoai { TenTheLoai = "Kinh tế", KhuVucLuuTru = "Khu E - Tầng 2", MoTa = "Kinh tế học, tài chính, quản trị kinh doanh", TrangThai = TrangThaiTheLoai.HoatDong },
            new TheLoai { TenTheLoai = "Thiếu nhi", KhuVucLuuTru = "Khu F - Tầng 1", MoTa = "Truyện thiếu nhi, văn học tuổi thơ", TrangThai = TrangThaiTheLoai.HoatDong });
        await db.SaveChangesAsync();
    }

    // Bổ sung theo tên sách: chạy lại không tạo trùng, và CSDL cũ (12 sách) được nâng lên 50.
    private static async Task SeedSachAsync(AppDbContext db, ILogger log)
    {
        var cats = await db.TheLoais.ToDictionaryAsync(c => c.TenTheLoai, c => c.MaTheLoai);
        var existing = await db.Sachs.ToDictionaryAsync(s => s.TenSach);

        var add = new List<Sach>();
        var changed = false;

        // Tự sửa dữ liệu cũ: đường dẫn ảnh thiếu dấu / đầu (vd "wwwroot/uploads/..." hay "uploads/bia/...")
        // sẽ thành URL tương đối và vỡ 404 trên các trang /Home/...; chuẩn hoá về "/uploads/bia/...".
        var anhSaiTienTo = await db.Sachs
            .Where(s => s.ImgPath != null && !s.ImgPath.StartsWith("/"))
            .ToListAsync();
        foreach (var sach in anhSaiTienTo)
        {
            var duongDan = sach.ImgPath.Trim();
            if (duongDan.StartsWith("wwwroot", StringComparison.OrdinalIgnoreCase))
                duongDan = duongDan["wwwroot".Length..];
            sach.ImgPath = "/" + duongDan.TrimStart('/');
            changed = true;
        }

        for (var i = 0; i < SampleBooks.Length; i++)
        {
            var b = SampleBooks[i];
            var imagePath = $"/uploads/bia/{Uri.EscapeDataString(b.ImageFile)}";

            // Bổ sung ảnh cho dữ liệu cũ nếu chưa có, không ghi đè ảnh thủ thư đã cập nhật.
            if (existing.TryGetValue(b.Title, out var existingBook))
            {
                // Ảnh trỏ tới file không còn trên đĩa (upload cũ bị mất) mà file seed có sẵn: trỏ về ảnh seed.
                if (!string.IsNullOrWhiteSpace(existingBook.ImgPath)
                    && existingBook.ImgPath.StartsWith("/uploads/bia/", StringComparison.OrdinalIgnoreCase)
                    && !File.Exists(Path.Combine(_webRootPath!, existingBook.ImgPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)))
                    && File.Exists(Path.Combine(_webRootPath!, "uploads", "bia", b.ImageFile)))
                {
                    existingBook.ImgPath = imagePath;
                    changed = true;
                }

                if (string.IsNullOrWhiteSpace(existingBook.ImgPath))
                {
                    existingBook.ImgPath = imagePath;
                    changed = true;
                }
                continue;
            }

            if (!cats.TryGetValue(b.Category, out var maTheLoai))
            {
                log.LogWarning("Bỏ qua sách '{Title}': không có thể loại '{Category}'.", b.Title, b.Category);
                continue;
            }

            add.Add(new Sach
            {
                MaTheLoai = maTheLoai,
                TenSach = b.Title,
                TacGia = b.Author,
                NhaXuatBan = b.Publisher,
                NamXuatBan = b.Year,
                SoLuongTong = b.Total,
                GiaBia = b.Price,
                ViTriKe = $"K{i % 9 + 1}",
                MoTa = b.Description,
                TrangThai = b.Status,
                ImgPath = imagePath
            });
            changed = true;
        }

        if (!changed) return;
        if (add.Count > 0) db.Sachs.AddRange(add);
        await db.SaveChangesAsync();
    }

    private static readonly BookSeed[] SampleBooks =
    {
        new("C# in Depth", "Jon Skeet", "NXB Thông tin và Truyền thông", 2019, 5, 450000m, "Lập trình", TrangThaiSach.DangPhucVu, "Tài liệu chuyên sâu về C# từ cơ bản đến nâng cao: generics, async/await và LINQ.", "C# in Depth.jpg"),
        new("Clean Code", "Robert C. Martin", "NXB Khoa học và Kỹ thuật", 2008, 4, 380000m, "Lập trình", TrangThaiSach.DangPhucVu, "Cẩm nang quy tắc viết mã sạch, dễ bảo trì và mở rộng cho kỹ sư phần mềm.", "Clean Code.webp"),
        new("Pro ASP.NET Core", "Adam Freeman", "NXB Bách Khoa", 2022, 4, 520000m, "Lập trình", TrangThaiSach.DangPhucVu, "Hướng dẫn xây dựng ứng dụng web hiện đại với ASP.NET Core MVC và Entity Framework Core.", "Pro ASP.NET Core.jpg"),
        new("Cosmos", "Carl Sagan", "NXB Thế Giới", 1980, 4, 210000m, "Khoa học", TrangThaiSach.DangPhucVu, "Hành trình khám phá vũ trụ qua lời kể đầy cảm hứng của nhà thiên văn học Carl Sagan.", "cosmos.png"),
        new("A Brief History of Time", "Stephen Hawking", "NXB Trẻ", 1988, 3, 190000m, "Khoa học", TrangThaiSach.DangPhucVu, "Lược sử thời gian: từ vụ nổ lớn Big Bang đến lỗ đen vũ trụ.", "A Brief History of Time.webp"),
        new("Dế Mèn Phiêu Lưu Ký", "Tô Hoài", "NXB Kim Đồng", 1941, 6, 80000m, "Văn học", TrangThaiSach.DangPhucVu, "Tác phẩm kinh điển của Tô Hoài về chuyến phiêu lưu của chú Dế Mèn.", "Dế Mèn Phiêu Lưu Ký.webp"),
        new("Truyện Kiều", "Nguyễn Du", "NXB Văn học", 1920, 5, 95000m, "Văn học", TrangThaiSach.DangPhucVu, "Kiệt tác thơ Nôm của đại thi hào Nguyễn Du.", "Truyện Kiều.webp"),
        new("Sapiens: Lược Sử Loài Người", "Yuval Noah Harari", "NXB Tri Thức", 2011, 4, 260000m, "Lịch sử", TrangThaiSach.DangPhucVu, "Lịch sử tiến hóa và phát triển của loài người từ thời đồ đá đến hiện đại.", "Sapiens Lược Sử Loài Người.webp"),
        new("Việt Nam Sử Lược", "Trần Trọng Kim", "NXB Văn học", 1920, 3, 150000m, "Lịch sử", TrangThaiSach.DangPhucVu, "Bộ thông sử Việt Nam viết bằng chữ quốc ngữ đầu tiên.", "Việt Nam Sử Lược.jpg"),
        new("Tư Duy Nhanh Và Chậm", "Daniel Kahneman", "NXB Thế Giới", 2011, 3, 310000m, "Kinh tế", TrangThaiSach.DangPhucVu, "Kinh tế học hành vi và tâm lý ra quyết định của con người.", "Tư Duy Nhanh Và Chậm.jpg"),
        new("Kinh Tế Học Hài Hước", "Steven Levitt & Stephen Dubner", "NXB Trẻ", 2005, 4, 200000m, "Kinh tế", TrangThaiSach.DangPhucVu, "Góc nhìn kinh tế học dí dỏm giải thích các hiện tượng xã hội hằng ngày.", "Kinh Tế Học Hài Hước.jpg"),
        new("Hoàng Tử Bé", "Antoine de Saint-Exupéry", "NXB Kim Đồng", 1943, 5, 120000m, "Thiếu nhi", TrangThaiSach.DangPhucVu, "Câu chuyện ngụ ngôn về tình yêu, tình bạn và trách nhiệm.", "Hoàng Tử Bé.jpg"),
        new("The Pragmatic Programmer", "Andrew Hunt & David Thomas", "NXB Khoa học và Kỹ thuật", 1999, 4, 420000m, "Lập trình", TrangThaiSach.DangPhucVu, "Những nguyên tắc thực dụng giúp lập trình viên làm việc hiệu quả và trưởng thành nghề nghiệp.", "The Pragmatic Programmer.jpg"),
        new("Design Patterns", "Erich Gamma và cộng sự", "NXB Thông tin và Truyền thông", 1994, 3, 580000m, "Lập trình", TrangThaiSach.DangPhucVu, "Danh mục các mẫu thiết kế hướng đối tượng kinh điển.", "Design Patterns.webp"),
        new("Head First C#", "Andrew Stellman & Jennifer Greene", "NXB Bách Khoa", 2020, 4, 460000m, "Lập trình", TrangThaiSach.DangPhucVu, "Học C# bằng hình ảnh và bài tập thực hành, phù hợp người mới bắt đầu.", "Head First C#.jpg"),
        new("Introduction to Algorithms", "Thomas H. Cormen và cộng sự", "NXB Khoa học và Kỹ thuật", 2009, 3, 720000m, "Lập trình", TrangThaiSach.DangPhucVu, "Giáo trình thuật toán và cấu trúc dữ liệu toàn diện, dùng rộng rãi ở bậc đại học.", "Introduction to Algorithms.jpg"),
        new("Refactoring", "Martin Fowler", "NXB Thông tin và Truyền thông", 2018, 3, 560000m, "Lập trình", TrangThaiSach.DangPhucVu, "Các kỹ thuật cải thiện cấu trúc mã nguồn mà không làm đổi hành vi.", "Refactoring.png"),
        new("Code Complete", "Steve McConnell", "NXB Bách Khoa", 2004, 2, 540000m, "Lập trình", TrangThaiSach.DangPhucVu, "Cẩm nang thực hành xây dựng phần mềm chất lượng cao.", "Code Complete.jpg"),
        new("SQL Performance Explained", "Markus Winand", "NXB Khoa học và Kỹ thuật", 2012, 2, 350000m, "Lập trình", TrangThaiSach.TamNgung, "Giải thích cách chỉ mục và truy vấn ảnh hưởng đến hiệu năng cơ sở dữ liệu SQL.", "SQL Performance Explained.jpg"),
        new("Astrophysics for People in a Hurry", "Neil deGrasse Tyson", "NXB Trẻ", 2017, 3, 230000m, "Khoa học", TrangThaiSach.DangPhucVu, "Giới thiệu ngắn gọn các khái niệm vũ trụ học dành cho người bận rộn.", "Astrophysics for People in a Hurry.jpg"),
        new("The Selfish Gene", "Richard Dawkins", "NXB Tri Thức", 1976, 3, 230000m, "Khoa học", TrangThaiSach.DangPhucVu, "Góc nhìn lấy gen làm trung tâm về tiến hóa và hành vi của sinh vật.", "The Selfish Gene.jpg"),
        new("The Gene: An Intimate History", "Siddhartha Mukherjee", "NXB Thế Giới", 2016, 2, 290000m, "Khoa học", TrangThaiSach.DangPhucVu, "Lịch sử di truyền học và những câu hỏi về tương lai của con người.", "The Gene - An Intimate History.webp"),
        new("Surely You're Joking, Mr. Feynman!", "Richard P. Feynman", "NXB Trẻ", 1985, 3, 210000m, "Khoa học", TrangThaiSach.DangPhucVu, "Những câu chuyện đời thường và hóm hỉnh của nhà vật lý Richard Feynman.", "Surely You're Joking, Mr. Feynman.webp"),
        new("Pale Blue Dot", "Carl Sagan", "NXB Tri Thức", 1994, 2, 200000m, "Khoa học", TrangThaiSach.DangPhucVu, "Suy ngẫm về vị trí nhỏ bé của Trái Đất và tương lai nhân loại trong vũ trụ.", "Pale Blue Dot.png"),
        new("Silent Spring", "Rachel Carson", "NXB Thế Giới", 1962, 2, 170000m, "Khoa học", TrangThaiSach.NgungPhucVu, "Cuốn sách tiên phong về tác hại của thuốc trừ sâu đối với môi trường.", "Silent Spring.jpg"),
        new("The Double Helix", "James D. Watson", "NXB Trẻ", 1968, 2, 180000m, "Khoa học", TrangThaiSach.TamNgung, "Câu chuyện khám phá cấu trúc xoắn kép của DNA.", "The Double Helix.jpg"),
        new("Tắt Đèn", "Ngô Tất Tố", "NXB Kim Đồng", 1939, 4, 70000m, "Văn học", TrangThaiSach.DangPhucVu, "Tiểu thuyết hiện thực phê phán về đời sống người nông dân nghèo.", "Tắt Đèn.webp"),
        new("Chí Phèo", "Nam Cao", "NXB Văn học", 1941, 5, 65000m, "Văn học", TrangThaiSach.DangPhucVu, "Truyện ngắn nổi tiếng về bi kịch người nông dân bị tha hóa.", "Chí Phèo.webp"),
        new("Số Đỏ", "Vũ Trọng Phụng", "NXB Trẻ", 1936, 3, 90000m, "Văn học", TrangThaiSach.DangPhucVu, "Tiểu thuyết trào phúng phê phán xã hội thành thị đương thời.", "Số Đỏ.jpg"),
        new("Mắt Biếc", "Nguyễn Nhật Ánh", "NXB Kim Đồng", 1990, 5, 90000m, "Văn học", TrangThaiSach.DangPhucVu, "Câu chuyện tình đầu trong trẻo và day dứt ở một làng quê.", "Mắt Biếc.jpg"),
        new("Cho Tôi Xin Một Vé Đi Tuổi Thơ", "Nguyễn Nhật Ánh", "NXB Văn học", 2008, 6, 85000m, "Văn học", TrangThaiSach.DangPhucVu, "Tản văn hóm hỉnh về tuổi thơ và những trò chơi trẻ con.", "Cho Tôi Xin Một Vé Đi Tuổi Thơ.jpg"),
        new("Lão Hạc", "Nam Cao", "NXB Trẻ", 1943, 3, 60000m, "Văn học", TrangThaiSach.DangPhucVu, "Truyện ngắn cảm động về số phận và lòng tự trọng của người nông dân.", "Lão Hạc.webp"),
        new("Trăm Năm Cô Đơn", "Gabriel García Márquez", "NXB Kim Đồng", 1967, 2, 210000m, "Văn học", TrangThaiSach.NgungPhucVu, "Tiểu thuyết hiện thực huyền ảo kể về bảy thế hệ của gia tộc Buendía.", "Trăm Năm Cô Đơn.webp"),
        new("Homo Deus", "Yuval Noah Harari", "NXB Thế Giới", 2015, 3, 270000m, "Lịch sử", TrangThaiSach.DangPhucVu, "Dự báo về tương lai loài người trong kỷ nguyên dữ liệu và trí tuệ nhân tạo.", "Homo Deus.webp"),
        new("Guns, Germs, and Steel", "Jared Diamond", "NXB Tri Thức", 1997, 2, 280000m, "Lịch sử", TrangThaiSach.DangPhucVu, "Lý giải vì sao các nền văn minh phát triển không đồng đều.", "Guns, Germs, and Steel.jpg"),
        new("The Silk Roads", "Peter Frankopan", "NXB Văn học", 2015, 2, 300000m, "Lịch sử", TrangThaiSach.DangPhucVu, "Lịch sử thế giới nhìn từ những tuyến đường giao thương xuyên lục địa.", "The Silk Roads.png"),
        new("21 Lessons for the 21st Century", "Yuval Noah Harari", "NXB Thế Giới", 2018, 3, 250000m, "Lịch sử", TrangThaiSach.DangPhucVu, "Những vấn đề cấp bách của thế giới hiện đại qua góc nhìn của Harari.", "21 Lessons for the 21st Century.jpg"),
        new("SPQR", "Mary Beard", "NXB Tri Thức", 2015, 2, 330000m, "Lịch sử", TrangThaiSach.DangPhucVu, "Lịch sử La Mã cổ đại từ thuở sơ khai đến đế chế.", "SPQR.jpg"),
        new("Vietnam: A History", "Stanley Karnow", "NXB Văn học", 1983, 2, 340000m, "Lịch sử", TrangThaiSach.NgungPhucVu, "Lịch sử Việt Nam hiện đại và cuộc chiến tranh Việt Nam.", "Vietnam - A History.jpg"),
        new("Rich Dad Poor Dad", "Robert T. Kiyosaki", "NXB Thế Giới", 1997, 4, 150000m, "Kinh tế", TrangThaiSach.DangPhucVu, "Bài học về tiền bạc và đầu tư cá nhân qua hai người cha.", "Rich Dad Poor Dad.jpg"),
        new("Principles of Economics", "N. Gregory Mankiw", "NXB Trẻ", 2017, 3, 360000m, "Kinh tế", TrangThaiSach.DangPhucVu, "Giáo trình nhập môn kinh tế học vi mô và vĩ mô.", "Principles of Economics.jpg"),
        new("Nudge", "Richard H. Thaler & Cass R. Sunstein", "NXB Lao động", 2008, 2, 240000m, "Kinh tế", TrangThaiSach.DangPhucVu, "Cách những cú huých nhỏ giúp định hướng quyết định tốt hơn.", "Nudge.jpg"),
        new("Zero to One", "Peter Thiel", "NXB Thế Giới", 2014, 3, 220000m, "Kinh tế", TrangThaiSach.DangPhucVu, "Tư duy khởi nghiệp và xây dựng công ty tạo ra giá trị mới.", "Zero to One.jpg"),
        new("The Intelligent Investor", "Benjamin Graham", "NXB Trẻ", 1949, 2, 310000m, "Kinh tế", TrangThaiSach.TamNgung, "Nguyên tắc đầu tư giá trị kinh điển của Benjamin Graham.", "The Intelligent Investor.webp"),
        new("Good to Great", "Jim Collins", "NXB Lao động", 2001, 3, 260000m, "Kinh tế", TrangThaiSach.DangPhucVu, "Nghiên cứu vì sao một số công ty vượt trội bền vững.", "Good to Great.webp"),
        new("Charlotte's Web", "E. B. White", "NXB Kim Đồng", 1952, 3, 100000m, "Thiếu nhi", TrangThaiSach.DangPhucVu, "Câu chuyện cảm động về tình bạn giữa chú heo Wilbur và nhện Charlotte.", "Charlotte's Web.jpg"),
        new("Matilda", "Roald Dahl", "NXB Trẻ", 1988, 3, 110000m, "Thiếu nhi", TrangThaiSach.DangPhucVu, "Cô bé thần đồng Matilda và những người lớn kỳ quặc.", "Matilda.jpg"),
        new("Harry Potter and the Philosopher's Stone", "J. K. Rowling", "NXB Thế Giới", 1997, 5, 175000m, "Thiếu nhi", TrangThaiSach.DangPhucVu, "Cuộc phiêu lưu đầu tiên của cậu bé phù thủy Harry Potter.", "Harry Potter and the Philosopher's Stone.jpg"),
        new("Nhóc Nicolas", "René Goscinny", "NXB Kim Đồng", 1959, 3, 95000m, "Thiếu nhi", TrangThaiSach.DangPhucVu, "Những câu chuyện học đường hài hước của cậu bé Nicolas.", "Nhóc Nicolas.jpg"),
        new("Doraemon - Tập 1", "Fujiko F. Fujio", "NXB Trẻ", 1970, 5, 25000m, "Thiếu nhi", TrangThaiSach.DangPhucVu, "Chú mèo máy Doraemon và những bảo bối thần kỳ giúp Nobita.", "Doraemon - Tập 1.jpg"),
    };
}
