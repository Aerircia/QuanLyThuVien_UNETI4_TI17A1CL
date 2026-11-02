// Họ và tên: Nguyễn Hữu Tuấn
// Mã sinh viên: 23103100136
// Nội dung thực hiện: Xây dựng service xử lý nghiệp vụ quản lý bạn đọc

using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using QuanLyThuVien_UNETI4_TI17A1CL.Data;
using QuanLyThuVien_UNETI4_TI17A1CL.Models;
using QuanLyThuVien_UNETI4_TI17A1CL.Models.Enums;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Common;
using QuanLyThuVien_UNETI4_TI17A1CL.Services.Interfaces;
using QuanLyThuVien_UNETI4_TI17A1CL.ViewModels;

namespace QuanLyThuVien_UNETI4_TI17A1CL.Services;

public class BanDocService : IBanDocService
{
    private readonly AppDbContext _db;
    private readonly ITaiKhoanService _taiKhoanService;

    public BanDocService(AppDbContext db, ITaiKhoanService taiKhoanService)
    {
        _db = db;
        _taiKhoanService = taiKhoanService;
    }

    public async Task<PagedResult<BanDocListItem>> GetListAsync(BanDocFilter filter, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var query = _db.BanDocs
            .Include(b => b.TaiKhoan)
            .Include(b => b.PhieuMuons)
                .ThenInclude(p => p.TraSach)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.Trim();
            query = query.Where(b => b.HoTen.Contains(s)
                                  || (b.MaSoThe != null && b.MaSoThe.Contains(s))
                                  || (b.SoDienThoai != null && b.SoDienThoai.Contains(s))
                                  || (b.Email != null && b.Email.Contains(s))
                                  || (b.TaiKhoan != null && b.TaiKhoan.TenDangNhap.Contains(s)));
        }

        if (filter.LoaiBanDoc.HasValue)
            query = query.Where(b => b.LoaiBanDoc == filter.LoaiBanDoc.Value);

        if (filter.TrangThai.HasValue)
            query = query.Where(b => b.TrangThai == filter.TrangThai.Value);

        var projected = query
            .OrderBy(b => b.HoTen)
            .ThenBy(b => b.MaBanDoc)
            .Select(b => new BanDocListItem
            {
                MaBanDoc = b.MaBanDoc,
                MaTaiKhoan = b.MaTaiKhoan,
                HoTen = b.HoTen,
                NgaySinh = b.NgaySinh,
                GioiTinh = b.GioiTinh,
                DiaChi = b.DiaChi,
                MaSoThe = b.MaSoThe,
                LoaiBanDoc = b.LoaiBanDoc,
                NgayCapThe = b.NgayCapThe,
                NgayHetHanThe = b.NgayHetHanThe,
                TrangThai = b.TrangThai,
                SoDienThoai = b.SoDienThoai,
                Email = b.Email,
                TenDangNhap = b.TaiKhoan != null ? b.TaiKhoan.TenDangNhap : null,
                SoPhieuDangMuon = b.PhieuMuons.Count(p => p.TrangThai == TrangThaiPhieuMuon.DangMuon),
                SoPhieuQuaHan = b.PhieuMuons.Count(p => p.TrangThai == TrangThaiPhieuMuon.DangMuon && p.HanTra.HasValue && p.HanTra.Value < today),
                TongTienPhatChuaNop = b.PhieuMuons
                    .Where(p => p.TraSach != null && p.TraSach.TrangThaiPhat == TrangThaiPhat.ChuaNop)
                    .Sum(p => p.TraSach!.TienPhat)
            });

        return await PagedResult<BanDocListItem>.CreateAsync(projected, filter.Page, filter.PageSize, ct);
    }

    public async Task<BanDocDetail?> GetAsync(int maBanDoc, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var b = await _db.BanDocs
            .Include(x => x.TaiKhoan)
            .Include(x => x.PhieuMuons)
                .ThenInclude(p => p.Sach)
                    .ThenInclude(s => s!.TheLoai)
            .Include(x => x.PhieuMuons)
                .ThenInclude(p => p.TraSach)
            .Include(x => x.PhieuMuons)
                .ThenInclude(p => p.GiaHanMuons)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.MaBanDoc == maBanDoc, ct);

        if (b is null) return null;

        var phieuMuons = b.PhieuMuons
            .OrderByDescending(p => p.NgayDangKy)
            .ThenByDescending(p => p.MaPhieuMuon)
            .Select(p => new PhieuMuonItemViewModel
            {
                MaPhieuMuon = p.MaPhieuMuon,
                MaBanDoc = p.MaBanDoc,
                HoTenBanDoc = b.HoTen,
                MaSoThe = b.MaSoThe,
                MaSach = p.MaSach,
                TenSach = p.Sach?.TenSach ?? string.Empty,
                TacGia = p.Sach?.TacGia ?? string.Empty,
                TheLoai = p.Sach?.TheLoai?.TenTheLoai,
                ViTriKe = p.Sach?.ViTriKe,
                NgayDangKy = p.NgayDangKy,
                NgayMuon = p.NgayMuon,
                HanTra = p.HanTra,
                TrangThai = p.TrangThai,
                NgayXuLy = p.NgayXuLy,
                NhanXetThuThu = p.NhanXetThuThu,
                GhiChu = p.GhiChu,
                IsOverdue = p.TrangThai == TrangThaiPhieuMuon.DangMuon && p.HanTra.HasValue && p.HanTra.Value < today,
                SoNgayQuaHan = p.HanTra.HasValue ? Math.Max(0, today.DayNumber - p.HanTra.Value.DayNumber) : 0,
                SoNgayConLai = p.HanTra.HasValue ? Math.Max(0, p.HanTra.Value.DayNumber - today.DayNumber) : 0,
                CanCancel = p.TrangThai == TrangThaiPhieuMuon.ChoDuyet || (p.TrangThai == TrangThaiPhieuMuon.DaDuyet && !p.NgayMuon.HasValue),
                SoLanGiaHan = p.GiaHanMuons.Count(g => g.TrangThai == TrangThaiGiaHan.DaDuyet),
                TienPhat = p.TraSach?.TienPhat,
                TrangThaiPhat = p.TraSach?.TrangThaiPhat
            }).ToList();

        var khoanPhats = b.PhieuMuons
            .Where(p => p.TraSach != null && p.TraSach.TienPhat > 0)
            .OrderByDescending(p => p.TraSach!.NgayTra)
            .Select(p => new FineItemViewModel
            {
                MaTra = p.TraSach!.MaTra,
                MaPhieuMuon = p.MaPhieuMuon,
                TenSach = p.Sach?.TenSach ?? string.Empty,
                NgayTra = p.TraSach.NgayTra,
                TinhTrangSach = p.TraSach.TinhTrangSach,
                SoNgayTre = p.TraSach.SoNgayTre,
                TienPhat = p.TraSach.TienPhat,
                TrangThaiPhat = p.TraSach.TrangThaiPhat,
                GhiChu = p.TraSach.GhiChu
            }).ToList();

        return new BanDocDetail
        {
            MaBanDoc = b.MaBanDoc,
            MaTaiKhoan = b.MaTaiKhoan,
            TenDangNhap = b.TaiKhoan?.TenDangNhap,
            HoTen = b.HoTen,
            NgaySinh = b.NgaySinh,
            GioiTinh = b.GioiTinh,
            SoDienThoai = b.SoDienThoai,
            Email = b.Email,
            DiaChi = b.DiaChi,
            MaSoThe = b.MaSoThe,
            LoaiBanDoc = b.LoaiBanDoc,
            NgayCapThe = b.NgayCapThe,
            NgayHetHanThe = b.NgayHetHanThe,
            TrangThai = b.TrangThai,
            ImgPath = b.ImgPath,
            SoPhieuDangMuon = b.PhieuMuons.Count(p => p.TrangThai == TrangThaiPhieuMuon.DangMuon),
            SoPhieuQuaHan = b.PhieuMuons.Count(p => p.TrangThai == TrangThaiPhieuMuon.DangMuon && p.HanTra.HasValue && p.HanTra.Value < today),
            TongTienPhatChuaNop = khoanPhats.Where(f => f.TrangThaiPhat == TrangThaiPhat.ChuaNop).Sum(f => f.TienPhat),
            PhieuMuons = phieuMuons,
            KhoanPhats = khoanPhats
        };
    }

    public async Task<BanDocDetail?> GetByTaiKhoanAsync(int maTaiKhoan, CancellationToken ct = default)
    {
        var maBanDoc = await _db.BanDocs
            .Where(b => b.MaTaiKhoan == maTaiKhoan)
            .Select(b => (int?)b.MaBanDoc)
            .FirstOrDefaultAsync(ct);

        if (!maBanDoc.HasValue) return null;
        return await GetAsync(maBanDoc.Value, ct);
    }

    public async Task<ServiceResult<int>> CreateAsync(CreateBanDocInput input, CancellationToken ct = default)
    {
        var result = new ServiceResult<int>();
        var homNay = DateOnly.FromDateTime(DateTime.Today);

        // Validation
        var hoTen = (input.HoTen ?? string.Empty).Trim();
        var email = (input.Email ?? string.Empty).Trim();
        var soDienThoai = (input.SoDienThoai ?? string.Empty).Trim();
        var diaChi = (input.DiaChi ?? string.Empty).Trim();
        soDienThoai = Regex.Replace(soDienThoai, @"[\s\.\-]+", "");
        var maSoThe = string.IsNullOrWhiteSpace(input.MaSoThe) ? null : input.MaSoThe.Trim();

        if (string.IsNullOrWhiteSpace(hoTen))
            result.AddError(nameof(input.HoTen), "Họ tên là bắt buộc.");

        if (input.NgaySinh < new DateOnly(1900, 1, 1) || input.NgaySinh >= homNay)
            result.AddError(nameof(input.NgaySinh), "Ngày sinh không hợp lệ.");

        if (string.IsNullOrWhiteSpace(email) || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email))
            result.AddError(nameof(input.Email), "Email không đúng định dạng.");

        if (string.IsNullOrWhiteSpace(soDienThoai) || !Regex.IsMatch(soDienThoai, @"^0\d{9,10}$"))
            result.AddError(nameof(input.SoDienThoai), "Số điện thoại không hợp lệ (10-11 số, bắt đầu bằng 0).");

        if (input.NgayHetHanThe <= input.NgayCapThe)
            result.AddError(nameof(input.NgayHetHanThe), "Ngày hết hạn thẻ phải sau ngày cấp thẻ.");

        if (maSoThe != null && !await IsMaSoTheAvailableAsync(maSoThe, null, ct))
            result.AddError(nameof(input.MaSoThe), "Mã số thẻ đã tồn tại.");

        if (!input.MaTaiKhoan.HasValue)
            result.AddError(nameof(input.MaTaiKhoan), "Vui lòng chọn tài khoản liên kết.");
        else
        {
            var tk = await _db.TaiKhoans.FirstOrDefaultAsync(t => t.MaTaiKhoan == input.MaTaiKhoan.Value, ct);
            if (tk is null)
                result.AddError(nameof(input.MaTaiKhoan), "Tài khoản không tồn tại.");
            else if (tk.VaiTro != VaiTro.BanDoc)
                result.AddError(nameof(input.MaTaiKhoan), "Chỉ có thể tạo hồ sơ bạn đọc cho tài khoản có vai trò Bạn đọc.");
            else if (await _db.BanDocs.AnyAsync(b => b.MaTaiKhoan == tk.MaTaiKhoan, ct))
                result.AddError(nameof(input.MaTaiKhoan), "Tài khoản này đã có hồ sơ bạn đọc.");
        }

        if (!result.Succeeded)
            return result;

        var banDoc = new BanDoc
        {
            MaTaiKhoan = input.MaTaiKhoan!.Value,
            HoTen = hoTen,
            NgaySinh = input.NgaySinh,
            GioiTinh = input.GioiTinh,
            SoDienThoai = soDienThoai,
            Email = email,
            DiaChi = diaChi,
            MaSoThe = maSoThe, // Nếu null, SQL Server sequence SoTheSeq sẽ tự sinh
            LoaiBanDoc = input.LoaiBanDoc,
            NgayCapThe = input.NgayCapThe,
            NgayHetHanThe = input.NgayHetHanThe,
            TrangThai = input.TrangThai
        };

        // Đồng bộ họ tên và email sang TaiKhoan
        var taiKhoan = await _db.TaiKhoans.FindAsync(new object[] { input.MaTaiKhoan.Value }, ct);
        if (taiKhoan != null)
        {
            taiKhoan.HoTen = hoTen;
            taiKhoan.Email = email;
        }

        _db.BanDocs.Add(banDoc);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            return ServiceResult<int>.Error(nameof(input.MaSoThe), "Không thể lưu hồ sơ bạn đọc. Mã số thẻ có thể bị trùng.");
        }

        return ServiceResult<int>.Ok(banDoc.MaBanDoc);
    }

    public async Task<ServiceResult> UpdateAsync(UpdateBanDocInput input, CancellationToken ct = default)
    {
        var result = new ServiceResult();
        var homNay = DateOnly.FromDateTime(DateTime.Today);

        var hoTen = (input.HoTen ?? string.Empty).Trim();
        var email = (input.Email ?? string.Empty).Trim();
        var soDienThoai = (input.SoDienThoai ?? string.Empty).Trim();
        var diaChi = (input.DiaChi ?? string.Empty).Trim();
        soDienThoai = Regex.Replace(soDienThoai, @"[\s\.\-]+", "");
        var maSoThe = string.IsNullOrWhiteSpace(input.MaSoThe) ? null : input.MaSoThe.Trim();

        if (string.IsNullOrWhiteSpace(hoTen))
            result.AddError(nameof(input.HoTen), "Họ tên là bắt buộc.");

        if (input.NgaySinh < new DateOnly(1900, 1, 1) || input.NgaySinh >= homNay)
            result.AddError(nameof(input.NgaySinh), "Ngày sinh không hợp lệ.");

        if (string.IsNullOrWhiteSpace(email) || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email))
            result.AddError(nameof(input.Email), "Email không đúng định dạng.");

        if (string.IsNullOrWhiteSpace(soDienThoai) || !Regex.IsMatch(soDienThoai, @"^0\d{9,10}$"))
            result.AddError(nameof(input.SoDienThoai), "Số điện thoại không hợp lệ (10-11 số, bắt đầu bằng 0).");

        if (input.NgayHetHanThe <= input.NgayCapThe)
            result.AddError(nameof(input.NgayHetHanThe), "Ngày hết hạn thẻ phải sau ngày cấp thẻ.");

        if (maSoThe != null && !await IsMaSoTheAvailableAsync(maSoThe, input.MaBanDoc, ct))
            result.AddError(nameof(input.MaSoThe), "Mã số thẻ đã tồn tại.");

        var banDoc = await _db.BanDocs
            .Include(b => b.TaiKhoan)
            .FirstOrDefaultAsync(b => b.MaBanDoc == input.MaBanDoc, ct);

        if (banDoc is null)
            return ServiceResult.Fail("Không tìm thấy bạn đọc.");

        if (!await _taiKhoanService.IsEmailAvailableAsync(email, banDoc.MaTaiKhoan, ct))
            result.AddError(nameof(input.Email), "Email đã được sử dụng bởi tài khoản khác.");

        if (!result.Succeeded)
            return result;

        banDoc.HoTen = hoTen;
        banDoc.NgaySinh = input.NgaySinh;
        banDoc.GioiTinh = input.GioiTinh;
        banDoc.SoDienThoai = soDienThoai;
        banDoc.Email = email;
        banDoc.DiaChi = diaChi;
        banDoc.MaSoThe = maSoThe ?? banDoc.MaSoThe;
        banDoc.LoaiBanDoc = input.LoaiBanDoc;
        banDoc.NgayCapThe = input.NgayCapThe;
        banDoc.NgayHetHanThe = input.NgayHetHanThe;
        banDoc.TrangThai = input.TrangThai;

        if (banDoc.TaiKhoan != null)
        {
            banDoc.TaiKhoan.HoTen = hoTen;
            banDoc.TaiKhoan.Email = email;
        }

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            return ServiceResult.Fail("Không thể cập nhật hồ sơ bạn đọc.");
        }

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> UpdateProfileAsync(UpdateProfileInput input, int maTaiKhoan, CancellationToken ct = default)
    {
        var result = new ServiceResult();
        var homNay = DateOnly.FromDateTime(DateTime.Today);

        var hoTen = (input.HoTen ?? string.Empty).Trim();
        var email = (input.Email ?? string.Empty).Trim();
        var soDienThoai = (input.SoDienThoai ?? string.Empty).Trim();
        var diaChi = (input.DiaChi ?? string.Empty).Trim();
        soDienThoai = Regex.Replace(soDienThoai, @"[\s\.\-]+", "");

        if (string.IsNullOrWhiteSpace(hoTen))
            result.AddError(nameof(input.HoTen), "Họ tên là bắt buộc.");

        if (input.NgaySinh < new DateOnly(1900, 1, 1) || input.NgaySinh >= homNay)
            result.AddError(nameof(input.NgaySinh), "Ngày sinh không hợp lệ.");

        if (string.IsNullOrWhiteSpace(email) || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email))
            result.AddError(nameof(input.Email), "Email không đúng định dạng.");

        if (string.IsNullOrWhiteSpace(soDienThoai) || !Regex.IsMatch(soDienThoai, @"^0\d{9,10}$"))
            result.AddError(nameof(input.SoDienThoai), "Số điện thoại không hợp lệ (10-11 số, bắt đầu bằng 0).");

        if (string.IsNullOrWhiteSpace(diaChi))
            result.AddError(nameof(input.DiaChi), "Địa chỉ là bắt buộc.");

        var banDoc = await _db.BanDocs
            .Include(b => b.TaiKhoan)
            .FirstOrDefaultAsync(b => b.MaTaiKhoan == maTaiKhoan, ct);

        if (banDoc is null)
            return ServiceResult.Fail("Không tìm thấy hồ sơ bạn đọc của tài khoản hiện tại.");

        if (!await _taiKhoanService.IsEmailAvailableAsync(email, maTaiKhoan, ct))
            result.AddError(nameof(input.Email), "Email đã được sử dụng bởi tài khoản khác.");

        if (!result.Succeeded)
            return result;

        // Bạn đọc chỉ được sửa thông tin cá nhân của chính mình, KHÔNG được tự sửa thẻ hoặc trạng thái
        banDoc.HoTen = hoTen;
        banDoc.NgaySinh = input.NgaySinh;
        banDoc.GioiTinh = input.GioiTinh;
        banDoc.SoDienThoai = soDienThoai;
        banDoc.Email = email;
        banDoc.DiaChi = diaChi;

        if (banDoc.TaiKhoan != null)
        {
            banDoc.TaiKhoan.HoTen = hoTen;
            banDoc.TaiKhoan.Email = email;
        }

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            return ServiceResult.Fail("Không thể lưu thông tin cá nhân. Vui lòng thử lại.");
        }

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> SetStatusAsync(int maBanDoc, TrangThaiBanDoc trangThai, CancellationToken ct = default)
    {
        var banDoc = await _db.BanDocs.FindAsync(new object[] { maBanDoc }, ct);
        if (banDoc is null)
            return ServiceResult.Fail("Không tìm thấy bạn đọc.");

        banDoc.TrangThai = trangThai;
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Success();
    }

    public async Task<bool> IsMaSoTheAvailableAsync(string maSoThe, int? excludeMaBanDoc = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(maSoThe)) return true;
        var s = maSoThe.Trim();

        return !await _db.BanDocs.AnyAsync(b => b.MaSoThe == s && (!excludeMaBanDoc.HasValue || b.MaBanDoc != excludeMaBanDoc.Value), ct);
    }

    public async Task<List<TaiKhoanLookupItem>> GetUnlinkedAccountsAsync(int? currentMaTaiKhoan = null, CancellationToken ct = default)
    {
        var linkedIds = await _db.BanDocs
            .Where(b => !currentMaTaiKhoan.HasValue || b.MaTaiKhoan != currentMaTaiKhoan.Value)
            .Select(b => b.MaTaiKhoan)
            .ToListAsync(ct);

        return await _db.TaiKhoans
            .AsNoTracking()
            .Where(t => t.VaiTro == VaiTro.BanDoc && t.TrangThai == TrangThaiTaiKhoan.HoatDong && !linkedIds.Contains(t.MaTaiKhoan))
            .OrderBy(t => t.TenDangNhap)
            .Select(t => new TaiKhoanLookupItem(t.MaTaiKhoan, t.TenDangNhap, t.HoTen, t.Email))
            .ToListAsync(ct);
    }

    public async Task<ServiceResult<int>> DangKyTheAsync(DangKyTheBanDocInput input, int maTaiKhoan, CancellationToken ct = default)
    {
        var result = new ServiceResult<int>();
        var homNay = DateOnly.FromDateTime(DateTime.Today);

        var hoTen = (input.HoTen ?? string.Empty).Trim();
        var email = (input.Email ?? string.Empty).Trim();
        var soDienThoai = (input.SoDienThoai ?? string.Empty).Trim();
        var diaChi = (input.DiaChi ?? string.Empty).Trim();
        soDienThoai = Regex.Replace(soDienThoai, @"[\s\.\-]+", "");

        if (string.IsNullOrWhiteSpace(hoTen))
            result.AddError(nameof(input.HoTen), "Họ và tên là bắt buộc.");

        if (input.NgaySinh < new DateOnly(1900, 1, 1) || input.NgaySinh >= homNay)
            result.AddError(nameof(input.NgaySinh), "Ngày sinh không hợp lệ.");

        if (string.IsNullOrWhiteSpace(email) || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email))
            result.AddError(nameof(input.Email), "Email không đúng định dạng.");

        if (string.IsNullOrWhiteSpace(soDienThoai) || !Regex.IsMatch(soDienThoai, @"^0\d{9,10}$"))
            result.AddError(nameof(input.SoDienThoai), "Số điện thoại không hợp lệ (10-11 số, bắt đầu bằng 0).");

        if (string.IsNullOrWhiteSpace(diaChi))
            result.AddError(nameof(input.DiaChi), "Địa chỉ là bắt buộc.");

        var taiKhoan = await _db.TaiKhoans.FirstOrDefaultAsync(t => t.MaTaiKhoan == maTaiKhoan, ct);
        if (taiKhoan is null)
            return ServiceResult<int>.Error(string.Empty, "Không tìm thấy tài khoản người dùng.");

        if (false && taiKhoan.VaiTro != VaiTro.BanDoc)
            return ServiceResult<int>.Error(string.Empty, "Chỉ tài khoản Bạn đọc mới có thể đăng ký thẻ bạn đọc.");

        if (!await _taiKhoanService.IsEmailAvailableAsync(email, maTaiKhoan, ct))
            result.AddError(nameof(input.Email), "Email đã được sử dụng bởi tài khoản khác.");

        if (!result.Succeeded)
            return result;

        var existingBanDoc = await _db.BanDocs.FirstOrDefaultAsync(b => b.MaTaiKhoan == maTaiKhoan, ct);
        if (existingBanDoc != null)
        {
            if (existingBanDoc.TrangThai == TrangThaiBanDoc.HoatDong)
                return ServiceResult<int>.Error(string.Empty, "Tài khoản của bạn đã có thẻ thư viện đang hoạt động.");

            // Cập nhật lại thông tin và chuyển sang Chờ duyệt (áp dụng cho trường hợp gửi lại sau khi bị từ chối hoặc sửa thông tin chờ duyệt)
            existingBanDoc.HoTen = hoTen;
            existingBanDoc.NgaySinh = input.NgaySinh;
            existingBanDoc.GioiTinh = input.GioiTinh;
            existingBanDoc.SoDienThoai = soDienThoai;
            existingBanDoc.Email = email;
            existingBanDoc.DiaChi = diaChi;
            existingBanDoc.LoaiBanDoc = input.LoaiBanDoc;
            existingBanDoc.TrangThai = TrangThaiBanDoc.ChoDuyet;
            existingBanDoc.NgayCapThe = homNay;
            existingBanDoc.NgayHetHanThe = homNay.AddYears(1);

            taiKhoan.HoTen = hoTen;
            taiKhoan.Email = email;

            await _db.SaveChangesAsync(ct);
            return ServiceResult<int>.Ok(existingBanDoc.MaBanDoc);
        }

        var banDoc = new BanDoc
        {
            MaTaiKhoan = maTaiKhoan,
            HoTen = hoTen,
            NgaySinh = input.NgaySinh,
            GioiTinh = input.GioiTinh,
            SoDienThoai = soDienThoai,
            Email = email,
            DiaChi = diaChi,
            LoaiBanDoc = input.LoaiBanDoc,
            NgayCapThe = homNay,
            NgayHetHanThe = homNay.AddYears(1),
            TrangThai = TrangThaiBanDoc.ChoDuyet
        };

        taiKhoan.HoTen = hoTen;
        taiKhoan.Email = email;

        _db.BanDocs.Add(banDoc);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            var msg = ex.InnerException?.Message ?? ex.Message;
            return ServiceResult<int>.Error(string.Empty, $"Không thể gửi hồ sơ đăng ký: {msg}");
        }

        return ServiceResult<int>.Ok(banDoc.MaBanDoc);
    }

    public async Task<ServiceResult> DuyetTheAsync(int maBanDoc, CancellationToken ct = default)
    {
        var banDoc = await _db.BanDocs.FindAsync(new object[] { maBanDoc }, ct);
        if (banDoc is null)
            return ServiceResult.Fail("Không tìm thấy hồ sơ bạn đọc.");

        var homNay = DateOnly.FromDateTime(DateTime.Today);
        banDoc.TrangThai = TrangThaiBanDoc.HoatDong;
        banDoc.NgayCapThe = homNay;
        banDoc.NgayHetHanThe = homNay.AddYears(1);

        if (string.IsNullOrWhiteSpace(banDoc.MaSoThe))
        {
            banDoc.MaSoThe = $"TV-{banDoc.MaBanDoc:D6}";
        }

        await _db.SaveChangesAsync(ct);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> TuChoiTheAsync(int maBanDoc, string? lyDo = null, CancellationToken ct = default)
    {
        var banDoc = await _db.BanDocs.FindAsync(new object[] { maBanDoc }, ct);
        if (banDoc is null)
            return ServiceResult.Fail("Không tìm thấy hồ sơ bạn đọc.");

        banDoc.TrangThai = TrangThaiBanDoc.TuChoi;
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> HuyDangKyTheAsync(int maTaiKhoan, CancellationToken ct = default)
    {
        var banDoc = await _db.BanDocs.FirstOrDefaultAsync(b => b.MaTaiKhoan == maTaiKhoan && b.TrangThai == TrangThaiBanDoc.ChoDuyet, ct);
        if (banDoc is null)
            return ServiceResult.Fail("Không tìm thấy yêu cầu đăng ký thẻ nào đang chờ duyệt.");

        _db.BanDocs.Remove(banDoc);
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Success();
    }
}
