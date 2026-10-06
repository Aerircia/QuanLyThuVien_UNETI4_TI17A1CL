using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyThuVien_UNETI4_TI17A1CL.Migrations
{
    /// <inheritdoc />
    public partial class QuanLyThuVienDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence<int>(
                name: "SoTheSeq");

            migrationBuilder.CreateTable(
                name: "CauHinhThuVien",
                columns: table => new
                {
                    MaCauHinh = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SoNgayMuonSinhVien = table.Column<int>(type: "int", nullable: false),
                    SoNgayMuonGiangVien = table.Column<int>(type: "int", nullable: false),
                    SoNgayMuonKhac = table.Column<int>(type: "int", nullable: false),
                    SoPhieuToiDa = table.Column<int>(type: "int", nullable: false),
                    SoLanGiaHanToiDa = table.Column<int>(type: "int", nullable: false),
                    SoNgayGiaHanToiDa = table.Column<int>(type: "int", nullable: false),
                    TienPhatMoiNgay = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TyLePhatHuHong = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    TyLePhatMat = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CauHinhThuVien", x => x.MaCauHinh);
                    table.CheckConstraint("CK_CauHinhThuVien_MotDong", "[MaCauHinh] = 1");
                });

            migrationBuilder.CreateTable(
                name: "OTPs",
                columns: table => new
                {
                    MaOtp = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    OtpCode = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    ThoiGianHetHan = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TinhTrangSuDung = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OTPs", x => x.MaOtp);
                });

            migrationBuilder.CreateTable(
                name: "TaiKhoan",
                columns: table => new
                {
                    MaTaiKhoan = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenDangNhap = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    MatKhau = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    HoTen = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    VaiTro = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaiKhoan", x => x.MaTaiKhoan);
                });

            migrationBuilder.CreateTable(
                name: "TheLoai",
                columns: table => new
                {
                    MaTheLoai = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenTheLoai = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    KhuVucLuuTru = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TrangThai = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TheLoai", x => x.MaTheLoai);
                });

            migrationBuilder.CreateTable(
                name: "BanDoc",
                columns: table => new
                {
                    MaBanDoc = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaTaiKhoan = table.Column<int>(type: "int", nullable: false),
                    HoTen = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    NgaySinh = table.Column<DateOnly>(type: "date", nullable: false),
                    GioiTinh = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SoDienThoai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    DiaChi = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    MaSoThe = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValueSql: "'TV-' + RIGHT('000000' + CAST(NEXT VALUE FOR SoTheSeq AS varchar(10)), 6)"),
                    LoaiBanDoc = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NgayCapThe = table.Column<DateOnly>(type: "date", nullable: false),
                    NgayHetHanThe = table.Column<DateOnly>(type: "date", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ImgPath = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BanDoc", x => x.MaBanDoc);
                    table.CheckConstraint("CK_BanDoc_HanThe", "[NgayHetHanThe] > [NgayCapThe]");
                    table.ForeignKey(
                        name: "FK_BanDoc_TaiKhoan_MaTaiKhoan",
                        column: x => x.MaTaiKhoan,
                        principalTable: "TaiKhoan",
                        principalColumn: "MaTaiKhoan",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Sach",
                columns: table => new
                {
                    MaSach = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaTheLoai = table.Column<int>(type: "int", nullable: false),
                    TenSach = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    TacGia = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    NhaXuatBan = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    NamXuatBan = table.Column<int>(type: "int", nullable: false),
                    SoLuongTong = table.Column<int>(type: "int", nullable: false),
                    GiaBia = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ViTriKe = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    MoTa = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TrangThai = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ImgPath = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sach", x => x.MaSach);
                    table.CheckConstraint("CK_Sach_GiaBia", "[GiaBia] >= 0");
                    table.CheckConstraint("CK_Sach_NamXuatBan", "[NamXuatBan] >= 1900");
                    table.CheckConstraint("CK_Sach_SoLuongTong", "[SoLuongTong] > 0");
                    table.ForeignKey(
                        name: "FK_Sach_TheLoai_MaTheLoai",
                        column: x => x.MaTheLoai,
                        principalTable: "TheLoai",
                        principalColumn: "MaTheLoai",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PhieuMuon",
                columns: table => new
                {
                    MaPhieuMuon = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaBanDoc = table.Column<int>(type: "int", nullable: false),
                    MaSach = table.Column<int>(type: "int", nullable: false),
                    NgayDangKy = table.Column<DateOnly>(type: "date", nullable: false),
                    NgayMuon = table.Column<DateOnly>(type: "date", nullable: true),
                    HanTra = table.Column<DateOnly>(type: "date", nullable: true),
                    GhiChu = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TrangThai = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NgayXuLy = table.Column<DateOnly>(type: "date", nullable: true),
                    NhanXetThuThu = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhieuMuon", x => x.MaPhieuMuon);
                    table.ForeignKey(
                        name: "FK_PhieuMuon_BanDoc_MaBanDoc",
                        column: x => x.MaBanDoc,
                        principalTable: "BanDoc",
                        principalColumn: "MaBanDoc",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhieuMuon_Sach_MaSach",
                        column: x => x.MaSach,
                        principalTable: "Sach",
                        principalColumn: "MaSach",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GiaHanMuon",
                columns: table => new
                {
                    MaGiaHan = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaPhieuMuon = table.Column<int>(type: "int", nullable: false),
                    HanTraCu = table.Column<DateOnly>(type: "date", nullable: false),
                    HanTraMoi = table.Column<DateOnly>(type: "date", nullable: false),
                    LyDo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NgayYeuCau = table.Column<DateOnly>(type: "date", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NgayXuLy = table.Column<DateOnly>(type: "date", nullable: true),
                    NhanXet = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GiaHanMuon", x => x.MaGiaHan);
                    table.CheckConstraint("CK_GiaHanMuon_HanTra", "[HanTraMoi] > [HanTraCu]");
                    table.ForeignKey(
                        name: "FK_GiaHanMuon_PhieuMuon_MaPhieuMuon",
                        column: x => x.MaPhieuMuon,
                        principalTable: "PhieuMuon",
                        principalColumn: "MaPhieuMuon",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PhieuMuonLichSu",
                columns: table => new
                {
                    MaLichSu = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaPhieuMuon = table.Column<int>(type: "int", nullable: false),
                    TrangThaiCu = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TrangThaiMoi = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NgayThucHien = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MaNguoiThucHien = table.Column<int>(type: "int", nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhieuMuonLichSu", x => x.MaLichSu);
                    table.ForeignKey(
                        name: "FK_PhieuMuonLichSu_PhieuMuon_MaPhieuMuon",
                        column: x => x.MaPhieuMuon,
                        principalTable: "PhieuMuon",
                        principalColumn: "MaPhieuMuon",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhieuMuonLichSu_TaiKhoan_MaNguoiThucHien",
                        column: x => x.MaNguoiThucHien,
                        principalTable: "TaiKhoan",
                        principalColumn: "MaTaiKhoan",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TraSach",
                columns: table => new
                {
                    MaTra = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaPhieuMuon = table.Column<int>(type: "int", nullable: false),
                    NgayTra = table.Column<DateOnly>(type: "date", nullable: false),
                    TinhTrangSach = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SoNgayTre = table.Column<int>(type: "int", nullable: false),
                    TienPhat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TrangThaiPhat = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MaNguoiNhan = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TraSach", x => x.MaTra);
                    table.CheckConstraint("CK_TraSach_SoNgayTre", "[SoNgayTre] >= 0");
                    table.CheckConstraint("CK_TraSach_TienPhat", "[TienPhat] >= 0");
                    table.ForeignKey(
                        name: "FK_TraSach_PhieuMuon_MaPhieuMuon",
                        column: x => x.MaPhieuMuon,
                        principalTable: "PhieuMuon",
                        principalColumn: "MaPhieuMuon",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TraSach_TaiKhoan_MaNguoiNhan",
                        column: x => x.MaNguoiNhan,
                        principalTable: "TaiKhoan",
                        principalColumn: "MaTaiKhoan",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BanDoc_MaSoThe",
                table: "BanDoc",
                column: "MaSoThe",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BanDoc_MaTaiKhoan",
                table: "BanDoc",
                column: "MaTaiKhoan",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_GiaHanMuon_ChoDuyet",
                table: "GiaHanMuon",
                column: "MaPhieuMuon",
                unique: true,
                filter: "[TrangThai] = N'ChoDuyet'");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuMuon_MaBanDoc_TrangThai",
                table: "PhieuMuon",
                columns: new[] { "MaBanDoc", "TrangThai" });

            migrationBuilder.CreateIndex(
                name: "IX_PhieuMuon_MaSach_TrangThai",
                table: "PhieuMuon",
                columns: new[] { "MaSach", "TrangThai" });

            migrationBuilder.CreateIndex(
                name: "UX_PhieuMuon_BanDoc_Sach_DangXuLy",
                table: "PhieuMuon",
                columns: new[] { "MaBanDoc", "MaSach" },
                unique: true,
                filter: "[TrangThai] IN (N'ChoDuyet', N'DaDuyet', N'DangMuon')");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuMuonLichSu_MaNguoiThucHien",
                table: "PhieuMuonLichSu",
                column: "MaNguoiThucHien");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuMuonLichSu_MaPhieuMuon",
                table: "PhieuMuonLichSu",
                column: "MaPhieuMuon");

            migrationBuilder.CreateIndex(
                name: "IX_Sach_MaTheLoai",
                table: "Sach",
                column: "MaTheLoai");

            migrationBuilder.CreateIndex(
                name: "IX_Sach_TenSach",
                table: "Sach",
                column: "TenSach");

            migrationBuilder.CreateIndex(
                name: "IX_Sach_TrangThai",
                table: "Sach",
                column: "TrangThai");

            migrationBuilder.CreateIndex(
                name: "IX_TaiKhoan_TenDangNhap",
                table: "TaiKhoan",
                column: "TenDangNhap",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TheLoai_TenTheLoai",
                table: "TheLoai",
                column: "TenTheLoai",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TraSach_MaNguoiNhan",
                table: "TraSach",
                column: "MaNguoiNhan");

            migrationBuilder.CreateIndex(
                name: "IX_TraSach_MaPhieuMuon",
                table: "TraSach",
                column: "MaPhieuMuon",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CauHinhThuVien");

            migrationBuilder.DropTable(
                name: "GiaHanMuon");

            migrationBuilder.DropTable(
                name: "OTPs");

            migrationBuilder.DropTable(
                name: "PhieuMuonLichSu");

            migrationBuilder.DropTable(
                name: "TraSach");

            migrationBuilder.DropTable(
                name: "PhieuMuon");

            migrationBuilder.DropTable(
                name: "BanDoc");

            migrationBuilder.DropTable(
                name: "Sach");

            migrationBuilder.DropTable(
                name: "TaiKhoan");

            migrationBuilder.DropTable(
                name: "TheLoai");

            migrationBuilder.DropSequence(
                name: "SoTheSeq");
        }
    }
}
