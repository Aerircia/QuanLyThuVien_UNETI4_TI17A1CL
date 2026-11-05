// Họ và tên: Vũ Tiến Thành
// Mã sinh viên: 23103100257
// Nội dung thực hiện: Xây dựng service cho việc quản lý thể loại
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien_UNETI4_TI17A1CL.Data;
using QuanLyThuVien_UNETI4_TI17A1CL.Models;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Common;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services;

public sealed class TheLoaiService : ITheLoaiService
{
    private readonly AppDbContext _db;

    public TheLoaiService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<TheLoaiListItem>> GetListAsync(TheLoaiFilter filter, CancellationToken ct = default)
    {
        var query = _db.TheLoais.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(t => t.TenTheLoai.Contains(search) ||
                                     (t.MoTa != null && t.MoTa.Contains(search)) ||
                                     (t.KhuVucLuuTru != null && t.KhuVucLuuTru.Contains(search)));
        }

        if (filter.TrangThai is { } trangThai)
            query = query.Where(t => t.TrangThai == trangThai);

        var projected = query
            .OrderBy(t => t.TenTheLoai)
            .ThenBy(t => t.MaTheLoai)
            .Select(t => new TheLoaiListItem(
                t.MaTheLoai,
                t.TenTheLoai,
                t.MoTa,
                t.KhuVucLuuTru,
                t.TrangThai,
                t.Sachs.Count));

        return await PagedResult<TheLoaiListItem>.CreateAsync(projected, filter.Page, filter.PageSize, ct);
    }

    public async Task<IReadOnlyList<TheLoaiOption>> GetActiveOptionsAsync(CancellationToken ct = default)
    {
        return await _db.TheLoais
            .AsNoTracking()
            .Where(t => t.TrangThai == TrangThaiTheLoai.HoatDong)
            .OrderBy(t => t.TenTheLoai)
            .ThenBy(t => t.MaTheLoai)
            .Select(t => new TheLoaiOption(t.MaTheLoai, t.TenTheLoai))
            .ToListAsync(ct);
    }

    public async Task<TheLoaiDetail?> GetAsync(int maTheLoai, CancellationToken ct = default)
    {
        return await _db.TheLoais
            .AsNoTracking()
            .Where(t => t.MaTheLoai == maTheLoai)
            .Select(t => new TheLoaiDetail(
                t.MaTheLoai,
                t.TenTheLoai,
                t.MoTa,
                t.KhuVucLuuTru,
                t.TrangThai))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<ServiceResult<int>> CreateAsync(CreateTheLoaiInput input, CancellationToken ct = default)
    {
        var ten = (input.TenTheLoai ?? string.Empty).Trim();
        var result = Validate(ten, input.MoTa, input.KhuVucLuuTru, input.TrangThai);
        if (await TenDaTonTaiAsync(ten, null, ct))
            result.AddError(nameof(input.TenTheLoai), TenTrungLoi);

        if (!result.Succeeded)
            return result;

        var theLoai = new TheLoai
        {
            TenTheLoai = ten,
            MoTa = Normalize(input.MoTa),
            KhuVucLuuTru = Normalize(input.KhuVucLuuTru),
            TrangThai = input.TrangThai
        };
        _db.TheLoais.Add(theLoai);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            if (await TenDaTonTaiAsync(ten, null, ct))
            {
                _db.Entry(theLoai).State = EntityState.Detached;
                return ServiceResult<int>.Error(nameof(input.TenTheLoai), TenTrungLoi);
            }
            throw;
        }

        return ServiceResult<int>.Ok(theLoai.MaTheLoai);
    }

    public async Task<ServiceResult> UpdateAsync(UpdateTheLoaiInput input, CancellationToken ct = default)
    {
        var theLoai = await _db.TheLoais.FirstOrDefaultAsync(t => t.MaTheLoai == input.MaTheLoai, ct);
        if (theLoai is null)
            return ServiceResult.Fail("Không tìm thấy thể loại.");

        var ten = (input.TenTheLoai ?? string.Empty).Trim();
        var result = Validate(ten, input.MoTa, input.KhuVucLuuTru, input.TrangThai);
        if (await TenDaTonTaiAsync(ten, input.MaTheLoai, ct))
            result.AddError(nameof(input.TenTheLoai), TenTrungLoi);

        if (!result.Succeeded)
            return result;

        theLoai.TenTheLoai = ten;
        theLoai.MoTa = Normalize(input.MoTa);
        theLoai.KhuVucLuuTru = Normalize(input.KhuVucLuuTru);
        theLoai.TrangThai = input.TrangThai;

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            if (await TenDaTonTaiAsync(ten, input.MaTheLoai, ct))
            {
                _db.Entry(theLoai).State = EntityState.Detached;
                return ServiceResult.Fail(nameof(input.TenTheLoai), TenTrungLoi);
            }
            throw;
        }

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> DeleteAsync(int maTheLoai, CancellationToken ct = default)
    {
        var theLoai = await _db.TheLoais.FirstOrDefaultAsync(t => t.MaTheLoai == maTheLoai, ct);
        if (theLoai is null)
            return ServiceResult.Fail("Không tìm thấy thể loại.");

        if (await _db.Sachs.AnyAsync(s => s.MaTheLoai == maTheLoai, ct))
            return ServiceResult.Fail("Không thể xóa thể loại đang có sách; hãy chuyển trạng thái sang ngừng hoạt động.");

        _db.TheLoais.Remove(theLoai);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            if (await _db.Sachs.AnyAsync(s => s.MaTheLoai == maTheLoai, ct))
                return ServiceResult.Fail("Không thể xóa thể loại đang có sách; hãy chuyển trạng thái sang ngừng hoạt động.");
            throw;
        }
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> SetTrangThaiAsync(int maTheLoai, TrangThaiTheLoai trangThai, CancellationToken ct = default)
    {
        var theLoai = await _db.TheLoais.FirstOrDefaultAsync(t => t.MaTheLoai == maTheLoai, ct);
        if (theLoai is null)
            return ServiceResult.Fail("Không tìm thấy thể loại.");

        if (!Enum.IsDefined(trangThai))
            return ServiceResult.Fail(nameof(TheLoai.TrangThai), "Trạng thái thể loại không hợp lệ.");
        if (theLoai.TrangThai == trangThai)
            return ServiceResult.Fail("Thể loại đang ở trạng thái này.");

        theLoai.TrangThai = trangThai;
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Success();
    }

    private const string TenTrungLoi = "Tên thể loại đã tồn tại.";

    private Task<bool> TenDaTonTaiAsync(string ten, int? excludeMaTheLoai, CancellationToken ct)
    {
        var exclude = excludeMaTheLoai;
        return _db.TheLoais.AnyAsync(t =>
            t.TenTheLoai == ten && (exclude == null || t.MaTheLoai != exclude), ct);
    }

    private static ServiceResult<int> Validate(
        string ten,
        string? moTa,
        string? khuVucLuuTru,
        TrangThaiTheLoai trangThai)
    {
        var result = new ServiceResult<int>();
        if (ten.Length == 0)
            result.AddError(nameof(TheLoai.TenTheLoai), "Tên thể loại là bắt buộc.");
        else if (ten.Length > 255)
            result.AddError(nameof(TheLoai.TenTheLoai), "Tên thể loại tối đa 255 ký tự.");

        if (moTa?.Length > 255)
            result.AddError(nameof(TheLoai.MoTa), "Mô tả tối đa 255 ký tự.");
        if (khuVucLuuTru?.Length > 100)
            result.AddError(nameof(TheLoai.KhuVucLuuTru), "Khu vực lưu trữ tối đa 100 ký tự.");
        if (!Enum.IsDefined(trangThai))
            result.AddError(nameof(TheLoai.TrangThai), "Trạng thái thể loại không hợp lệ.");

        return result;
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
