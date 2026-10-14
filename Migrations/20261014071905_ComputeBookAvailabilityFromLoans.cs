// Họ và tên: Vũ Tiến Thành
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Xóa cột số lượng còn lại đã lưu; tồn kho được tính từ tổng số bản và phiếu mượn đang xử lý.

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyThuVien_UNETI4_TI17A1CL.Migrations
{
    /// <inheritdoc />
    public partial class ComputeBookAvailabilityFromLoans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Sach_SoLuongConLai",
                table: "Sach");

            migrationBuilder.DropColumn(
                name: "SoLuongConLai",
                table: "Sach");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SoLuongConLai",
                table: "Sach",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Sach_SoLuongConLai",
                table: "Sach",
                sql: "[SoLuongConLai] >= 0 AND [SoLuongConLai] <= [SoLuongTong]");
        }
    }
}
