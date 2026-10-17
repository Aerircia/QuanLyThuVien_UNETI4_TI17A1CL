// Họ và tên: Vũ Tiến Thành
// Mã sinh viên: 23103100257
// Nội dung thực hiện: Xây dựng service cho việc quản lý sách
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien_UNETI4_TI17A1CL.Data;
using QuanLyThuVien_UNETI4_TI17A1CL.Models;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Common;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services;

public sealed class SachService : ISachService
{
    private readonly AppDbContext _db;

    public SachService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<SachListItem>> GetListAsync(SachFilter filter, CancellationToken ct = default)
    {
        var query = _db.Sachs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(s => s.TenSach.Contains(search) ||
                                     s.TacGia.Contains(search) ||
                                     (s.NhaXuatBan != null && s.NhaXuatBan.Contains(search)));
        }

        if (filter.MaTheLoai is { } maTheLoai)
            query = query.Where(s => s.MaTheLoai == maTheLoai);

        if (filter.TrangThai is { } trangThai)
            query = query.Where(s => s.TrangThai == trangThai);

        var projected = query
            .OrderBy(s => s.TenSach)
            .ThenBy(s => s.MaSach)
            .Select(s => new SachListItem(
                s.MaSach,
                s.TenSach,
                s.TacGia,
                s.NhaXuatBan,
                s.NamXuatBan,
                s.MaTheLoai,
                s.TheLoai!.TenTheLoai,
                s.SoLuongTong,
                s.GiaBia,
                s.TrangThai));

        return await PagedResult<SachListItem>.CreateAsync(projected, filter.Page, filter.PageSize, ct);
    }

    public async Task<SachDetail?> GetAsync(int maSach, CancellationToken ct = default)
    {
        return await _db.Sachs
            .AsNoTracking()
            .Where(s => s.MaSach == maSach)
            .Select(s => new SachDetail(
                s.MaSach,
                s.MaTheLoai,
                s.TheLoai!.TenTheLoai,
                s.TenSach,
                s.TacGia,
                s.NhaXuatBan,
                s.NamXuatBan,
                s.SoLuongTong,
                s.GiaBia,
                s.ViTriKe,
                s.MoTa,
                s.TrangThai,
                s.ImgPath))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<ServiceResult<int>> CreateAsync(CreateSachInput input, CancellationToken ct = default)
    {
        var result = Validate(input.MaTheLoai, input.TenSach, input.TacGia, input.NhaXuatBan,
            input.NamXuatBan, input.SoLuongTong, input.GiaBia, input.ViTriKe,
            input.MoTa, input.TrangThai, input.ImgPath);
        if (!await IsUsableCategoryAsync(input.MaTheLoai, ct))
            result.AddError(nameof(input.MaTheLoai), "Thể loại không tồn tại hoặc đã ngừng hoạt động.");

        if (!result.Succeeded)
            return result;

        var sach = new Sach
        {
            MaTheLoai = input.MaTheLoai,
            TenSach = input.TenSach.Trim(),
            TacGia = input.TacGia.Trim(),
            NhaXuatBan = Normalize(input.NhaXuatBan),
            NamXuatBan = input.NamXuatBan,
            SoLuongTong = input.SoLuongTong,
            GiaBia = input.GiaBia,
            ViTriKe = Normalize(input.ViTriKe),
            MoTa = Normalize(input.MoTa),
            TrangThai = input.TrangThai,
            ImgPath = Normalize(input.ImgPath)
        };

        _db.Sachs.Add(sach);
        await _db.SaveChangesAsync(ct);
        return ServiceResult<int>.Ok(sach.MaSach);
    }

    public async Task<ServiceResult> UpdateAsync(UpdateSachInput input, CancellationToken ct = default)
    {
        var sach = await _db.Sachs.FirstOrDefaultAsync(s => s.MaSach == input.MaSach, ct);
        if (sach is null)
            return ServiceResult.Fail("Không tìm thấy sách.");

        var result = Validate(input.MaTheLoai, input.TenSach, input.TacGia, input.NhaXuatBan,
            input.NamXuatBan, input.SoLuongTong, input.GiaBia, input.ViTriKe,
            input.MoTa, input.TrangThai, input.ImgPath);
        if (!await IsUsableCategoryForUpdateAsync(input.MaTheLoai, sach.MaTheLoai, ct))
            result.AddError(nameof(input.MaTheLoai), "Thể loại không tồn tại hoặc đã ngừng hoạt động.");

        if (!result.Succeeded)
            return result;

        sach.MaTheLoai = input.MaTheLoai;
        sach.TenSach = input.TenSach.Trim();
        sach.TacGia = input.TacGia.Trim();
        sach.NhaXuatBan = Normalize(input.NhaXuatBan);
        sach.NamXuatBan = input.NamXuatBan;
        sach.SoLuongTong = input.SoLuongTong;
        sach.GiaBia = input.GiaBia;
        sach.ViTriKe = Normalize(input.ViTriKe);
        sach.MoTa = Normalize(input.MoTa);
        sach.TrangThai = input.TrangThai;
        sach.ImgPath = Normalize(input.ImgPath);

        await _db.SaveChangesAsync(ct);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> DeleteAsync(int maSach, CancellationToken ct = default)
    {
        var sach = await _db.Sachs.FirstOrDefaultAsync(s => s.MaSach == maSach, ct);
        if (sach is null)
            return ServiceResult.Fail("Không tìm thấy sách.");

        if (await _db.PhieuMuons.AnyAsync(p => p.MaSach == maSach, ct))
            return ServiceResult.Fail("Không thể xóa sách đã phát sinh phiếu mượn; hãy chuyển trạng thái sang ngừng phục vụ.");

        _db.Sachs.Remove(sach);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            if (await _db.PhieuMuons.AnyAsync(p => p.MaSach == maSach, ct))
                return ServiceResult.Fail("Không thể xóa sách đã phát sinh phiếu mượn; hãy chuyển trạng thái sang ngừng phục vụ.");
            throw;
        }
        return ServiceResult.Success();
    }

    private async Task<bool> IsUsableCategoryAsync(int maTheLoai, CancellationToken ct) =>
        await _db.TheLoais.AnyAsync(t =>
            t.MaTheLoai == maTheLoai && t.TrangThai == TrangThaiTheLoai.HoatDong, ct);

    private async Task<bool> IsUsableCategoryForUpdateAsync(
        int maTheLoai,
        int currentMaTheLoai,
        CancellationToken ct)
    {
        var category = await _db.TheLoais.AsNoTracking()
            .Where(t => t.MaTheLoai == maTheLoai)
            .Select(t => new { t.MaTheLoai, t.TrangThai })
            .FirstOrDefaultAsync(ct);

        return category is not null &&
               (category.TrangThai == TrangThaiTheLoai.HoatDong || category.MaTheLoai == currentMaTheLoai);
    }

    private static ServiceResult<int> Validate(
        int maTheLoai,
        string tenSach,
        string tacGia,
        string? nhaXuatBan,
        int namXuatBan,
        int soLuongTong,
        decimal giaBia,
        string? viTriKe,
        string? moTa,
        TrangThaiSach trangThai,
        string? imgPath)
    {
        var result = new ServiceResult<int>();
        var ten = (tenSach ?? string.Empty).Trim();
        var tacGiaTrimmed = (tacGia ?? string.Empty).Trim();

        if (maTheLoai <= 0)
            result.AddError(nameof(Sach.MaTheLoai), "Vui lòng chọn thể loại.");
        if (ten.Length == 0)
            result.AddError(nameof(Sach.TenSach), "Tên sách là bắt buộc.");
        else if (ten.Length > 255)
            result.AddError(nameof(Sach.TenSach), "Tên sách tối đa 255 ký tự.");
        if (tacGiaTrimmed.Length == 0)
            result.AddError(nameof(Sach.TacGia), "Tác giả là bắt buộc.");
        else if (tacGiaTrimmed.Length > 255)
            result.AddError(nameof(Sach.TacGia), "Tác giả tối đa 255 ký tự.");
        if (namXuatBan < 1900 || namXuatBan > DateTime.Today.Year)
            result.AddError(nameof(Sach.NamXuatBan), "Năm xuất bản phải từ 1900 đến năm hiện tại.");
        if (soLuongTong <= 0)
            result.AddError(nameof(Sach.SoLuongTong), "Số lượng tổng phải lớn hơn 0.");
        if (giaBia < 0)
            result.AddError(nameof(Sach.GiaBia), "Giá bìa phải lớn hơn hoặc bằng 0.");
        if (nhaXuatBan?.Length > 255)
            result.AddError(nameof(Sach.NhaXuatBan), "Nhà xuất bản tối đa 255 ký tự.");
        if (viTriKe?.Length > 255)
            result.AddError(nameof(Sach.ViTriKe), "Vị trí kệ tối đa 255 ký tự.");
        if (moTa?.Length > 2000)
            result.AddError(nameof(Sach.MoTa), "Mô tả tối đa 2000 ký tự.");
        if (imgPath?.Length > 255)
            result.AddError(nameof(Sach.ImgPath), "Đường dẫn ảnh tối đa 255 ký tự.");
        if (!Enum.IsDefined(trangThai))
            result.AddError(nameof(Sach.TrangThai), "Trạng thái sách không hợp lệ.");

        return result;
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
