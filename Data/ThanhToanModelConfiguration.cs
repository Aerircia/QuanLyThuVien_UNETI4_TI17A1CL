// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Câp nhật cấu hình model GiaoDichThanhToan và GiaoDichThanhToanChiTiet lên Database
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien_UNETI4_TI17A1CL.Models;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Data;

public static class ThanhToanModelConfiguration
{
    /// <summary>Gọi một dòng trong AppDbContext.OnModelCreating: modelBuilder.ConfigureThanhToan();</summary>
    public static void ConfigureThanhToan(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GiaoDichThanhToan>(e =>
        {
            e.ToTable("GiaoDichThanhToan", t => t.HasCheckConstraint("CK_GiaoDichThanhToan_TongTien", "[TongTien] > 0"));
            e.Property(x => x.TongTien).HasColumnType("decimal(18,2)");
            e.Property(x => x.PhuongThuc).HasConversion<string>().HasMaxLength(50);
            e.Property(x => x.TrangThai).HasConversion<string>().HasMaxLength(50);

            e.HasIndex(x => x.OrderCode).IsUnique()
                .HasFilter("[OrderCode] IS NOT NULL").HasDatabaseName("UX_GiaoDichThanhToan_OrderCode");
            e.HasIndex(x => new { x.MaBanDoc, x.NgayTao }).HasDatabaseName("IX_GiaoDichThanhToan_BanDoc_NgayTao");
            e.HasIndex(x => new { x.TrangThai, x.NgayHetHan }).HasDatabaseName("IX_GiaoDichThanhToan_TrangThai_HetHan");

            e.HasOne(x => x.BanDoc).WithMany().HasForeignKey(x => x.MaBanDoc).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.NguoiXacNhan).WithMany().HasForeignKey(x => x.MaNguoiXacNhan).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<GiaoDichThanhToanChiTiet>(e =>
        {
            e.ToTable("GiaoDichThanhToanChiTiet", t => t.HasCheckConstraint("CK_GiaoDichThanhToanChiTiet_SoTien", "[SoTien] > 0"));
            e.Property(x => x.SoTien).HasColumnType("decimal(18,2)");

            // Mỗi khoản phạt chỉ có tối đa một giao dịch đang chờ (chống thanh toán trùng).
            e.HasIndex(x => x.MaTra).IsUnique()
                .HasFilter("[DangCho] = 1").HasDatabaseName("UX_GiaoDichChiTiet_MaTra_DangCho");
            e.HasIndex(x => x.MaGiaoDich);

            e.HasOne(x => x.GiaoDich).WithMany(g => g.ChiTiets).HasForeignKey(x => x.MaGiaoDich).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.TraSach).WithMany().HasForeignKey(x => x.MaTra).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
