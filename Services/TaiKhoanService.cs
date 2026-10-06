// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Xây dựng Service quản lý tài khoản

using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien_UNETI4_TI17A1CL.Data;
using QuanLyThuVien_UNETI4_TI17A1CL.Models;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Common;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services;

public class TaiKhoanService : ITaiKhoanService
{
    private const string MsgNotFound = "Không tìm thấy tài khoản.";
    private const string MsgSaveFailed = "Không thể lưu thay đổi. Vui lòng thử lại.";
    private const string MsgLastAdmin = "Hệ thống phải luôn còn ít nhất một tài khoản Admin đang hoạt động.";

    private readonly AppDbContext _db;
    private readonly IPasswordHasher<TaiKhoan> _hasher;

    public TaiKhoanService(AppDbContext db, IPasswordHasher<TaiKhoan> hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    // ------------------------------------------------------------------ Danh sách / chi tiết

    public async Task<PagedResult<TaiKhoanListItem>> GetListAsync(TaiKhoanFilter filter, CancellationToken ct = default)
    {
        var query = _db.Set<TaiKhoan>().AsNoTracking();
        var banDocs = _db.Set<BanDoc>().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.Trim();
            query = query.Where(t => t.TenDangNhap.Contains(s) || t.HoTen.Contains(s) || t.Email.Contains(s));
        }

        if (filter.VaiTro is { } vaiTro)
            query = query.Where(t => t.VaiTro == vaiTro);

        if (filter.TrangThai is { } trangThai)
            query = query.Where(t => t.TrangThai == trangThai);

        var projected = query
            .OrderBy(t => t.TenDangNhap)
            .ThenBy(t => t.MaTaiKhoan)
            .Select(t => new TaiKhoanListItem(
                t.MaTaiKhoan,
                t.TenDangNhap,
                t.HoTen,
                t.Email,
                t.VaiTro,
                t.TrangThai,
                banDocs.Any(b => b.MaTaiKhoan == t.MaTaiKhoan)));

        return await PagedResult<TaiKhoanListItem>.CreateAsync(projected, filter.Page, filter.PageSize, ct);
    }

    public async Task<TaiKhoanDetail?> GetAsync(int maTaiKhoan, CancellationToken ct = default)
    {
        var banDocs = _db.Set<BanDoc>().AsNoTracking();

        return await _db.Set<TaiKhoan>()
            .AsNoTracking()
            .Where(t => t.MaTaiKhoan == maTaiKhoan)
            .Select(t => new TaiKhoanDetail(
                t.MaTaiKhoan,
                t.TenDangNhap,
                t.HoTen,
                t.Email,
                t.VaiTro,
                t.TrangThai,
                banDocs.Any(b => b.MaTaiKhoan == t.MaTaiKhoan)))
            .FirstOrDefaultAsync(ct);
    }

    // ------------------------------------------------------------------ Tạo

    public async Task<ServiceResult<int>> CreateAsync(CreateTaiKhoanInput input, CancellationToken ct = default)
    {
        var tenDangNhap = (input.TenDangNhap ?? string.Empty).Trim();
        var hoTen = (input.HoTen ?? string.Empty).Trim();
        var email = (input.Email ?? string.Empty).Trim();

        var result = new ServiceResult<int>();

        if (string.IsNullOrWhiteSpace(input.MatKhau))
            result.AddError(nameof(input.MatKhau), "Vui lòng nhập mật khẩu.");

        if (!Enum.IsDefined(input.VaiTro))
            result.AddError(nameof(input.VaiTro), "Vai trò không hợp lệ.");

        if (!Enum.IsDefined(input.TrangThai))
            result.AddError(nameof(input.TrangThai), "Trạng thái không hợp lệ.");

        if (await _db.Set<TaiKhoan>().AnyAsync(t => t.TenDangNhap == tenDangNhap, ct))
            result.AddError(nameof(input.TenDangNhap), "Tên đăng nhập đã tồn tại.");

        if (!await IsEmailAvailableAsync(email, null, ct))
            result.AddError(nameof(input.Email), "Email đã được sử dụng bởi tài khoản khác.");

        if (!result.Succeeded)
            return result;

        var tk = new TaiKhoan
        {
            TenDangNhap = tenDangNhap,
            HoTen = hoTen,
            Email = email,
            VaiTro = input.VaiTro,
            TrangThai = input.TrangThai,
            MatKhau = string.Empty
        };
        tk.MatKhau = _hasher.HashPassword(tk, input.MatKhau);

        _db.Set<TaiKhoan>().Add(tk);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Hai request cùng tạo một tên đăng nhập: chỉ index unique của DB mới chặn được.
            return ServiceResult<int>.Error(nameof(input.TenDangNhap), "Tên đăng nhập đã tồn tại.");
        }

        return ServiceResult<int>.Ok(tk.MaTaiKhoan);
    }

    // ------------------------------------------------------------------ Sửa

    public async Task<ServiceResult> UpdateAsync(UpdateTaiKhoanInput input, int actingMaTaiKhoan, CancellationToken ct = default)
    {
        var hoTen = (input.HoTen ?? string.Empty).Trim();
        var email = (input.Email ?? string.Empty).Trim();

        // Serializable: hai Admin thao tác cùng lúc không thể cùng vượt qua kiểm tra "còn Admin".
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        var tk = await _db.Set<TaiKhoan>().FirstOrDefaultAsync(t => t.MaTaiKhoan == input.MaTaiKhoan, ct);
        if (tk is null)
            return ServiceResult.Fail(MsgNotFound);

        var banDoc = await _db.Set<BanDoc>().FirstOrDefaultAsync(b => b.MaTaiKhoan == tk.MaTaiKhoan, ct);
        var isSelf = actingMaTaiKhoan == tk.MaTaiKhoan;
        var result = new ServiceResult();

        if (!Enum.IsDefined(input.VaiTro))
            result.AddError(nameof(input.VaiTro), "Vai trò không hợp lệ.");
        if (!Enum.IsDefined(input.TrangThai))
            result.AddError(nameof(input.TrangThai), "Trạng thái không hợp lệ.");

        if (input.VaiTro != tk.VaiTro)
        {
            if (banDoc is not null)
                result.AddError(nameof(input.VaiTro), "Không thể đổi vai trò của tài khoản đã có hồ sơ bạn đọc.");
            else if (isSelf)
                result.AddError(nameof(input.VaiTro), "Bạn không thể tự đổi vai trò của chính mình.");
        }

        if (isSelf && input.TrangThai == TrangThaiTaiKhoan.BiKhoa && tk.TrangThai != TrangThaiTaiKhoan.BiKhoa)
            result.AddError(nameof(input.TrangThai), "Bạn không thể tự khóa tài khoản của chính mình.");

        if (!await IsEmailAvailableAsync(email, tk.MaTaiKhoan, ct))
            result.AddError(nameof(input.Email), "Email đã được sử dụng bởi tài khoản khác.");

        var isActiveAdmin = tk.VaiTro == VaiTro.Admin && tk.TrangThai == TrangThaiTaiKhoan.HoatDong;
        var staysActiveAdmin = input.VaiTro == VaiTro.Admin && input.TrangThai == TrangThaiTaiKhoan.HoatDong;
        if (isActiveAdmin && !staysActiveAdmin && !await HasOtherActiveAdminAsync(tk.MaTaiKhoan, ct))
            result.AddError(string.Empty, MsgLastAdmin);

        if (!result.Succeeded)
            return result;

        tk.HoTen = hoTen;
        tk.Email = email;
        tk.VaiTro = input.VaiTro;
        tk.TrangThai = input.TrangThai;

        // BanDoc là nơi lưu chính; hai bản luôn giống nhau, lưu cùng một SaveChanges.
        if (banDoc is not null)
        {
            banDoc.HoTen = hoTen;
            banDoc.Email = email;
        }

        try
        {
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (DbUpdateException)
        {
            return ServiceResult.Fail(MsgSaveFailed);
        }

        return ServiceResult.Success();
    }

    // ------------------------------------------------------------------ Khóa / mở khóa

    public async Task<ServiceResult> SetStatusAsync(
        int maTaiKhoan, TrangThaiTaiKhoan trangThai, int actingMaTaiKhoan, CancellationToken ct = default)
    {
        if (!Enum.IsDefined(trangThai))
            return ServiceResult.Fail("Trạng thái không hợp lệ.");

        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        var tk = await _db.Set<TaiKhoan>().FirstOrDefaultAsync(t => t.MaTaiKhoan == maTaiKhoan, ct);
        if (tk is null)
            return ServiceResult.Fail(MsgNotFound);

        if (tk.TrangThai == trangThai)
            return ServiceResult.Success();

        if (trangThai == TrangThaiTaiKhoan.BiKhoa)
        {
            if (actingMaTaiKhoan == tk.MaTaiKhoan)
                return ServiceResult.Fail("Bạn không thể tự khóa tài khoản của chính mình.");

            var isActiveAdmin = tk.VaiTro == VaiTro.Admin && tk.TrangThai == TrangThaiTaiKhoan.HoatDong;
            if (isActiveAdmin && !await HasOtherActiveAdminAsync(tk.MaTaiKhoan, ct))
                return ServiceResult.Fail(MsgLastAdmin);
        }

        tk.TrangThai = trangThai;
        try
        {
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (DbUpdateException)
        {
            return ServiceResult.Fail(MsgSaveFailed);
        }

        return ServiceResult.Success();
    }

    // ------------------------------------------------------------------ Đặt lại mật khẩu (Admin)

    public async Task<ServiceResult> ResetPasswordAsync(int maTaiKhoan, string matKhauMoi, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(matKhauMoi))
            return ServiceResult.Fail("MatKhauMoi", "Vui lòng nhập mật khẩu mới.");

        var tk = await _db.Set<TaiKhoan>().FirstOrDefaultAsync(t => t.MaTaiKhoan == maTaiKhoan, ct);
        if (tk is null)
            return ServiceResult.Fail(MsgNotFound);

        tk.MatKhau = _hasher.HashPassword(tk, matKhauMoi);
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Success();
    }

    // ------------------------------------------------------------------ Xóa

    public async Task<ServiceResult> DeleteAsync(int maTaiKhoan, int actingMaTaiKhoan, CancellationToken ct = default)
    {
        if (actingMaTaiKhoan == maTaiKhoan)
            return ServiceResult.Fail("Bạn không thể xóa chính tài khoản đang đăng nhập.");

        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        var tk = await _db.Set<TaiKhoan>().FirstOrDefaultAsync(t => t.MaTaiKhoan == maTaiKhoan, ct);
        if (tk is null)
            return ServiceResult.Fail(MsgNotFound);

        var isActiveAdmin = tk.VaiTro == VaiTro.Admin && tk.TrangThai == TrangThaiTaiKhoan.HoatDong;
        if (isActiveAdmin && !await HasOtherActiveAdminAsync(tk.MaTaiKhoan, ct))
            return ServiceResult.Fail(MsgLastAdmin);

        // Lịch sử mượn trả phải được giữ nguyên: có phiếu mượn thì chỉ cho khóa, không cho xóa.
        var banDoc = await _db.Set<BanDoc>().FirstOrDefaultAsync(b => b.MaTaiKhoan == tk.MaTaiKhoan, ct);
        if (banDoc is not null &&
            await _db.Set<PhieuMuon>().AnyAsync(p => p.MaBanDoc == banDoc.MaBanDoc, ct))
        {
            return ServiceResult.Fail("Bạn đọc của tài khoản này đã có phiếu mượn. Hãy khóa tài khoản thay vì xóa để giữ lịch sử.");
        }

        // Thủ thư đã nhận trả sách / chuyển trạng thái phiếu thì tài khoản được lưu trong lịch sử.
        if (await _db.Set<TraSach>().AnyAsync(r => r.MaNguoiNhan == tk.MaTaiKhoan, ct) ||
            await _db.Set<PhieuMuonLichSu>().AnyAsync(h => h.MaNguoiThucHien == tk.MaTaiKhoan, ct))
        {
            return ServiceResult.Fail("Tài khoản này đã xử lý phiếu mượn/trả sách. Hãy khóa tài khoản thay vì xóa để giữ lịch sử.");
        }

        if (banDoc is not null)
            _db.Set<BanDoc>().Remove(banDoc);
        _db.Set<TaiKhoan>().Remove(tk);

        try
        {
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (DbUpdateException)
        {
            return ServiceResult.Fail("Không thể xóa tài khoản vì còn dữ liệu liên quan. Hãy khóa tài khoản thay vì xóa.");
        }

        return ServiceResult.Success();
    }

    // ------------------------------------------------------------------ Dùng chung

    public async Task<bool> IsEmailAvailableAsync(string email, int? excludeMaTaiKhoan = null, CancellationToken ct = default)
    {
        email = (email ?? string.Empty).Trim();

        return !await _db.Set<TaiKhoan>()
            .AnyAsync(t => t.Email == email &&
                           (excludeMaTaiKhoan == null || t.MaTaiKhoan != excludeMaTaiKhoan), ct);
    }

    private Task<bool> HasOtherActiveAdminAsync(int excludeMaTaiKhoan, CancellationToken ct) =>
        _db.Set<TaiKhoan>().AnyAsync(t =>
            t.MaTaiKhoan != excludeMaTaiKhoan &&
            t.VaiTro == VaiTro.Admin &&
            t.TrangThai == TrangThaiTaiKhoan.HoatDong, ct);
}
