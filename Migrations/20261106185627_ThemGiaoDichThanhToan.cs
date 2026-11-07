using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyThuVien_UNETI4_TI17A1CL.Migrations
{
    /// <inheritdoc />
    public partial class ThemGiaoDichThanhToan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GiaoDichThanhToan",
                columns: table => new
                {
                    MaGiaoDich = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderCode = table.Column<long>(type: "bigint", nullable: true),
                    MaBanDoc = table.Column<int>(type: "int", nullable: false),
                    TongTien = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PhuongThuc = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NhaCungCap = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MaDonHangNhaCungCap = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MaGiaoDichNhaCungCap = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CheckoutUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayHetHan = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NgayThanhToan = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MaNguoiXacNhan = table.Column<int>(type: "int", nullable: true),
                    GhiChu = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GiaoDichThanhToan", x => x.MaGiaoDich);
                    table.CheckConstraint("CK_GiaoDichThanhToan_TongTien", "[TongTien] > 0");
                    table.ForeignKey(
                        name: "FK_GiaoDichThanhToan_BanDoc_MaBanDoc",
                        column: x => x.MaBanDoc,
                        principalTable: "BanDoc",
                        principalColumn: "MaBanDoc",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GiaoDichThanhToan_TaiKhoan_MaNguoiXacNhan",
                        column: x => x.MaNguoiXacNhan,
                        principalTable: "TaiKhoan",
                        principalColumn: "MaTaiKhoan",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GiaoDichThanhToanChiTiet",
                columns: table => new
                {
                    MaChiTiet = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaGiaoDich = table.Column<int>(type: "int", nullable: false),
                    MaTra = table.Column<int>(type: "int", nullable: false),
                    SoTien = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DangCho = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GiaoDichThanhToanChiTiet", x => x.MaChiTiet);
                    table.CheckConstraint("CK_GiaoDichThanhToanChiTiet_SoTien", "[SoTien] > 0");
                    table.ForeignKey(
                        name: "FK_GiaoDichThanhToanChiTiet_GiaoDichThanhToan_MaGiaoDich",
                        column: x => x.MaGiaoDich,
                        principalTable: "GiaoDichThanhToan",
                        principalColumn: "MaGiaoDich",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GiaoDichThanhToanChiTiet_TraSach_MaTra",
                        column: x => x.MaTra,
                        principalTable: "TraSach",
                        principalColumn: "MaTra",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GiaoDichThanhToan_BanDoc_NgayTao",
                table: "GiaoDichThanhToan",
                columns: new[] { "MaBanDoc", "NgayTao" });

            migrationBuilder.CreateIndex(
                name: "IX_GiaoDichThanhToan_MaNguoiXacNhan",
                table: "GiaoDichThanhToan",
                column: "MaNguoiXacNhan");

            migrationBuilder.CreateIndex(
                name: "IX_GiaoDichThanhToan_TrangThai_HetHan",
                table: "GiaoDichThanhToan",
                columns: new[] { "TrangThai", "NgayHetHan" });

            migrationBuilder.CreateIndex(
                name: "UX_GiaoDichThanhToan_OrderCode",
                table: "GiaoDichThanhToan",
                column: "OrderCode",
                unique: true,
                filter: "[OrderCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_GiaoDichThanhToanChiTiet_MaGiaoDich",
                table: "GiaoDichThanhToanChiTiet",
                column: "MaGiaoDich");

            migrationBuilder.CreateIndex(
                name: "UX_GiaoDichChiTiet_MaTra_DangCho",
                table: "GiaoDichThanhToanChiTiet",
                column: "MaTra",
                unique: true,
                filter: "[DangCho] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GiaoDichThanhToanChiTiet");

            migrationBuilder.DropTable(
                name: "GiaoDichThanhToan");
        }
    }
}
