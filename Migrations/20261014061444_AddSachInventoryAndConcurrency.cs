// Họ và tên: Vũ Tiến Thành
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Thêm ISBN/ Mã sách, số lượng còn lại và RowVersion cho sách; điều chỉnh điều kiện csdl áp dụng cho sách và backfill tồn kho theo phiếu mượn đang xử lý.

using Microsoft.EntityFrameworkCore.Migrations;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;

#nullable disable

namespace QuanLyThuVien_UNETI4_TI17A1CL.Migrations
{
    /// <inheritdoc />
    public partial class AddSachInventoryAndConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Sach_NamXuatBan",
                table: "Sach");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Sach_SoLuongTong",
                table: "Sach");

            migrationBuilder.AddColumn<string>(
                name: "ISBN",
                table: "Sach",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Sach",
                type: "rowversion",
                rowVersion: true,
                nullable: false);

            migrationBuilder.AddColumn<int>(
                name: "SoLuongConLai",
                table: "Sach",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql($"""
                UPDATE s
                SET SoLuongConLai = CASE
                    WHEN s.SoLuongTong > activeLoans.CountActive THEN s.SoLuongTong - activeLoans.CountActive
                    ELSE 0
                END
                FROM Sach AS s
                OUTER APPLY (
                    SELECT COUNT(*) AS CountActive
                    FROM PhieuMuon AS p
                    WHERE p.MaSach = s.MaSach
                      AND p.TrangThai IN
                          (N'{nameof(TrangThaiPhieuMuon.ChoDuyet)}',
                           N'{nameof(TrangThaiPhieuMuon.DaDuyet)}',
                           N'{nameof(TrangThaiPhieuMuon.DangMuon)}')
                ) AS activeLoans;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Sach_NamXuatBan",
                table: "Sach",
                sql: "[NamXuatBan] >= 1450");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Sach_SoLuongConLai",
                table: "Sach",
                sql: "[SoLuongConLai] >= 0 AND [SoLuongConLai] <= [SoLuongTong]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Sach_SoLuongTong",
                table: "Sach",
                sql: "[SoLuongTong] >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Sach_NamXuatBan",
                table: "Sach");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Sach_SoLuongConLai",
                table: "Sach");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Sach_SoLuongTong",
                table: "Sach");

            migrationBuilder.DropColumn(
                name: "ISBN",
                table: "Sach");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Sach");

            migrationBuilder.DropColumn(
                name: "SoLuongConLai",
                table: "Sach");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Sach_NamXuatBan",
                table: "Sach",
                sql: "[NamXuatBan] >= 1900");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Sach_SoLuongTong",
                table: "Sach",
                sql: "[SoLuongTong] > 0");
        }
    }
}
