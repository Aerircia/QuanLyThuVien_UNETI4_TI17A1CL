// Quy tắc đủ điều kiện của bạn đọc, dùng chung cho mượn sách, giao sách và gia hạn.

using Microsoft.EntityFrameworkCore;
using QuanLyThuVien_UNETI4_TI17A1CL.Data;
using QuanLyThuVien_UNETI4_TI17A1CL.Models;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services;

public static class ReaderEligibility
{
    /// <param name="excludeMaPhieuMuon">
    /// Phiếu đang xét (ví dụ phiếu xin gia hạn): không tính phiếu đó là "quá hạn" lần nữa,
    /// vì điều kiện phiếu quá hạn đã được kiểm tra riêng.
    /// </param>
    public static async Task<List<string>> GetBlockersAsync(
        AppDbContext db, BanDoc banDoc, DateOnly today, CancellationToken ct, int? excludeMaPhieuMuon = null)
    {
        var errors = new List<string>();
        var exclude = excludeMaPhieuMuon ?? 0;

        if (banDoc.TrangThai != TrangThaiBanDoc.HoatDong)
            errors.Add("Hồ sơ bạn đọc đang chờ duyệt, bị từ chối, tạm khóa hoặc ngừng hoạt động.");

        if (banDoc.NgayHetHanThe < today)
            errors.Add($"Thẻ thư viện đã hết hạn từ ngày {banDoc.NgayHetHanThe:dd/MM/yyyy}.");

        var thongTinThieu = new List<string>();
        if (string.IsNullOrWhiteSpace(banDoc.HoTen)) thongTinThieu.Add("Họ tên");
        if (string.IsNullOrWhiteSpace(banDoc.SoDienThoai)) thongTinThieu.Add("Số điện thoại");
        if (string.IsNullOrWhiteSpace(banDoc.Email)) thongTinThieu.Add("Email");
        if (string.IsNullOrWhiteSpace(banDoc.DiaChi)) thongTinThieu.Add("Địa chỉ");
        if (thongTinThieu.Count > 0)
            errors.Add($"Hồ sơ chưa khai báo đủ thông tin bắt buộc ({string.Join(", ", thongTinThieu)}).");

        if (await db.PhieuMuons.AnyAsync(p => p.MaBanDoc == banDoc.MaBanDoc &&
                                              p.MaPhieuMuon != exclude &&
                                              p.TrangThai == TrangThaiPhieuMuon.DangMuon &&
                                              p.HanTra.HasValue && p.HanTra.Value < today, ct))
            errors.Add("Đang có sách mượn quá hạn chưa trả, cần trả sách quá hạn trước khi tiếp tục.");

        if (await db.PhieuMuons.AnyAsync(p => p.MaBanDoc == banDoc.MaBanDoc &&
                                              p.TraSach != null &&
                                              p.TraSach.TrangThaiPhat == TrangThaiPhat.ChuaNop, ct))
            errors.Add("Đang có khoản tiền phạt chưa nộp, cần thanh toán tại thư viện trước khi tiếp tục.");

        return errors;
    }
}
