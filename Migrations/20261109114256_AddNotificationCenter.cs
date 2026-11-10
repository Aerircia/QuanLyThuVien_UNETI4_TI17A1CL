using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyThuVien_UNETI4_TI17A1CL.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationCenter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CaiDatThongBao",
                columns: table => new
                {
                    MaCaiDat = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaTaiKhoan = table.Column<int>(type: "int", nullable: false),
                    BatThongBaoWeb = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    BatThongBaoEmail = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    NhomMuonTra = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    NhomGiaHan = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    NhomTienPhat = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    NhomHeThong = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaiDatThongBao", x => x.MaCaiDat);
                    table.ForeignKey(
                        name: "FK_CaiDatThongBao_TaiKhoan_MaTaiKhoan",
                        column: x => x.MaTaiKhoan,
                        principalTable: "TaiKhoan",
                        principalColumn: "MaTaiKhoan",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ThongBao",
                columns: table => new
                {
                    MaThongBao = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaTaiKhoan = table.Column<int>(type: "int", nullable: false),
                    TieuDe = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    LoaiThongBao = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    MucDo = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    Icon = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    LoaiDoiTuong = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    MaDoiTuong = table.Column<int>(type: "int", nullable: true),
                    DuongDan = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    KhoaSuKien = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DaDoc = table.Column<bool>(type: "bit", nullable: false),
                    NgayDoc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DangHienThi = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThongBao", x => x.MaThongBao);
                    table.ForeignKey(
                        name: "FK_ThongBao_TaiKhoan_MaTaiKhoan",
                        column: x => x.MaTaiKhoan,
                        principalTable: "TaiKhoan",
                        principalColumn: "MaTaiKhoan",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CaiDatThongBao_MaTaiKhoan",
                table: "CaiDatThongBao",
                column: "MaTaiKhoan",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ThongBao_Inbox",
                table: "ThongBao",
                columns: new[] { "MaTaiKhoan", "DangHienThi", "DaDoc", "NgayTao" });

            migrationBuilder.CreateIndex(
                name: "IX_ThongBao_MaTaiKhoan_KhoaSuKien",
                table: "ThongBao",
                columns: new[] { "MaTaiKhoan", "KhoaSuKien" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CaiDatThongBao");

            migrationBuilder.DropTable(
                name: "ThongBao");
        }
    }
}
