// Họ và tên: Vũ Tiến Thành
// Mã sinh viên: 23103100257
// Nội dung thực hiện: Xây dựng service cho việc quản lý sách
using System.Data;
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

        if (!string.IsNullOrWhiteSpace(filter.TacGia))
            query = query.Where(s => s.TacGia == filter.TacGia);

        if (filter.NamTu is { } namTu)
            query = query.Where(s => s.NamXuatBan >= namTu);

        if (filter.NamDen is { } namDen)
            query = query.Where(s => s.NamXuatBan <= namDen);

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
                s.TrangThai,
                s.ImgPath,
                s.PhieuMuons.Count(p => p.TrangThai == TrangThaiPhieuMuon.DaDuyet || p.TrangThai == TrangThaiPhieuMuon.DangMuon),
                s.PhieuMuons.Count(p => p.TraSach != null && p.TraSach.TinhTrangSach == TinhTrangSach.Mat)));

        return await PagedResult<SachListItem>.CreateAsync(projected, filter.Page, filter.PageSize, ct);
    }
    
    public async Task<IReadOnlyList<string>> GetAuthorOptionsAsync(CancellationToken ct = default)
    {
        return await _db.Sachs
            .AsNoTracking()
            .Select(s => s.TacGia)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync(ct);
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
                s.ImgPath,
                s.PhieuMuons.Count(p => p.TrangThai == TrangThaiPhieuMuon.DaDuyet || p.TrangThai == TrangThaiPhieuMuon.DangMuon),
                s.PhieuMuons.Count(p => p.TraSach != null && p.TraSach.TinhTrangSach == TinhTrangSach.Mat)))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<ServiceResult<int>> CreateAsync(CreateSachInput input, CancellationToken ct = default)
    {
        var result = Validate(new SachFields(
            input.MaTheLoai, input.TenSach, input.TacGia, input.NhaXuatBan, input.NamXuatBan,
            input.SoLuongTong, input.GiaBia, input.ViTriKe, input.MoTa, input.TrangThai, input.ImgPath));
        if (!await IsUsableCategoryAsync(input.MaTheLoai, ct))
            result.AddError(nameof(input.MaTheLoai), "Thể loại không tồn tại hoặc đã ngừng hoạt động.");

        if (!result.Succeeded)
            return result;

        var sach = new Sach
        {
            MaTheLoai = input.MaTheLoai,
            TenSach = input.TenSach.Trim(),
            TacGia = input.TacGia.Trim(),
            NhaXuatBan = Normalize(input.NhaXuatBan) ?? string.Empty,
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
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var sach = await _db.Sachs.FirstOrDefaultAsync(s => s.MaSach == input.MaSach, ct);
        if (sach is null)
            return ServiceResult.Fail("Không tìm thấy sách.");

var result = Validate(new SachFields(
            input.MaTheLoai, input.TenSach, input.TacGia, input.NhaXuatBan, input.NamXuatBan,
            input.SoLuongTong, input.GiaBia, input.ViTriKe, input.MoTa, input.TrangThai, input.ImgPath));

        if (!await IsUsableCategoryForUpdateAsync(input.MaTheLoai, sach.MaTheLoai, ct))
            result.AddError(nameof(input.MaTheLoai), "Thể loại không tồn tại hoặc đã ngừng hoạt động.");

        // Tổng số bản phải đủ cho cả bản đang giữ và bản đã mất.
        var soDangGiu = await DemDangGiuAsync(input.MaSach, ct);
        var soDaMat = await DemDaMatAsync(input.MaSach, ct);
        var soToiThieu = soDangGiu + soDaMat;
        if (input.SoLuongTong < soToiThieu)
            result.AddError(nameof(input.SoLuongTong),
                $"Tổng số lượng không được thấp hơn số bản đang giữ hoặc đã mất ({soToiThieu} bản: {soDangGiu} đang giữ, {soDaMat} đã mất).");
        if (input.TrangThai == TrangThaiSach.NgungPhucVu && soDangGiu > 0)
            result.AddError(nameof(input.TrangThai),
                "Còn phiếu mượn đang hiệu lực, chưa thể chuyển Ngừng phục vụ.");

        if (!result.Succeeded)
            return result;

        sach.MaTheLoai = input.MaTheLoai;
        sach.TenSach = input.TenSach.Trim();
        sach.TacGia = input.TacGia.Trim();
        sach.NhaXuatBan = Normalize(input.NhaXuatBan) ?? string.Empty;
        sach.NamXuatBan = input.NamXuatBan;
        sach.SoLuongTong = input.SoLuongTong;
        sach.GiaBia = input.GiaBia;
        sach.ViTriKe = Normalize(input.ViTriKe);
        sach.MoTa = Normalize(input.MoTa);
        sach.TrangThai = input.TrangThai;
        sach.ImgPath = Normalize(input.ImgPath);

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult<bool>> DeleteAsync(int maSach, CancellationToken ct = default)
    {
        var sach = await _db.Sachs.FirstOrDefaultAsync(s => s.MaSach == maSach, ct);
        if (sach is null)
            return ServiceResult<bool>.Error(string.Empty, "Không tìm thấy sách.");

        if (await DemDangGiuAsync(maSach, ct) > 0)
            return ServiceResult<bool>.Error(string.Empty, "Còn phiếu mượn đang hiệu lực, không thể xóa sách.");

        if (await _db.PhieuMuons.AnyAsync(p => p.MaSach == maSach, ct))
        {
            // Đã có lịch sử mượn: giữ dữ liệu, chỉ chuyển sang Ngừng phục vụ.
            sach.TrangThai = TrangThaiSach.NgungPhucVu;
            await _db.SaveChangesAsync(ct);
            return ServiceResult<bool>.Ok(false);
        }

        _db.Sachs.Remove(sach);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            if (await _db.PhieuMuons.AnyAsync(p => p.MaSach == maSach, ct))
            {
                _db.Entry(sach).State = EntityState.Detached;
                sach = await _db.Sachs.FirstAsync(s => s.MaSach == maSach, ct);
                sach.TrangThai = TrangThaiSach.NgungPhucVu;
                await _db.SaveChangesAsync(ct);
                return ServiceResult<bool>.Ok(false);
            }
            throw;
        }
        return ServiceResult<bool>.Ok(true);
    }
    
    public async Task<ServiceResult> ToggleTrangThaiAsync(int maSach, CancellationToken ct = default)
    {
        var sach = await _db.Sachs.FirstOrDefaultAsync(s => s.MaSach == maSach, ct);
        if (sach is null)
            return ServiceResult.Fail("Không tìm thấy sách.");

        if (sach.TrangThai == TrangThaiSach.NgungPhucVu)
            return ServiceResult.Fail("Sách đã ngừng phục vụ vĩnh viễn, không thể mở lại.");

        if (sach.TrangThai == TrangThaiSach.TamNgung)
        {
            sach.TrangThai = TrangThaiSach.DangPhucVu;
            await _db.SaveChangesAsync(ct);
            return ServiceResult.Success();
        }

        // DangPhucVu -> TamNgung: chặn khi còn phiếu mượn hiệu lực.
        if (await DemDangGiuAsync(maSach, ct) > 0)
            return ServiceResult.Fail("Còn phiếu mượn đang hiệu lực, chưa thể chuyển Tạm ngưng.");

        sach.TrangThai = TrangThaiSach.TamNgung;
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Success();
    }

    private Task<int> DemDangGiuAsync(int maSach, CancellationToken ct) =>
        _db.PhieuMuons.CountAsync(p =>
            p.MaSach == maSach &&
            (p.TrangThai == TrangThaiPhieuMuon.DaDuyet || p.TrangThai == TrangThaiPhieuMuon.DangMuon), ct);

    private Task<int> DemDaMatAsync(int maSach, CancellationToken ct) =>
        _db.TraSachs.CountAsync(t =>
            t.PhieuMuon != null && t.PhieuMuon.MaSach == maSach && t.TinhTrangSach == TinhTrangSach.Mat, ct);

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

    // Gom các trường nhập liệu của Create/Update thành một khối để Validate không phải nhận 11 tham số rời rạc.
    private sealed record SachFields(
        int MaTheLoai,
        string TenSach,
        string TacGia,
        string? NhaXuatBan,
        int NamXuatBan,
        int SoLuongTong,
        decimal GiaBia,
        string? ViTriKe,
        string? MoTa,
        TrangThaiSach TrangThai,
        string? ImgPath);

    private static ServiceResult<int> Validate(SachFields f)
    {
        var result = new ServiceResult<int>();
        var ten = (f.TenSach ?? string.Empty).Trim();
        var tacGiaTrimmed = (f.TacGia ?? string.Empty).Trim();

        if (f.MaTheLoai <= 0)
            result.AddError(nameof(Sach.MaTheLoai), "Vui lòng chọn thể loại.");
        if (ten.Length == 0)
            result.AddError(nameof(Sach.TenSach), "Tên sách là bắt buộc.");
        else if (ten.Length > 255)
            result.AddError(nameof(Sach.TenSach), "Tên sách tối đa 255 ký tự.");
        if (tacGiaTrimmed.Length == 0)
            result.AddError(nameof(Sach.TacGia), "Tác giả là bắt buộc.");
        else if (tacGiaTrimmed.Length > 255)
            result.AddError(nameof(Sach.TacGia), "Tác giả tối đa 255 ký tự.");
        if (f.NamXuatBan < 1900 || f.NamXuatBan > DateTime.Today.Year)
            result.AddError(nameof(Sach.NamXuatBan), "Năm xuất bản phải từ 1900 đến năm hiện tại.");
        if (f.SoLuongTong <= 0)
            result.AddError(nameof(Sach.SoLuongTong), "Số lượng tổng phải lớn hơn 0.");
        if (f.GiaBia < 0)
            result.AddError(nameof(Sach.GiaBia), "Giá bìa phải lớn hơn hoặc bằng 0.");
        if (f.NhaXuatBan?.Length > 255)
            result.AddError(nameof(Sach.NhaXuatBan), "Nhà xuất bản tối đa 255 ký tự.");
        if (f.ViTriKe?.Length > 255)
            result.AddError(nameof(Sach.ViTriKe), "Vị trí kệ tối đa 255 ký tự.");
        if (f.MoTa?.Length > 2000)
            result.AddError(nameof(Sach.MoTa), "Mô tả tối đa 2000 ký tự.");
        if (f.ImgPath?.Length > 255)
            result.AddError(nameof(Sach.ImgPath), "Đường dẫn ảnh tối đa 255 ký tự.");
        if (!Enum.IsDefined(f.TrangThai))
            result.AddError(nameof(Sach.TrangThai), "Trạng thái sách không hợp lệ.");

        return result;
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
